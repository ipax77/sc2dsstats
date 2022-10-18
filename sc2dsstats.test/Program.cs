using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var loc = Assembly.GetExecutingAssembly().Location;

var logger = ApplicationLogging.CreateLogger<Program>();
logger.LogInformation("Running ...");

var services = new ServiceCollection();

services.AddDbContext<ReplayContext>(options =>
{
    options
        .UseLoggerFactory(ApplicationLogging.LogFactory)
        .UseSqlite(@"Data Source=C:\Users\pax77\AppData\Local\Packages\sc2dsstats.maui_veygnay3cpztg\LocalState\dsstats2.db",
        x =>
        {
            x.MigrationsAssembly("SqliteMigrations");
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        }
        ).EnableSensitiveDataLogging()
        .EnableDetailedErrors();
});

var serviceProvider = services.BuildServiceProvider();
var context = serviceProvider.GetService<ReplayContext>();

ArgumentNullException.ThrowIfNull(context);

context.Database.Migrate();

var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<AutoMapperProfile>();
});
var mapper = config.CreateMapper();

// var mmrService = new MmrService(serviceProvider, mapper);
// var mmrService = new FireMmrService(serviceProvider, mapper);

// mmrService.CalcMmmr().GetAwaiter().GetResult();

// var dev = await context.Players
//     .GroupBy(g => Math.Round(g.DsR, 0))
//     .Select(s => new
//     {
//         Count = s.Count(),
//         AvgDsr = s.Average(a => Math.Round(a.DsR, 0))
//     }).ToListAsync();

// foreach (var d in dev)
// {
//     Console.WriteLine($"{d.Count} => {d.AvgDsr}");
// }

var buildService = new BuildService(context);

var buildRequest = new BuildRequest()
{
    PlayerNames = new List<string>() { "PAX" },
    Interest = Commander.Abathur,
    StartTime = new DateTime(2020, 1, 1),
    EndTime = DateTime.Today
};

Stopwatch sw = new();
sw.Start();
var result = buildService.GetBuild(buildRequest).GetAwaiter().GetResult();

// buildService.GetBuildResponse(buildRequest).GetAwaiter().GetResult();

sw.Stop();

Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");

Console.ReadLine();


public static class ApplicationLogging
{
    public static ILoggerFactory LogFactory { get; } = LoggerFactory.Create(builder =>
    {
        builder.ClearProviders();
        // Clear Microsoft's default providers (like eventlogs and others)
        builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd hh:mm:ss ";
        }).SetMinimumLevel(LogLevel.Information);
    });

    public static ILogger<T> CreateLogger<T>() => LogFactory.CreateLogger<T>();
}