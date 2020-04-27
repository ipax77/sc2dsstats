using Microsoft.Extensions.Logging;
using sc2dsstats.lib.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class OnTheFlyScan
    {
        ConcurrentDictionary<Task, CancellationTokenSource> TASKS { get; set; } = new ConcurrentDictionary<Task, CancellationTokenSource>();
        ConcurrentDictionary<string, FileSystemWatcher> WATCHER { get; set; } = new ConcurrentDictionary<string, FileSystemWatcher>();
        ObservableCollection<string> TODO { get; set; }
        ConcurrentBag<string> fsBag = new ConcurrentBag<string>();
        
        public int fsCount = 0;
        public bool Running = false;
        private Status _status;

        Regex rx_ds = new Regex(@"(Direct Strike.*)\.SC2Replay$|(DST.*)\.SC2Replay$", RegexOptions.Singleline);
        private readonly ILogger _logger;

        public OnTheFlyScan(ILogger<OnTheFlyScan> logger)
        {
            _logger = logger;
        }

        public void Start(Status status)
        {
            _logger.LogInformation("otf start request.");
            if (Running == true) return;
            Running = true;
            _status = status;
            TODO = new ObservableCollection<string>();
            TODO.CollectionChanged += Source_CollectionChanged;
            TASKS = new ConcurrentDictionary<Task, CancellationTokenSource>();
            WATCHER = new ConcurrentDictionary<string, FileSystemWatcher>();

            foreach (string path in DSdata.Config.Replays)
            {
                if (Directory.Exists(path))
                {
                    CancellationTokenSource tokenSource = new CancellationTokenSource();
                    CancellationToken token = tokenSource.Token;
                    Task task = Task.Factory.StartNew(() =>
                    {
                        FileSystemWatcher fsw = null;
                        while (!token.IsCancellationRequested)
                        {
                            fsw = MonitorDirectory(path);
                        }
                        if (token.IsCancellationRequested)
                        {
                            //fsw.EnableRaisingEvents = false;
                            //fsw.Dispose();
                            //fsw = null;
                            //token.ThrowIfCancellationRequested();
                        }
                    }, token);
                    TASKS.TryAdd(task, tokenSource);
                }
            }
            _logger.LogInformation("otf start.");
        }

        public void Stop()
        {
            _logger.LogInformation("otf stop request.");
            TODO.CollectionChanged -= Source_CollectionChanged;
            TODO.Clear();
            fsBag.Clear();
            foreach (string path in WATCHER.Keys)
            {
                WATCHER[path].EnableRaisingEvents = false;
                WATCHER[path].Dispose();
                WATCHER[path] = null;
            }

            WATCHER.Clear();
            WATCHER = null;

            foreach (Task task in TASKS.Keys)
            {
                try
                {
                    TASKS[task].Cancel();
                    //task.Wait();
                    TASKS[task].Dispose();
                }
                catch (AggregateException ex)
                {
                    _logger.LogError("Task cancel failed :( {0}", ex.InnerExceptions[0].Message);
                }
            }

            TASKS.Clear();
            TASKS = null;
            Running = false;
            _logger.LogInformation("otf stop.");
        }

        private FileSystemWatcher MonitorDirectory(string path)
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path);
            fileSystemWatcher.IncludeSubdirectories = false;
            //fileSystemWatcher.Path = path;

            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            fileSystemWatcher.EnableRaisingEvents = true;

            //fileSystemWatcher.Created += FileSystemWatcher_Created;
            //fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            //fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            if (WATCHER != null && !WATCHER.ContainsKey(path)) WATCHER.TryAdd(path, fileSystemWatcher);
            fileSystemWatcher.WaitForChanged(WatcherChangeTypes.All);
            return fileSystemWatcher;
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.fff")} File renamed: {e.OldName} => {e.Name}");
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.fff")} File created: {e.Name}");
        }


        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.fff")} File changed: {e.Name}");
            lock (fsBag)
            {
                if (!fsBag.Contains(e.Name))
                {
                    fsBag.Add(e.Name);
                    return;
                }
            }

            if (rx_ds.IsMatch(e.Name))
            {
                if (TODO != null)
                {
                    lock (TODO)
                    {
                        TODO.Add(e.FullPath);
                    }
                }
                else
                {
                    FileSystemWatcher temp = sender as FileSystemWatcher;
                    temp.EnableRaisingEvents = false;
                }
            }
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.fff")} File deleted: {e.Name}");


        }

        private bool CheckAccess(string replay)
        {
            bool go = false;
            int attemptWaitMS = 250;

            /*
            if (fsString == replay)
                fsString = String.Empty;
            else
            {
                fsString = replay;
                return go;
            }
            */

            Thread.Sleep(attemptWaitMS);

            if (File.Exists(replay))
            {
                FileStream fs = null;
                int attempts = 0;
                int maximumAttempts = 14;

                // Loop allow multiple attempts
                while (true)
                {
                    try
                    {
                        fs = File.Open(replay, FileMode.Open, FileAccess.Read, FileShare.None);

                        //If we get here, the File.Open succeeded, so break out of the loop and return the FileStream
                        break;
                    }
                    catch
                    {
                        // IOExcception is thrown if the file is in use by another process.

                        // Check the numbere of attempts to ensure no infinite loop
                        attempts++;
                        if (attempts > maximumAttempts)
                        {
                            // Too many attempts,cannot Open File, break and return null 
                            fs = null;
                            break;
                        }
                        else
                        {
                            // Sleep before making another attempt
                            Thread.Sleep(attemptWaitMS);
                        }
                    }
                }
                if (fs != null)
                {
                    go = true;
                    fs.Close();
                    fs = null;
                }
            }
            
            return go;
        }

        void Source_CollectionChanged(object aSender, NotifyCollectionChangedEventArgs aArgs)
        {
            string todo = TODO.FirstOrDefault();
            if (!String.IsNullOrEmpty(todo))
            {
                _logger.LogInformation($"New replay detected: {todo}");
                _status.DecodeReplays(todo);
                TODO.Clear();
            }
        }
    }
}
