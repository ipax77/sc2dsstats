using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace sc2dsstats_rc1
{
    class dsotf
    {
        public string REPLAY_PATH { get; set; }
        internal CancellationToken ct { get; set; }
        internal bool running { get; set; }
        internal CancellationTokenSource tokenSource2 { get; set; }
        public List<dsreplay> REPLAYS = new List<dsreplay>();
        public MainWindow MW { get; set; }
        internal string REPLAY { get; set; }
        internal Task task { get; set; }
        internal FileSystemWatcher watcher { get; set; }
        internal string DUMMY { get; set; }

        public dsotf()
        {
            running = false;
            tokenSource2 = new CancellationTokenSource();
            REPLAY = "bab";
            string dummy = REPLAY_PATH + "\\dummy.SC2Replay";
        }

        public dsotf(MainWindow mw) : this()
        {
            MW = mw;
        }

        public dsotf(MainWindow mw, string replay_path) : this(mw)
        {
            REPLAY_PATH = replay_path;
        }

        public bool Start()
        {
            DUMMY = REPLAY_PATH + "\\dummy.SC2Replay";
            if (File.Exists(DUMMY))
            {
                try
                {
                    File.Delete(DUMMY);
                }
                catch (System.IO.IOException)
                {

                }
            }

            tokenSource2 = new CancellationTokenSource();
            ct = tokenSource2.Token;
            if (running == false)
            {
                task = Task.Factory.StartNew(() => { startWatching(); }, tokenSource2.Token);
                running = true;
                MW.Dispatcher.Invoke(() => {
                    MW.lb_info.Text = "Watching for new replays ..";
                });
            }
            return running;
        }

        public bool Stop()
        {
            running = false;
            //watcher.EnableRaisingEvents = false;
            tokenSource2.Cancel();



            File.Create(DUMMY);
            Thread.Sleep(250);
            
            try
            {
                task.Wait();
                //File.Delete(dummy);
            } 
            catch 
            {

            }
            finally
            {
                tokenSource2.Dispose();
                watcher.Dispose();
                task.Dispose();
                MW.Dispatcher.Invoke(() => {
                    MW.lb_info.Text = "Watching for new replays stopped.";
                });
            }

            if (File.Exists(DUMMY))
            {
                try
                {
                    File.Delete(DUMMY);
                }
                catch (System.IO.IOException)
                {
                    MW.myTempfiles_col.Add(DUMMY);
                }
            }



            return running;
        }

        internal void ScanReplays(string replay)
        {
            if (replay == REPLAY)
            {
                // already running;
            }
            else if (replay == DUMMY)
            {
                // stop it somhow ..
            } else
            {
                REPLAY = replay;
                if (CheckAccess(replay))
                {

                    var appSettings = ConfigurationManager.AppSettings;
                    int cores = 2;
                    if (appSettings["CORES"] != null && appSettings["CORES"] != "0")
                    {
                        cores = int.Parse(appSettings["CORES"]);
                    }

                    string ExecutableFilePath = MW.myScan_exe;
                    string Arguments = @"--priority=" + "NORMAL" + " "
                                        + "--cores=" + cores.ToString() + " "
                                        + "--player=\"" + appSettings["PLAYER"] + "\" "
                                        + "--stats_file=\"" + MW.myStats_csv + "\" "
                                        + "--replay_path=\"" + appSettings["REPLAY_PATH"] + "\" "
                                        + "--DEBUG=" + appSettings["DEBUG"] + " "
                                        + "--keep=" + appSettings["KEEP"] + " "
                                        + "--store_path=\"" + appSettings["STORE_PATH"] + "\" "
                                        + "--skip_file=\"" + appSettings["SKIP_FILE"] + "\" "
                                        + "--log_file=\"" + MW.myScan_log + "\" "
                                        + "--s2_cli=\"" + MW.myS2cli_exe + "\" "
                                        + "--num_file=\"" + MW.myAppData_dir + "\\num.txt" + "\" "
                                       ;
                    Process doit = new Process();

                    if (File.Exists(ExecutableFilePath))
                    {
                        doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                        doit.WaitForExit();


                    }

                    MW.replays.Clear();
                    MW.replays = MW.LoadData(MW.myStats_csv);

                    MW.Dispatcher.Invoke(() =>
                    {
                        if (File.Exists(MW.myScan_log))
                        {
                            string log = "";
                            StreamReader reader = new StreamReader(MW.myScan_log, Encoding.UTF8, true);
                            log = "Log:" + Environment.NewLine;
                            byte[] bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

                            log += Encoding.Default.GetString(bytes);
                            reader.Close();
                            MW.lb_info.Text = log;
                        }

                        MW.UpdateGraph(null);
                    });


                }
            }
        }

        internal bool CheckAccess(string replay)
        {
            bool go = false;
            int attemptWaitMS = 250;

            Thread.Sleep(attemptWaitMS);

            if (File.Exists(replay)) {

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
                    catch (IOException ioEx)
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

        internal void startWatching ()
        {

            ct.ThrowIfCancellationRequested();

            watcher = new FileSystemWatcher();

            watcher.Path = REPLAY_PATH;
            watcher.Filter = "*.SC2Replay";
            watcher.IncludeSubdirectories = false;
            watcher.NotifyFilter = NotifyFilters.FileName;

            watcher.Changed += new FileSystemEventHandler(watcher_Changed);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.Deleted += new FileSystemEventHandler(watcher_Deleted);
            watcher.Renamed += new RenamedEventHandler(watcher_Renamed);

            watcher.EnableRaisingEvents = true;

            watcher.WaitForChanged(WatcherChangeTypes.All);

        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Änderungsereignisse
            ///MessageBox.Show(e.FullPath + " wurde geändert");
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Erstellungsereignisse
            Task.Factory.StartNew(() => { ScanReplays(e.FullPath); }, TaskCreationOptions.None);

        }

        private void watcher_Deleted(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Löschereignisse
            ///MessageBox.Show(e.FullPath + " wurde neu angelegt");
        }

        private void watcher_Renamed(object sender, RenamedEventArgs e)
        {

            // Ereignisbehandlungsroutine für Namensänderungen im Dateisystem
            ///MessageBox.Show(e.OldName + " heißt jetzt " + e.Name);
        }

    }



}
