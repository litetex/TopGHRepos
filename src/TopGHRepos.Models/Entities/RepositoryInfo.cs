using System;
using System.Collections.Generic;
using System.Text;

namespace TopGHRepos.Models.Entities
{
   public class RepositoryInfo : Entity
   {
      public string DefaultBranch { get; set; }

      public int OpenIssuesCount { get; set; }

      public DateTimeOffset? PushedAt { get; set; }

      public DateTimeOffset CreatedAt { get; set; }

      public DateTimeOffset UpdatedAt { get; set; }

      public bool HasIssues { get; set; }

      public int WatchersCount { get; set; }

      public bool HasWiki { get; set; }

      public bool HasDownloads { get; set; }

      public bool HasPages { get; set; }
      
      public string LicenseKey { get; set; }

      public int StargazersCount { get; set; }

      public int ForksCount { get; set; }

      public bool Fork { get; set; }

      public string HtmlUrl { get; set; }

      public string MirrorUrl { get; set; }

      public long GitHubId { get; set; }

      public string NodeId { get; set; }

      public string OwnerLoginName { get; set; }

      public string Name { get; set; }

      public bool IsTemplate { get; set; }

      public string Description { get; set; }

      public string Homepage { get; set; }

      public string Language { get; set; }

      public long Size { get; set; }

      public bool Archived { get; set; }
   }
}
