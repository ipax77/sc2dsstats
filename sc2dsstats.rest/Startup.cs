using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sc2dsstats.rest.Attributes;
using sc2dsstats.rest.Repositories;
using sc2dsstats.data;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using System;
using Pomelo.EntityFrameworkCore.MySql;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace sc2dsstats.rest
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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DSRestContext>(options =>
                options.UseMySql(DSdata.ServerConfig.DBRestConnectionString, mySqlOptions => mySqlOptions
                .ServerVersion(new ServerVersion(new Version(5, 7, 29), ServerType.MySql))));
            services.AddSingleton<IDataRepository, DataRepository>();
            services.AddScoped<AuthenticationFilterAttribute>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var bab = app.ApplicationServices.GetService<IDataRepository>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
