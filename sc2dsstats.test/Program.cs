using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var loc = Assembly.GetExecutingAssembly().Location;

Console.WriteLine($"Location: {loc}");

var services = new ServiceCollection();

services.AddDbContext<ReplayContext>(options =>
{
    options.UseSqlite(@"Data Source=C:\Users\pax77\AppData\Local\Packages\sc2dsstats.maui_veygnay3cpztg\LocalState\dsstats2.db",
        x =>
        {
            x.MigrationsAssembly("SqliteMigrations");
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        }
        );
});

var serviceProvider = services.BuildServiceProvider();
var context = serviceProvider.GetService<ReplayContext>();

ArgumentNullException.ThrowIfNull(context);

var mmrService = new MmrService(context);

mmrService.CalcMmmr().GetAwaiter().GetResult();

Console.ReadLine();