﻿using CommandLine;
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

      [Option("minstars")]
      public int? MinStars { get; set; }

      [Option("maxstars")]
      public int? MaxStars { get; set; }

      [Option("searchwaitinterval")]
      public int? SearchWaitInterval { get; set; }

      [Option("sqllitefile")]
      public string SQLLiteOutputFile { get; set; }

   }
}
