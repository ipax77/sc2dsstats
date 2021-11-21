using Microsoft.Extensions.Logging;

namespace sc2dsstats.decode
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = ApplicationLogging.CreateLogger<Program>();
            logger.LogInformation("Running ...");

            DecodeService decodeService = new DecodeService();
            // NameService.Init();

            List<string> replays = new List<string>()
            {
                @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3870).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3871).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3872).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3873).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3874).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3875).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3876).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3877).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3878).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3860).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3861).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3862).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3863).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3864).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3865).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3866).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3867).SC2Replay",
                //@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (3868).SC2Replay",
            };
            CancellationTokenSource source = new CancellationTokenSource();
            decodeService.DecodeReplays("", replays, 1, source.Token);


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
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                }).SetMinimumLevel(LogLevel.Information);
            });

            public static ILogger<T> CreateLogger<T>() => LogFactory.CreateLogger<T>();
        }
    }
}
