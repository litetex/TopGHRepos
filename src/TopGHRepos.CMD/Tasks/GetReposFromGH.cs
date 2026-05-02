using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TopGHRepos.CMD.Config;
using TopGHRepos.Data;
using TopGHRepos.Models.Entities;
using Range = Octokit.Range;

namespace TopGHRepos.CMD.Tasks
{
   public class GetReposFromGH
   {
      protected Configuration Config { get; set; }

      protected TopGHReposContext Context { get; set; }

      protected GitHubClient GitHubClient { get; set; }

      protected int SearchWaitInterval { get; set; } = Configuration.SearchWaitIntervalWithoutToken;


      private readonly object _lockRateLimit = new object();

      private long TotalProcessedItems { get; set; } = 0;

      private readonly object _lockDBWriter = new object();

      private HashSet<long> AlreadyProcessedRepoIds { get; set; } = new HashSet<long>();

      public GetReposFromGH(Configuration config, TopGHReposContext context)
      {
         Config = config;
         Context = context;

         GitHubClient = new GitHubClient(new ProductHeaderValue("Top-Repos-Crawler"));
         if (config.GitHubPAT != null)
            GitHubClient.Credentials = new Credentials(config.GitHubPAT);


         ConfigureSearchWaitInterval();
         Log.Info($"SearchWaitInterval is {SearchWaitInterval}ms");
      }

      protected void ConfigureSearchWaitInterval()
      {
         if (Config.SearchWaitInterval.HasValue)
         {
            SearchWaitInterval = Config.SearchWaitInterval.Value;
            return;
         }

         SearchWaitInterval = !string.IsNullOrWhiteSpace(Config.GitHubPAT) ? Configuration.SearchWaitIntervalWithToken : Configuration.SearchWaitIntervalWithoutToken;
      }

      public async Task Run()
      {
         var runStart = DateTimeOffset.Now;
         var lastBatchFinished = runStart;
         
         BatchResult lastBatchResult = new BatchResult(Config.SearchMinStars, Config.InitialExpectedItemCount);
         int batchNum = 0;
         do
         {
            lastBatchResult = await DoSearchBatch(++batchNum, lastBatchResult, runStart, lastBatchFinished);
            lastBatchFinished = DateTimeOffset.Now;
         } while (lastBatchResult.Stars > 0);

         Log.Info($"Total processed items: {TotalProcessedItems}");
         Log.Info($"Total processed unique repos: {AlreadyProcessedRepoIds.Count}");
      }
      
      public record BatchResult(
         int Stars,
         int ExpectedItemsLeft
      )
      {
      }

      public async Task<BatchResult> DoSearchBatch(int currentBatch, BatchResult lastBatchResult, DateTimeOffset runStart, DateTimeOffset lastBatchFinished)
      {
         int minStars = lastBatchResult.Stars;
         
         Log.Info($"Starting batch #{currentBatch}; with MinStars >= {minStars}");
         object lockTotalProcessedItems = new object();
         int lastBatchRepoStars = -1;
         int totalSearchedBatchItems = 0;

         var databaseProcessorTasks = new List<Task>();

         Log.Info($"Doing initial search-page (1) of batch #{currentBatch}");
         SearchRepositoryResult initialSearchResult = null;
         int initialRetryCount = 0;
         do
         {
            if (initialRetryCount > 1)
            {
               Log.Warn($"Retrying[{initialRetryCount}] initial search as it yielded no results: " +
                        $"[TotalCount=${initialSearchResult?.TotalCount}," +
                        $"LastBatch.ExpectedItemsLeft=${lastBatchResult.ExpectedItemsLeft}," +
                        $"TotalProcessedItems=${TotalProcessedItems}]");
               Thread.Sleep(initialRetryCount * 5_000);
            }
            
            initialSearchResult = await Search(GetRepositoriesRequest(0, minStars), currentBatch, 1);
            initialRetryCount++;
         } while (initialSearchResult.Items.Count == 0 
                  && lastBatchResult.ExpectedItemsLeft > Config.InitialBatchSearchExpectedItemRetryCount 
                  && initialRetryCount <= 3);

         TrySetLastRepo(currentBatch, 1, initialSearchResult, lastRepo => lastBatchRepoStars = lastRepo.StargazersCount);

         var initialDbProcessorTask = Task.Run(() =>
         {
            try
            {
               ProcessForDB(currentBatch, 0, initialSearchResult);
            }
            catch(Exception ex)
            {
               Log.Error($"DB-processing failed for search-page ({1}) of batch #{currentBatch} Result={initialSearchResult}", ex);
            }
         });
         databaseProcessorTasks.Add(initialDbProcessorTask);

         Log.Info($"Initial search-page (1) returned: items={initialSearchResult.Items.Count}, totalItems={initialSearchResult.TotalCount}");
         totalSearchedBatchItems += initialSearchResult.Items.Count;
         var lastResult = initialSearchResult;

         int spanningSearchTasks = Math.Max(Math.Min((int)Math.Ceiling((initialSearchResult.TotalCount - 100) / 100.0), 9), 0);
         Log.Info($"Will span {spanningSearchTasks} parallel searches");

         var searchTasks = new List<Task>();
         for (int i = 1; i <= spanningSearchTasks; i++)
         {
            int currentSearchPage = i + 1;
            var searchTask = Task.Run(() => 
            {
               try
               {
                  Log.Info($"Doing search-page ({currentSearchPage}) of batch #{currentBatch}");
                  var searchResult = Search(GetRepositoriesRequest(currentSearchPage, minStars), currentBatch, currentSearchPage).Result;

                  lastResult = searchResult;

                  if (currentSearchPage == spanningSearchTasks + 1)
                     TrySetLastRepo(currentBatch, currentSearchPage, searchResult, lastRepo => lastBatchRepoStars = lastRepo.StargazersCount);

                  databaseProcessorTasks.Add(Task.Run(() =>
                  {
                     try
                     {
                        ProcessForDB(currentBatch, currentSearchPage, searchResult);
                     }
                     catch(Exception ex)
                     {
                        Log.Error($"DB-processing failed for search-page ({currentSearchPage}) of batch #{currentBatch}; Result={searchResult}", ex);
                     }
                  }));

                  Log.Info($"Search-page ({currentSearchPage}) returned: items={searchResult.Items.Count()}, totalItems={searchResult.TotalCount}");
                  lock (lockTotalProcessedItems)
                     totalSearchedBatchItems += searchResult.Items.Count();
               }
               catch(Exception ex)
               {
                  Log.Error($"Exception in searchTask #{currentBatch}/({currentSearchPage})", ex);
               }
            });
            searchTasks.Add(searchTask);

            Thread.Sleep(SearchWaitInterval);
         }

         Log.Info("Waiting for all searchTasks to finish");
         await Task.WhenAll(searchTasks);
         Log.Info("All searchTasks finished");

         Log.Info("Waiting for all databaseProcessorTasks to finish");
         await Task.WhenAll(databaseProcessorTasks);
         Log.Info("All databaseProcessorTasks finished");

         Log.Info("Saving changes to db");
         await Context.SaveChangesAsync();
         Log.Info("Saved changes to db");

         Log.Info($"Processed items of #{currentBatch}: {totalSearchedBatchItems}");

         TotalProcessedItems += totalSearchedBatchItems;
         
         ReportProgress(
            runStart, 
            lastBatchFinished, 
            TotalProcessedItems, 
            AlreadyProcessedRepoIds.Count,
            lastResult.TotalCount,
            totalSearchedBatchItems);
         
         int expectedItemsLeft = lastResult.TotalCount - totalSearchedBatchItems;
         if (totalSearchedBatchItems >= lastResult.TotalCount || lastBatchRepoStars == -1)
            return new BatchResult(-1, expectedItemsLeft);

         if(lastBatchRepoStars < minStars)
         {
            throw new InvalidOperationException($"Last Repo of Batch[Stars={lastBatchRepoStars}] returned less stars than inputted[{minStars}]");
         }
         if(lastBatchRepoStars == minStars)
         {
            Log.Warn($"Got more than 1000 Repos having the same star-count '{lastBatchRepoStars}' since last refitting query");
            Log.Warn("Will count +1 to not get the same search results again (may lose some repos) ...");
            lastBatchRepoStars++;
         }

         Context.DetachAllEntities();

         return new BatchResult(lastBatchRepoStars, expectedItemsLeft);
      }

      private SearchRepositoriesRequest GetRepositoriesRequest(int page, int minStars)
      {
         return new SearchRepositoriesRequest()
         {
            Stars = Config.SearchMaxStars.HasValue ? new Range(minStars, Config.SearchMaxStars.Value) : Range.GreaterThanOrEquals(minStars),
            Page = page,
            SortField = RepoSearchSort.Stars,
            Order = SortDirection.Ascending,
            PerPage = 100,
         };
      }

      private async Task<SearchRepositoryResult> Search(SearchRepositoriesRequest searchRequest, int currentBatch, int currentSearchPage)
      {
         CheckRateLimitAndWait();

         var searchResult = await GitHubClient.Search.SearchRepo(searchRequest);
         Log.Info($"Search returned {searchResult.Items.Count} results for #{currentBatch}/({currentSearchPage})");

         return searchResult;
      }

      private void CheckRateLimitAndWait()
      {
         lock (_lockRateLimit)
         {
            var rateLimitInfo = GitHubClient.RateLimit.GetRateLimits().Result;

            WriteRateLimit(rateLimitInfo);

            var searchRateLimit = rateLimitInfo.Resources.Search;

            while (searchRateLimit.Remaining <= 1)
            {
               var waitMS = Math.Max((int)(searchRateLimit.Reset - DateTimeOffset.Now).TotalMilliseconds + 1000, 1000);
               Log.Info($"RateLimit for search exceeded! Waiting till {searchRateLimit.Reset} (will wait for ~{waitMS} ms)");

               Thread.Sleep(waitMS);
               Log.Info("Waited long enough");

               rateLimitInfo = GitHubClient.RateLimit.GetRateLimits().Result;

               WriteRateLimit(rateLimitInfo);

               searchRateLimit = rateLimitInfo.Resources.Search;
            }
         }
      }

      private void WriteRateLimit(MiscellaneousRateLimit rateLimitInfo)
      {
         var coreRateLimit = rateLimitInfo.Resources.Core;
         Log.Debug($"RateLimit (Core): {coreRateLimit.Remaining}/{coreRateLimit.Limit}; Reset at {coreRateLimit.Reset:yyyy-MM-dd HH:mm:ss,fff}");
         var searchRateLimit = rateLimitInfo.Resources.Search;
         Log.Info($"RateLimit (Search): {searchRateLimit.Remaining}/{searchRateLimit.Limit}; Reset at {searchRateLimit.Reset:yyyy-MM-dd HH:mm:ss,fff}");
      }

      private void TrySetLastRepo(int currentBatch, int currentPage, SearchRepositoryResult searchResult, Action<Repository> onLastRepo)
      {
         var lastRepo = searchResult.Items.LastOrDefault((Repository)null);
         if (lastRepo == null)
            Log.Warn($"Unable to find last repo for search-page ({currentPage}) of batch #{currentBatch} skipping it");
         else
            onLastRepo?.Invoke(lastRepo);
      }


      private void ProcessForDB(int currentBatch, int currentPage, SearchRepositoryResult searchResult)
      {
         lock (_lockDBWriter)
         {
            try
            {
               int alreadyProcessedInBlock = 0;

               foreach (var repo in searchResult.Items)
               {
                  if (AlreadyProcessedRepoIds.Contains(repo.Id))
                  {
                     alreadyProcessedInBlock++;
                     continue;
                  }

                  var repoInfo = new RepositoryInfo
                  {
                     GitHubId = repo.Id,
                     Archived = repo.Archived,
                     CreatedAt = repo.CreatedAt,
                     DefaultBranch = repo.DefaultBranch,
                     Description = repo.Description,
                     Fork = repo.Fork,
                     ForksCount = repo.ForksCount,
                     HasDownloads = repo.HasDownloads,
                     HasIssues = repo.HasIssues,
                     HasPages = repo.HasPages,
                     HasWiki = repo.HasWiki,
                     Homepage = repo.Homepage,
                     HtmlUrl = repo.HtmlUrl,
                     IsTemplate = repo.IsTemplate,
                     Language = repo.Language,
                     LicenseKey = repo.License?.Key,
                     MirrorUrl = repo.MirrorUrl,
                     Name = repo.Name,
                     NodeId = repo.NodeId,
                     OpenIssuesCount = repo.OpenIssuesCount,
                     OwnerLoginName = repo.Owner.Login,
                     PushedAt = repo.PushedAt,
                     Size = repo.Size,
                     StargazersCount = repo.StargazersCount,
                     UpdatedAt = repo.UpdatedAt,
                     WatchersCount = repo.SubscribersCount
                  };

                  Context.RepositoryInfos.Add(repoInfo);
                  AlreadyProcessedRepoIds.Add(repo.Id);
               }

               Log.Info($"Processed changes of #{currentBatch}/({currentPage})={searchResult.Items.Count - alreadyProcessedInBlock}; already processed repos={alreadyProcessedInBlock}");
            }
            catch(Exception ex)
            {
               Log.Error($"Failed to process for DB #{currentBatch}/({currentPage})", ex);
            }
         }
      }

      private void ReportProgress(
         DateTimeOffset runStart,
         DateTimeOffset lastBatchFinished,
         long totalProcessedItems,
         int totalProcessedUniqueRepos,
         long lastResultTotalCount,
         int totalSearchedBatchItems)
      {
         try
         {
            var runningTime = DateTimeOffset.Now - runStart;
            var batchTime = DateTimeOffset.Now - lastBatchFinished;

            var itemsPerSecTotal = Math.Round(totalProcessedItems / runningTime.TotalSeconds, 2);
            var reposPerSecTotal = Math.Round(totalProcessedUniqueRepos / runningTime.TotalSeconds, 2);

            var itemsPerSecBatch = Math.Round(totalSearchedBatchItems / batchTime.TotalSeconds, 2);

            var itemsLeft = lastResultTotalCount - totalSearchedBatchItems;

            Log.Info($"\r\n=== PROGRESS ===\r\n" +
               $"- time needed since start: {runningTime}\r\n" +
               $"- time needed since last batch: {batchTime}\r\n" +
               $"- total processed: {totalProcessedItems} items | {AlreadyProcessedRepoIds.Count} repos ({(totalProcessedItems > 0 ? Math.Round(totalProcessedUniqueRepos / (double)totalProcessedItems * 100, 2).ToString() : "--")}%)\r\n" +
               $"- process speed (total): {itemsPerSecTotal} item/s | {reposPerSecTotal} repo/s\r\n" +
               $"- process speed (batch): {itemsPerSecBatch} item/s\r\n" +
               $"- total left (estimated): {itemsLeft} items\r\n" +
               $"- total time left (estimated): {TimeSpan.FromSeconds(Math.Round(Math.Min(itemsLeft / Math.Max(itemsPerSecTotal, 0.0001), 60 * 60 * 24)))}s (total-speed) | {TimeSpan.FromSeconds(Math.Round(Math.Min(itemsLeft / Math.Max(itemsPerSecBatch, 0.0001), 60 * 60 * 24)))}s (batch-speed)\r\n");
         }
         catch(Exception ex)
         {
            Log.Warn("Failed to report progress", ex);
         }
      }
   }
}
