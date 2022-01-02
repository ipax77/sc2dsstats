using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Server.Attributes;
using sc2dsstats._2022.Server.Hubs;
using sc2dsstats._2022.Server.Services;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats.lib.Db;
using System.Globalization;

namespace sc2dsstats._2022.Server
{
    public class Startup
    {
        readonly string MyAllowSpecificOrigins = "mtOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration["ServerConfig:WSLConnectionString2"];
            // var connectionString = Configuration["ServerConfig:ProdConnectionString"];

            var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 36));
            services.AddDbContext<sc2dsstatsContext>(
                dbContextOptions => dbContextOptions
                    .UseMySql(connectionString, serverVersion, p =>
                    {
                        p.EnableRetryOnFailure();
                        p.MigrationsAssembly("sc2dsstats.2022.Server");
                        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                    })
                // .EnableSensitiveDataLogging() // <-- These two calls are optional but help
                // .EnableDetailedErrors()       // <-- with debugging (remove for production).
            );

            // oldcontexts - to be deleted ----------------------------------------------
            services.AddDbContext<DSReplayContext>(options =>
                options.UseMySql(Configuration["ServerConfig:DBConnectionStringOld"], serverVersion,
                    // options.UseMySql(Configuration["ServerConfig:RemoteConnectionString"], serverVersion,
                    p =>
                    {
                        p.EnableRetryOnFailure();
                    }
                ));

            services.AddDbContext<DSRestContext>(options =>
                options.UseMySql(Configuration["ServerConfig:DBRestConnectionString"], serverVersion)
            );
            // -------------------------------------------------------------------------

            // services.AddCors(options =>
            // {
            //     options.AddPolicy(name: MyAllowSpecificOrigins,
            //                     builder =>
            //                     {
            //                         builder.WithOrigins(
            //                                             "https://localhost:5001",
            //                                             "https://localhost:5003"
            //                                             );
            //                     });
            // });

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddMemoryCache();
            services.AddSignalR();

            services.AddScoped<UpdateService>();

            services.AddSingleton<AuthenticationFilterAttribute>();
            // services.AddSingleton<UploadService>();
            // services.AddSingleton<InsertService>();
            services.AddSingleton<IInsertService, Services.InsertService>();
            services.AddSingleton<ProducerService>();
            services.AddSingleton<CacheService>();

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.AddW3CLogging(options =>
            {
                options.LogDirectory = "/data/logs";
                options.LoggingFields = W3CLoggingFields.Request | W3CLoggingFields.ConnectionInfoFields;
            });

            services.AddApiVersioning(setup =>
            {
                setup.DefaultApiVersion = new ApiVersion(1, 0);
                setup.AssumeDefaultVersionWhenUnspecified = true;
                setup.ReportApiVersions = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, sc2dsstatsContext context, ILogger<Startup> _logger, CacheService cacheService)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            DSData.Init();

            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
            context.Database.Migrate();

            NameService.Init(context, "").GetAwaiter().GetResult();

            _ = cacheService.SetBuildCache();


            // StatsService.TeamStats(context);

            //var oldreps = oldcontext.DSReplays.Count();
            //_logger.LogInformation($"oldreps: {oldreps}");
            //var newreps = context.Dsreplays.Count();
            //_logger.LogInformation($"newreps: {newreps}");

            //DbService.FixGamemode(context, oldcontext);

            //cacheService.SetBuildCache();

            // OldContextsService.CopyRestPlayerData(context, restcontext);
            // OldContextsService.UpdateFromOldDb(context, oldcontext, insertService, fullCopy: true);

            string basePath = Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH");
            // string basePath = "/sc2dsstats";
            if (!string.IsNullOrEmpty(basePath))
            {
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });

                app.Use((context, next) =>
                {
                    context.Request.PathBase = new PathString(basePath);
                    if (context.Request.Path.StartsWithSegments(basePath, out var remainder))
                    {
                        context.Request.Path = remainder;
                    }
                    return next();
                });
            }
            else
                basePath = String.Empty;

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<PickBanHub>("/pickbanhub");
                endpoints.MapHub<GdslHub>("/gdslhub");
                endpoints.MapHub<PbHub>("/pbhub");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
