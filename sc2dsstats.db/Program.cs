﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Shared;
using sc2dsstats.db.Services;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace sc2dsstats.db
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = ApplicationLogging.CreateLogger<Program>();
            logger.LogInformation("Running ...");

            var services = new ServiceCollection();

            var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
            var config = json.GetProperty("ServerConfig");
            var connectionString = config.GetProperty("WSLConnectionString2").GetString();
            var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 34));

            bool dbToggle = false;

            if (dbToggle)
            {
                logger.LogInformation("--------------------- MYSQL -------------------");
                services.AddDbContext<sc2dsstatsContext>(options =>
                {
                    options.UseLoggerFactory(ApplicationLogging.LogFactory);
                    options.UseMySql(connectionString, serverVersion, p =>
                    {
                        p.EnableRetryOnFailure();
                        p.MigrationsAssembly("sc2dsstats.2022.Server");
                        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                    });
                });
            }
            else
            {
                logger.LogInformation("--------------------- SQLITE -------------------");
                services.AddDbContext<sc2dsstatsContext>(options =>
                {
                    options.UseLoggerFactory(ApplicationLogging.LogFactory);
                    options.UseSqlite(@"Data Source=C:\Users\pax77\AppData\Local\sc2dsstats_desktop\data_v4_1.db",
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
            var users = context.DSRestPlayers.Count();
            logger.LogInformation($"Rest user count: {users}");

            // PlayerService.GetPlayerStats(context, new List<string>() { "b33aef3fcc740b0d67eda3faa12c0f94cef5213fe70921d72fc2bfa8125a5889" }).GetAwaiter().GetResult();
            // context.Database.ExecuteSqlRaw("RESET QUERY CACHE");
            // context.Database.ExecuteSqlRaw("SET SESSION query_cache_type=0;");

            DSData.Init();
            NameService.Init(context, Assembly.GetExecutingAssembly().Location).GetAwaiter().GetResult();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int winMax = 0;
            int losMax = 0;

            int wins = 0;
            int loss = 0;

            var ents = context.Dsreplays
                .Include(i => i.Dsplayers)
                .OrderBy(o => o.Gametime)
                .SelectMany(s => s.Dsplayers).Where(x => x.isPlayer)
                .Select(s => s.Win)
                .ToList()
                ;

            bool current = true;
            for (int i = 0; i < ents.Count; i++)
            {
                if (ents[i] == current)
                {
                    if (current)
                    {
                        wins++;
                    }
                    else
                    {
                        loss++;
                    }
                } else 
                {
                    if (current && wins > winMax)
                    {
                        winMax = wins;
                    } 
                    else if (!current && loss > losMax)
                    {
                        losMax = loss;
                    }

                    wins = 0;
                    loss = 0;
                }
                current = ents[i];
            }
            Console.WriteLine($"Replays: {ents.Count}");
            Console.WriteLine($"Winstreak max: {winMax}");
            Console.WriteLine($"Loosesreak max: {losMax}");
            Console.WriteLine($"Winrate: {Math.Round(ents.Count(c => c == true) * 100.0 / (double)ents.Count, 2)}");

            // logger.LogInformation($"replays: {context.Dsreplays.Count()}");

            // var replays = context.Dsreplays
            //     .Include(i => i.Dsplayers).ThenInclude(j => j.PlayerName)
            //     .Where(x => x.DefaultFilter)
            //     .OrderBy(o => o.Gametime)
            //     .AsNoTracking()
            //     // .Take(40000)
            //     .ToList();

            //Dictionary<string, EloPlayer> eloPlayers = new Dictionary<string, EloPlayer>();
            //foreach (var replay in replays)
            //{
            //    HashSet<EloPlayer> team1 = new HashSet<EloPlayer>();
            //    HashSet<EloPlayer> team2 = new HashSet<EloPlayer>();
            //    foreach (var pl in replay.Dsplayers)
            //    {
            //        string name = pl.PlayerName == null ? pl.Name : pl.PlayerName.Name;
            //        if (name.StartsWith("player") || !eloPlayers.ContainsKey(name))
            //        {
            //            eloPlayers[name] = new EloPlayer()
            //            {
            //                Rating = 1500
            //            };
            //        }
            //        if (pl.Team == 0)
            //        {
            //            team1.Add(eloPlayers[name]);
            //        }
            //        else if (pl.Team == 1)
            //        {
            //            team2.Add(eloPlayers[name]);
            //        }
            //    }

            //    Result result = replay.Winner == 0 ? Result.Win : Result.Lose;

            //    EloGame eloGame = new EloGame(team1, team2, result);

            //    foreach (var eloPl in team1)
            //    {
            //        eloPl.Rating += eloGame.Team1Delta;
            //    }
            //    foreach (var eloPl in team2)
            //    {
            //        eloPl.Rating += eloGame.Team2Delta;
            //    }
            //}

            //foreach (var ent in eloPlayers.OrderBy(o => o.Value.Rating))
            //{
            //    Console.WriteLine($"{ent.Key} => {ent.Value.Rating} ({ent.Value.Games})");
            //}

            //var replays = context.Dsreplays
            //    .Include(i => i.Dsplayers)
            //    .AsNoTracking()
            //    .Where(x =>
            //        x.Gamemode == (byte)DSData.Gamemode.Standard
            //        && x.Gametime > new DateTime(2019, 1, 1)
            //        && x.Duration > 300
            //        && x.Maxleaver < 90
            //        && x.Playercount == 6
            //    );

            //string interest = "ZergZergZerg";
            //string[] iSplit = Regex.Split(interest, @"(?<!^)(?=[A-Z])");
            //byte[] races = iSplit.Select(s => (byte)DSData.GetCommander(s)).ToArray();

            // replays = replays.Where(x =>
            //              (x.Dsplayers.Where(s => s.Realpos == 1 && s.Race == races[0]).Any()
            //              && x.Dsplayers.Where(s => s.Realpos == 2 && s.Race == races[1]).Any()
            //              && x.Dsplayers.Where(s => s.Realpos == 3 && s.Race == races[2]).Any())
            //             ||
            //              (x.Dsplayers.Where(s => s.Realpos == 4 && s.Race == races[0]).Any()
            //              && x.Dsplayers.Where(s => s.Realpos == 5 && s.Race == races[1]).Any()
            //              && x.Dsplayers.Where(s => s.Realpos == 6 && s.Race == races[2]).Any())
            // );

            //List<DsResponseItem> teamResponses = new List<DsResponseItem>();
            //for (int p1 = 1; p1 < 4; p1++)
            //{
            //    for (int p2 = 1; p2 < 4; p2++)
            //    {
            //        for (int p3 = 1; p3 < 4; p3++)
            //        {
            //            var team1Replays = replays.Where(x =>
            //             (x.Dsplayers.Where(s => s.Realpos == 1 && s.Race == p1).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 2 && s.Race == p2).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 3 && s.Race == p3).Any())
            //             &&
            //            (x.Dsplayers.Where(s => s.Realpos == 4 && s.Race == races[0]).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 5 && s.Race == races[1]).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 6 && s.Race == races[2]).Any())
            //            );

            //            var team2Replays = replays.Where(x =>
            //             (x.Dsplayers.Where(s => s.Realpos == 4 && s.Race == p1).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 5 && s.Race == p2).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 6 && s.Race == p3).Any())
            //             &&
            //            (x.Dsplayers.Where(s => s.Realpos == 1 && s.Race == races[0]).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 2 && s.Race == races[1]).Any()
            //             && x.Dsplayers.Where(s => s.Realpos == 3 && s.Race == races[2]).Any())
            //            );

            //            var team1Results = from r in team1Replays
            //                               group r by r.Winner into g
            //                               select new
            //                               {
            //                                   Winner = g.Key,
            //                                   Count = g.Count(),
            //                                   duration = g.Sum(s => s.Duration),
            //                               };
            //            var team2Results = from r in team2Replays
            //                               group r by r.Winner into g
            //                               select new
            //                               {
            //                                   Winner = g.Key,
            //                                   Count = g.Select(s => s.Id).Distinct().Count(),
            //                                   duration = g.Sum(s => s.Duration),
            //                               };

            //            var t1 = team1Results.AsNoTracking().ToList();
            //            var t2 = team2Results.AsNoTracking().ToList();
            //            int count = t1.Sum(s => s.Count) + t2.Sum(s => s.Count);
            //            teamResponses.Add(new DsResponseItem()
            //            {
            //                Label = ((DSData.Commander)p1).ToString() + ((DSData.Commander)p2).ToString() + ((DSData.Commander)p3).ToString(),
            //                Count = count,
            //                Wins = t1.Where(x => x.Winner == 0).Sum(s => s.Count) + t2.Where(x => x.Winner == 1).Sum(s => s.Count),
            //                duration = t1.Sum(s => s.duration) + t2.Sum(s => s.duration),
            //                Replays = count,
            //            });

            //        }
            //    }
            //}


            //foreach (var teamResponse in teamResponses.OrderBy(o => o.Winrate))
            //{
            //    logger.LogInformation($"{teamResponse.Label} => {teamResponse.Winrate}% ({teamResponse.Count})");
            //}


            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
            Console.ReadLine();
        }

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

        public static void SetMengskTestReplaysDefaultToFalse(sc2dsstatsContext context)
        {
            var replays = ReplayFilter.DefaultFilter(context);
            var testReplays = from r in replays
                              from p in r.Dsplayers
                              where p.Race == (byte)DSData.Commander.Mengsk && r.Gametime < new DateTime(2020, 07, 28, 5, 23, 0)
                              select r;
            foreach (var replay in testReplays)
            {
                replay.DefaultFilter = false;
            }
            context.SaveChanges();
        }

        public static void GetTimeResultsAsJson(sc2dsstatsContext context, ILogger logger)
        {
            var replays = ReplayFilter.DefaultFilter(context);

            var results = replays
                .Include(i => i.Dsplayers)
                .Select(s => UploadService.GetTimeResultValues(s)).ToList().SelectMany(s => s).ToList();

            logger.LogInformation($"Got results: {results.Count} - serializing ...");
            File.WriteAllText("/data/results.json", JsonSerializer.Serialize(results, new JsonSerializerOptions() { WriteIndented = false }));
        }

        public static void ResetTimeResultsfromJson(sc2dsstatsContext context, ILogger logger)
        {
            var results = JsonSerializer.Deserialize<List<DsTimeResultValue>>(File.ReadAllText("/data/results.json"));
            logger.LogInformation($"Got {results.Count} from json file.");

            var timeresults = context.DsTimeResults.ToList();
            timeresults.ForEach(f => { f.Count = 0; f.Wins = 0; f.MVP = 0; f.Duration = 0; f.Kills = 0; f.Army = 0; });
            context.SaveChanges();

            var participants = context.Participants.ToList();
            participants.ForEach(f => { f.Count = 0; f.Wins = 0; });
            context.SaveChanges();

            UploadService.CollectTimeResults2(context, logger, results);
        }
    }
}
