// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sc2dsstats.db;


var services = new ServiceCollection();

var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
var config = json.GetProperty("ServerConfig");
var connectionString = config.GetProperty("ProdConnectionString").GetString();
var localConnectionString = config.GetProperty("DBConnectionString2").GetString();
var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 34));

bool dbToggle = true;

if (dbToggle)
{
    logger?.LogInformation("--------------------- MYSQL -------------------");
    services.AddDbContext<sc2dsstatsContext>(options =>
    {
        options.UseLoggerFactory(ApplicationLogging.LogFactory);
        options.UseMySql(connectionString, serverVersion, p =>
        {
            p.EnableRetryOnFailure();
            p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        });
    });

    services.AddDbContext<localContext>(options =>
    {
        options.UseLoggerFactory(ApplicationLogging.LogFactory);
        options.UseMySql(localConnectionString, serverVersion, p =>
        {
            p.EnableRetryOnFailure();
            p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        });
    });    
}
else
{
    logger?.LogInformation("--------------------- SQLITE -------------------");
    services.AddDbContext<sc2dsstatsContext>(options =>
    {
        options.UseLoggerFactory(ApplicationLogging.LogFactory);
        options.UseSqlite(@"Data Source=C:\Users\pax77\AppData\Local\sc2dsstats_desktop\data_v4_0.db",
            x =>
            {
                x.MigrationsAssembly("sc2dsstats.app");
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            }
            )
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    });
}

var serviceProvider = services.BuildServiceProvider();
var context = serviceProvider.GetService<sc2dsstatsContext>();
var localContext = serviceProvider.GetService<localContext>();

if (context != null)
{
    bool success = true;
    if (success)
        success = NameService.CreateJson(context, localContext);
    if (success)
        success = BuildService.Build();
}

public partial class Program
{
    public static ILogger<Program>? logger = ApplicationLogging.CreateLogger<Program>();
}
