using Blazored.Toast;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.app.Services;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats.lib.Db;
using System.Globalization;
using System.Net.Http.Headers;

namespace sc2dsstats.app
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            string dbFile = Path.Combine(Program.workdir, "data_v4_0.db");
            string olddbFile = Path.Combine(Program.workdir, "data_v3_0.db");

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddDbContext<sc2dsstatsContext>(options =>
                options.UseSqlite($"Data Source={dbFile}",
                   x =>
                   {
                       x.MigrationsAssembly("sc2dsstats.app");
                       x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                   }
                )
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
            );

            if (File.Exists(olddbFile))
            {
                services.AddDbContext<DSReplayContext>(options =>
                    options.UseSqlite($"Data Source={olddbFile}",
                        x =>
                        {
                            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                        }
                ));
            }

            services.AddMemoryCache();
            services.AddBlazoredToast();

            services.AddScoped<IDataService, DataService>();
            // services.AddSingleton<UploadService>();
            services.AddSingleton<InsertService>();
            services.AddSingleton<CacheService>();
            services.AddSingleton<Services.ReplayService>();

            services.AddHttpClient("sc2dsstats.app", client =>
            {
                // client.BaseAddress = new Uri("https://localhost:5003");
                // client.BaseAddress = new Uri("https://sc2dsstats.pax77.org:9777");
                client.BaseAddress = new Uri("https://sc2dsstats.pax77.org");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSupload77");
            });

            services.AddHttpClient("github", client =>
            {
                client.BaseAddress = new Uri("https://raw.githubusercontent.com");
            });

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, sc2dsstatsContext context, ILoggerFactory loggerFactory)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            ApplicationLogging.LoggerFactory = loggerFactory;

            context.Database.Migrate();

            var path = ElectronService.GetPath().GetAwaiter().GetResult();

            DSData.Init(path);
            NameService.Init(context, path).GetAwaiter().GetResult();


            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
            Task.Run(async () => await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions()
            {
                AutoHideMenuBar = true,
                Width = 1920,
                Height = 1080,
                X = 0,
                Y = 0
            }));
        }
    }

    internal static class ApplicationLogging
    {
        internal static ILoggerFactory LoggerFactory { get; set; }// = new LoggerFactory();
        internal static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
        internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);

    }
}
