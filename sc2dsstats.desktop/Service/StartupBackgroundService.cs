using Microsoft.Extensions.Hosting;
using sc2dsstats.lib.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class StartupBackgroundService : BackgroundService
    {
        public StartupBackgroundService() { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                int i = 0;

                lock (DSdata.DesktopStatus)
                {
                    foreach (var directory in DSdata.Config.Replays)
                        foreach (var file in Directory.GetFiles(directory, "Direct Strike*.SC2Replay", SearchOption.AllDirectories))
                            i++;
                    DSdata.DesktopStatus.FoldersReplays = i;
                }
            });
        }
    }
}

