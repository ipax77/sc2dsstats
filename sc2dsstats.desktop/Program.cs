using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using sc2dsstats.desktop.Service;
using sc2dsstats.lib.Models;
using System.Linq;


namespace sc2dsstats.desktop
{
    public class Program
    {
        public static int DEBUG = 0;
        public static string workdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_desktop";
        public static string myScan_log = workdir + "/log.txt";
        public static string myJson_file = workdir + "/data.json";
        public static string myDetails_file = workdir + "/details.json";
        public static string myConfig = workdir + "/config.json";


        public static async Task Main(string[] args)
        {
            if (!Directory.Exists(workdir))
            {
                try
                {
                    Directory.CreateDirectory(workdir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + " An error occurred creating the workdir:" + workdir);
                    return;
                }
            }

            if (!File.Exists(myConfig))
                FirstRun.Helper();

            var host = CreateHostBuilder(args).Build();
            CreateDbIfNotExists(host);

            

            await host.RunAsync();
        }

        private static DSReplayContext CreateDbIfNotExists(IHost host)
        {
            DSReplayContext context = null;

            using (var scope = host.Services.CreateScope())
            {

                var services = scope.ServiceProvider;
                var config = services.GetRequiredService<IConfiguration>().GetSection("Config");
                if (config != null)
                    config.Bind(DSdata.Config);

                try
                {
                    context = services.GetRequiredService<DSReplayContext>();
                    context.Database.EnsureCreated();
                    DSdata.Status.Count = context.DSReplays.Count();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }
            return context;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    //config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                    //config.SetBasePath(workdir);
                    config.AddJsonFile(myConfig, optional: true, reloadOnChange: false);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseElectron(args);
                });
    }
}
