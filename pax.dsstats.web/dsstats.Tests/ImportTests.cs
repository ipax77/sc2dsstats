﻿using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace dsstats.Tests;

[Collection("Sequential")]
public class ImportTests : IDisposable
{
    private readonly UploadService uploadService;
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ReplayContext> _contextOptions;

    public ImportTests(IMapper mapper)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<ReplayContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ReplayContext(_contextOptions);

        context.Database.EnsureCreated();

        //if (context.Database.EnsureCreated())
        //{
        //    using var viewCommand = context.Database.GetDbConnection().CreateCommand();
        //    viewCommand.CommandText = @"
        //        CREATE VIEW AllResources AS
        //        SELECT Url
        //        FROM Blogs;";
        //    viewCommand.ExecuteNonQuery();
        //}
        //context.AddRange(
        //    new Blog { Name = "Blog1", Url = "http://blog1.com" },
        //    new Blog { Name = "Blog2", Url = "http://blog2.com" });
        //context.SaveChanges();

        var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        //        ServiceLifetime.Transient);

        serviceCollection.AddDbContext<ReplayContext>(options =>
        {
            options.UseSqlite(_connection, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("SqliteMigrations");
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            .EnableDetailedErrors()
            .EnableDetailedErrors();
        });

        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();
        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        uploadService = new UploadService(serviceProvider, mapper, NullLogger<UploadService>.Instance);
    }

    ReplayContext CreateContext() => new ReplayContext(_contextOptions);
    public void Dispose() => _connection.Dispose();


    [Fact]
    public async Task UploadTest()
    {
        var appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BattleNetId = 12345,
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 12345
                },
                new PlayerUploadDto()
                {
                    Name = "xPax",
                    Toonid = 12346
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.BattleNetId == 12345);
        Assert.True(dbHasUploader);

        string testFile = "/data/ds/uploadtest.base64";

        Assert.True(File.Exists(testFile));
        var base64String = File.ReadAllText(testFile);

        await uploadService.ImportReplays(base64String, appGuid);

        var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        Assert.NotNull(uploader);
        Assert.True(uploader?.LatestUpload > DateTime.MinValue);

        await Task.Delay(5000);

        Assert.True(context.Replays.Any());

    }
}
