using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using sc2dsstats.lib.Service;
using sc2dsstats.shared.Service;
using sc2dsstats.web.Controllers;
using System;
using System.Globalization;

namespace sc2dsstats.web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Configuration.GetSection("Config").Bind(DSdata.Config);
            Configuration.GetSection("ServerConfig").Bind(DSdata.ServerConfig);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddDbContext<DSReplayContext>(options =>
                options.UseMySql(DSdata.ServerConfig.DBConnectionString, mySqlOptions => mySqlOptions
                .ServerVersion(new ServerVersion(new Version(5, 7, 29), ServerType.MySql))));
            services.AddSingleton<ReloadFilterAttribute>();
            services.AddControllers();
            services.AddSingleton<LoadData>();
            services.AddScoped<DSoptions>();
            services.AddScoped<ChartService>();
            services.AddScoped<GameChartService>();
            services.AddScoped<DBService>();
            services.AddScoped<Visitor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            DSdata.Init();
            app.ApplicationServices.GetService<LoadData>().Init();
            app.ApplicationServices.GetService<LoadData>().DataLoaded += Update;

            string basePath = Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH");
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
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                //ForwardedHeaders = ForwardedHeaders.All
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
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
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }

        void Update(object sender, EventArgs ex)
        {
            DataService.Reset();
            BuildService.Reset();
            UpdateDataService.Reset();
        }
    }
}
