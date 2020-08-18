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

      protected TopGHReposContext Context { get; set; }

      public Runner(Configuration config, TopGHReposContext context)
      {
         Config = config;
         Context = context;
      }

      public async Task Run()
      {
         await new GetReposFromGH(Config, Context).Run();
      }

   }
}
