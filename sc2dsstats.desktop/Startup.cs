using ElectronNET.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using paxgamelib;
using sc2dsstats.desktop.Data;
using sc2dsstats.desktop.Service;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.shared.Service;
using System.Globalization;
using System.Threading.Tasks;

namespace sc2dsstats.desktop
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
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddDbContext<DSReplayContext>(options =>
                //options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;AttachDbFilename={Program.workdir}\\dsreplays.mdf; Database=sc2dsstats_replays;Trusted_Connection=True;MultipleActiveResultSets=true"));
                options.UseSqlite($"Data Source={Program.workdir}/data.db"));
            services.AddSingleton<LoadData>();
            services.AddScoped<DSoptions>();
            services.AddScoped<ChartService>();
            services.AddScoped<GameChartService>();
            services.AddScoped<Refresh>();
            services.AddScoped<Status>();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

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

            // app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
            Task.Run(() => { paxgame.Init(); });
            Task.Run(async () => await Electron.WindowManager.CreateWindowAsync());
        }
    }
}
