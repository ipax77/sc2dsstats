using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace sc2dsstats.app
{
    public class Program
    {
        public static string workdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_desktop";
        public static string myConfig = workdir + "\\config2.json";
        public static Version Version = new Version(3, 0, 0);

        public static void Main(string[] args)
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

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile(myConfig, optional: true, reloadOnChange: false);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseElectron(args);
                    webBuilder.UseStartup<Startup>();
                });
    }
}
