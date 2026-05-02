using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopGHRepos.CMD.CMD
{
   public class CmdOption
   {
      [Option("logtofile")]
      public bool EnableLoggingToFile { get; set; } = false;

      [Option("GITHUB_PAT")]
      public string GITHUB_PAT { get; set; } = null;

      [Option("minStars")]
      public int? MinStars { get; set; }

      [Option("maxStars")]
      public int? MaxStars { get; set; }

      [Option("searchWaitInterval")]
      public int? SearchWaitInterval { get; set; }
      
      [Option("initialExpectedItemCount")]
      public int? InitialExpectedItemCount { get; set; }
      
      [Option("initialBatchSearchExpectedItemRetryCount")]
      public int? InitialBatchSearchExpectedItemRetryCount { get; set; }

      [Option("sqliteFile")]
      public string SQLiteFile { get; set; }

   }
}
