using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TopGHRepos.CMD.Config;
using TopGHRepos.CMD.Tasks;
using TopGHRepos.Data;
using TopGHRepos.Models.Entities;
using Range = Octokit.Range;

namespace TopGHRepos.CMD
{
   public class Runner
   {
      protected Configuration Config { get; set; }

      public Runner(Configuration config)
      {
         Config = config;
      }

      public async Task Run()
      {
         var optBuilder = new DbContextOptionsBuilder<TopGHReposContext>();
         optBuilder.UseSqlite($"Data Source={Config.SQLLiteOutputFile}");

         using var context = new TopGHReposContext(optBuilder.Options);

         Log.Info("Ensuring db is deleted");
         context.Database.EnsureDeleted();
         Log.Info("db is deleted");

         Log.Info($"Doing {context.Database.GetPendingMigrations().Count()} pending database migrations");
         context.Database.Migrate();
         Log.Info($"Migration successful");

         await new GetReposFromGH(Config, context).Run();

         Log.Info("Saving changes before shutting down");
         context.SaveChanges();
         Log.Info("Saved changes");
      }

   }
}
