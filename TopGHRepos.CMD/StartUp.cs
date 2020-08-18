using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using TopGHRepos.CMD.CMD;
using TopGHRepos.CMD.Config;
using TopGHRepos.Data;
using TopGHRepos.Models.Entities;

namespace TopGHRepos.CMD
{
   public class StartUp
   {
      private CmdOption CmdOption { get; set; }

      private Configuration Config { get; set; } = new Configuration();

      public StartUp(CmdOption cmdOption)
      {
         CmdOption = cmdOption;
      }

      public void Start()
      {
         Contract.Requires(CmdOption != null);
         Log.Info($"Current directory is '{Directory.GetCurrentDirectory()}'");

         ReadCMDConfig();

         DoStart();
      }


      protected void ReadCMDConfig()
      {
         Log.Info("Doing config over commandline-args");

         if (!string.IsNullOrEmpty(CmdOption.GITHUB_TOKEN))
         {
            Log.Info($"SetInp: {nameof(Config.GitHubToken)}='****'");
            Config.GitHubToken = CmdOption.GITHUB_TOKEN;
         }

         if (CmdOption.MinStars != null)
         {
            Log.Info($"SetInp: {nameof(Config.SearchMinStars)}='{CmdOption.MinStars}'");
            Config.SearchMinStars = CmdOption.MinStars.Value;
         }

         if (CmdOption.MaxStars != null)
         {
            Log.Info($"SetInp: {nameof(Config.SearchMaxStars)}='{CmdOption.MaxStars}'");
            Config.SearchMaxStars = CmdOption.MaxStars;
         }

         if (CmdOption.SearchWaitInterval != null)
         {
            Log.Info($"SetInp: {nameof(Config.SearchWaitInterval)}='{CmdOption.SearchWaitInterval}'");
            Config.SearchWaitInterval = CmdOption.SearchWaitInterval.Value;
         }

      }


      protected void DoStart()
      {
         using (var context = new TopGHReposContext())
         {
            Log.Info("Ensuring db is deleted");
            context.Database.EnsureDeleted();
            Log.Info("db is deleted");

            Log.Info($"Doing {context.Database.GetPendingMigrations().Count()} pending database migrations");
            context.Database.Migrate();
            Log.Info($"Migration successful");

            new Runner(Config, context).Run().Wait();

            Log.Info("Saving changes before shutting down");
            context.SaveChanges();
            Log.Info("Saved changes");
         }

         Log.Info("Shutting down");
      }
   }
}
