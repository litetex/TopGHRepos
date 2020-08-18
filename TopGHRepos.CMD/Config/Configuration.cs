using System;
using System.Collections.Generic;
using System.Text;

namespace TopGHRepos.CMD.Config
{
   public class Configuration
   {
      public string GitHubToken { get; set; }

      public int SearchMinStars { get; set; } = 1000;

      public int? SearchMaxStars { get; set; } 

      // SearchAPI is resetted after ~1m = 60s, allows up to 30 requests -> Wait ~2s between each request
      // Optimum use case: 3000 Results / min
      public int SearchWaitInterval { get; set; } = 1950;
   }
}
