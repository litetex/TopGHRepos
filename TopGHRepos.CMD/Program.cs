using CommandLine;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.IO;
using System.Linq;
using TopGHRepos.CMD.CMD;
using TopGHRepos.Data;
using TopGHRepos.Models.Entities;

namespace TopGHRepos.CMD
{
   public static class Program
   {
      static void Main(string[] args)
      {
         Serilog.Log.Logger = GetDefaultLoggerConfiguration().CreateLogger();

         AppDomain.CurrentDomain.ProcessExit += (s, ev) =>
         {
            Log.Info("Shutting down logger; Flushing...");
            Serilog.Log.CloseAndFlush();
         };

#if !DEBUG
         AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
         {
            try
            {
               if (ev?.ExceptionObject is Exception ex)
               {
                  Log.Fatal("An unhandled error occured", ex);
                  return;
               }
               Log.Fatal($"An unhandled error occured {ev}");
            }
            catch(Exception ex)
            {
               Console.Error.WriteLine($"Failed to catch unhandled error '{ev?.ExceptionObject ?? ev}': {ex}");
            }
         };
#endif

         Parser.Default.ParseArguments<CmdOption>(args)
                     .WithParsed((opt) =>
                     {
                        if (opt.EnableLoggingToFile)
                        {
                           var logConf = GetDefaultLoggerConfiguration();

                           logConf.WriteTo.File(Path.Combine("logs", "log.log"),
                                 outputTemplate: "{Timestamp:HH:mm:ss,fff} {Level:u3} {ThreadId,-2} {Message:lj}{NewLine}{Exception}",
                                 rollingInterval: RollingInterval.Day,
                                 rollOnFileSizeLimit: true);

                           Serilog.Log.Logger = logConf.CreateLogger();
                           Log.Info("Logger will also write to file");
                        }

                        var starter = new StartUp(opt);
                        starter.Start();
                     })
                     .WithNotParsed((ex) =>
                     {
                        if (ex.All(err =>
                                new ErrorType[]
                                {
                                 ErrorType.HelpRequestedError,
                                 ErrorType.HelpVerbRequestedError
                                }.Contains(err.Tag))
                          )
                           return;

                        foreach (var error in ex)
                           Log.Error($"Failed to parse: {error.Tag}");

                        Log.Fatal("Failed to process args");
                     });
      }

      private static LoggerConfiguration GetDefaultLoggerConfiguration()
      {
         return new LoggerConfiguration()
            .Enrich.WithThreadId()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss,fff} {Level:u3} {ThreadId,-2} {Message:lj}{NewLine}{Exception}");
      }
   }
}
