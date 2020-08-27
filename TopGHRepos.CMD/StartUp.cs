﻿using Microsoft.EntityFrameworkCore;
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

         ReadEnvConfig();

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

         if(!string.IsNullOrWhiteSpace(CmdOption.SQLLiteOutputFile))
         {
            Log.Info($"SetInp: {nameof(Config.SQLLiteOutputFile)}='{CmdOption.SQLLiteOutputFile}'");
            Config.SQLLiteOutputFile = CmdOption.SQLLiteOutputFile;
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

      protected void ReadEnvConfig()
      {
         var ghPat = Environment.GetEnvironmentVariable("GITHUB_PAT") ?? Environment.GetEnvironmentVariable("GH_PAT");
         if (!string.IsNullOrWhiteSpace(ghPat))
         {
            Log.Info($"SetEnv: {nameof(Config.GitHubToken)}='{ghPat}'");
            Config.GitHubToken = ghPat;
         }
      }

      protected void DoStart()
      {
         Log.Info("Starting...");

         new Runner(Config).Run().Wait();

         Log.Info("Shutting down");
      }
   }
}
