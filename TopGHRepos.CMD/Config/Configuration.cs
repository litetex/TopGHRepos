using System;
using System.Collections.Generic;
using System.Text;

namespace TopGHRepos.CMD.Config
{
   public class Configuration
   {
      public string SQLLiteOutputFile { get; set; } = "database.sqlite";

      // SearchAPI (with token) is resetted after ~1m = 60s, allows up to 30 requests -> Wait ~2s between each request
      // Optimum use case: 3000 Results / min
      public const int SearchWaitIntervalWithToken = 1950;
      // Without token: 10 Requests -> Wait ~6s -> 1000 Results / min
      public const int SearchWaitIntervalWithoutToken = 5950;

      public string GitHubPAT { get; set; }

      public int SearchMinStars { get; set; } = 1000;

      public int? SearchMaxStars { get; set; }

      public int? SearchWaitInterval { get; set; }
   }
}
