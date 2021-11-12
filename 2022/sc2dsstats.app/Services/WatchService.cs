using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.app.Services
{
    public class WatchService : IDisposable
    {
        public event EventHandler<FileSystemEventArgs> NewFileDetected;
        private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private HashSet<string> filesDetected;
        private object lockobject = new object();
        private const int maxAccessChecks = 15;
        private const int sleepTime = 250;
        public bool isWatching = false;
        private readonly ILogger logger;

        public WatchService()
        {
            logger = ApplicationLogging.CreateLogger("WatchService");
        }

        protected virtual void OnFileDetected(FileSystemEventArgs e)
        {
            EventHandler<FileSystemEventArgs> handler = NewFileDetected;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void Watch(List<string> replayPaths)
        {
            manualResetEvent.Reset();
            filesDetected = new HashSet<string>();
            isWatching = true;
            logger.LogInformation("Watcher starting ...");
            foreach (var path in replayPaths)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    CreateWatcher(path);
                });
            }
        }

        public void Stop()
        {
            isWatching = false;
            manualResetEvent.Set();
        }

        public void CreateWatcher(string path)
        {
            using var watcher = new FileSystemWatcher(path);
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
            watcher.Changed += Watcher_Changed;
            watcher.Filter = "*.SC2Replay";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            logger.LogInformation($"watching {path}");
            manualResetEvent.WaitOne();
            watcher.Dispose();
            logger.LogInformation($"stopped watching {path}");
        }

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (lockobject)
            {
                if (filesDetected.Contains(e.FullPath))
                    return;
                filesDetected.Add(e.FullPath);
            }
            if (await CheckAccess(e.FullPath))
            {
                logger.LogInformation($"watcher file detected: {e.FullPath}");
                OnFileDetected(e);
            }
        }

        private async Task<bool> CheckAccess(string filePath)
        {
            FileStream fs = null;
            int attempts = 0;
            await Task.Delay(sleepTime);
            while (true)
            {
                try
                {
                    fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    break;
                }
                catch
                {
                    attempts++;
                    if (attempts > maxAccessChecks)
                    {
                        fs = null;
                        break;
                    }
                    else
                    {
                        await Task.Delay(sleepTime);
                    }
                }
            }
            if (fs != null)
            {
                fs.Close();
                fs = null;
                return true;
            }
            else
                return false;
        }

        public void Dispose()
        {
            manualResetEvent.Set();
        }
    }
}
