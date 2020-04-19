using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using paxgamelib.Data;
using paxgamelib.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace paxgamelib
{
    /// <summary>
    /// Main class
    /// </summary>
    public class paxgame
    {
        /// Modify Unit stats to adjust to the battlefield size (area damage size, knockback distance, cooldowns, ...)
        public static float Battlefieldmodifier { get; set; } = 4;
        
        /// Player income per round
        public static int Income { get; set; } = 500;

        public static int DEBUG { get; set; } = 0;
        public static Vector2 Buildareasize = new Vector2(20, 50);
        public static Vector2 Battleareasize = new Vector2(30, 120);

        public static ulong GameID { get; set; } = 1000;
        private static ulong _GameID;
        public static ulong PlayerID { get; set; } = 1000;
        private static ulong _PlayerID;

        public static readonly Dictionary<string, string> _dict =
            new Dictionary<string, string>
            {
                {"MemoryCollectionKey1", "value1"},
                {"MemoryCollectionKey2", "value2"}
            };


        private static ILogger _logger;

        public paxgame()
        {
            Init();
        }

        public paxgame(bool init)
        {
            
        }

        /// <summary>
        /// Initialize required data - call it after changeing any parameters of this
        /// </summary>
        public static void Init()
        {
            _GameID = GameID;
            _PlayerID = PlayerID;
            InitLogs();
            InitUnits();
        }

        static void InitLogs()
        {

            var serviceCollection = new ServiceCollection();
            if (DEBUG > 0)
                ConfigureServices(serviceCollection, true);
            else
                ConfigureServices(serviceCollection, false);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _logger = serviceProvider.GetService<ILogger<paxgame>>();

            UnitService._logger = _logger;
            MoveService._logger = _logger;
            AbilityService._logger = _logger;
            FightService._logger = _logger;

            _logger.LogInformation("Logger init done.");
        }

        static void InitUnits()
        {
            UpgradePool.Init();
            AbilityPool.Init();
            UnitPool.Init();
            AbilityPool.PoolInit();
        }

        public static ulong GetGameID()
        {
            return ++_GameID;
        }

        public static ulong GetPlayerID()
        {
            return ++_PlayerID;
        }

        private static void ConfigureServices(IServiceCollection services, bool isDebug)
        {
            services.AddLogging(configure => configure.AddConsole())
                    .AddTransient<paxgame>();

            if (isDebug)
            {
                services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);
            }
            else
            {
                services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Error);
            }
        }

    }
}
