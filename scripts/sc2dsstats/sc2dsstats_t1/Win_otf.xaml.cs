using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using sc2dsstats_t1;

namespace sc2dsstats
{
    /// <summary>
    /// Interaktionslogik für Win_otf.xaml
    /// </summary>
    public partial class Win_otf : Window
    {
        public Image myImage = null;
        public Task watcherThread = null;
        public CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        Task watcherTask = null;
        string replay_path = "";
        bool doit = false;
        bool running = false;
        MainWindow mw = new MainWindow();
        string myTemp_dir = null;
        string today_csv = null;
        string today_png = null;
        DateTime end = DateTime.Today;
        
        public Win_otf()
        {
            InitializeComponent();

            myTemp_dir = mw.GetmyVAR("myTemp_dir") + "\\today\\";
            if (!System.IO.Directory.Exists(myTemp_dir))
            {
                System.IO.Directory.CreateDirectory(myTemp_dir);
            }
            today_csv = myTemp_dir + "today_stats.csv";
            today_png = myTemp_dir + "today_stats.png";

            var appSettings = ConfigurationManager.AppSettings;
            replay_path = appSettings["replay_path"];
            dp_onthefly_startdate.SelectedDate = DateTime.Today;
            end = end.AddDays(2);
        }
   
        private void bt_onthefly_start_Click(object sender, RoutedEventArgs e)
        {
            

            doit = true;

            lb_onthefly.Content = "Watching ..";
            lb_onthefly.UpdateLayout();

            if (running == false)
            {
                ScanReplays(replay_path, "start");
            } else
            {
                MessageBox.Show("Already watching.");
            }

            

            ///MessageBox.Show("Klicken Sie hier, um die Überwachung zu beenden!", "Überwachung");


        }

        internal void startWatching (string replay_path, CancellationToken ct)
        {

            if (ct.IsCancellationRequested == true)
            {
                ct.ThrowIfCancellationRequested();
            }

            FileSystemWatcher watcher = new FileSystemWatcher();

            watcher.Path = replay_path;
            watcher.Filter = "*.SC2Replay";
            watcher.IncludeSubdirectories = false;
            watcher.NotifyFilter = NotifyFilters.FileName;

            watcher.Changed += new FileSystemEventHandler(watcher_Changed);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.Deleted += new FileSystemEventHandler(watcher_Deleted);
            watcher.Renamed += new RenamedEventHandler(watcher_Renamed);

            watcher.EnableRaisingEvents = true;

            bool True = true;
            while (True)
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    True = false;
                }
                watcher.WaitForChanged(WatcherChangeTypes.All);
                True = false;

            }
            running = false;

        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Änderungsereignisse
            ///MessageBox.Show(e.FullPath + " wurde geändert");
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Erstellungsereignisse
            Task.Factory.StartNew(() => { ScanReplays(replay_path, "watcher"); }, TaskCreationOptions.AttachedToParent);

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

        private void bt_onthefly_stop_Click(object sender, RoutedEventArgs e)
        {
            doit = false;
            lb_onthefly.Content = "Stoped.";
            lb_onthefly.UpdateLayout();

            CancellationToken ct = tokenSource2.Token;
            tokenSource2.Cancel();

            running = false;

            if (File.Exists(today_csv))
            {
                try
                {
                    File.Delete(today_csv);
                }
                catch (System.IO.IOException)
                {

                }
            }

            if (File.Exists(today_png)) {
                try
                {
                    File.Delete(today_png);
                }
                catch (System.IO.IOException)
                {

                }
            }


        }

        private void ScanReplays(string replay_path, string caller)
        {

            if (doit)
            {
                doit = false;
                running = true;

                if (String.Equals(caller, "watcher"))
                {
                    System.Threading.Thread.Sleep(4000);
                }

                Dispatcher.Invoke(() =>
                {

                    string logfile = myTemp_dir + "\\log.txt";


                    string sd = dp_onthefly_startdate.SelectedDate.Value.ToString("yyyyMMdd");
                    sd += "000000";
                    ///sd = "20190127000000";

                    string ed = end.ToString("yyyyMMdd");
                    ed += "000000";
                    string ExecutableFilePath = mw.GetmyVAR("myScan_exe");
                    string Arguments = "--start_date=" + sd + " "
                        + "--end_date=" + ed + " "
                        + "--stats_file=\"" + today_csv + "\" "
                        ;

                    List<string> files = new List<string>();

                    Process doit = new Process();

                    if (File.Exists(ExecutableFilePath))
                    {
                        doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                        doit.WaitForExit();

                    }

                    if (File.Exists(today_csv))
                    {

                        string alignment = "horizontal";
                        if (rb_otf_vertical.IsChecked == true)
                        {
                            alignment = "vertical";
                        }

                        string ExecutableFilePath2 = mw.GetmyVAR("myWorker_exe");
                        string Arguments2 = "--start_date=" + sd + " "
                            + "--end_date=" + ed + " "
                            + "--skip STD=0 "
                            + "--player_only "
                            + "--png=\"" + today_png + "\" "
                            + "--skip DURATION=0 "
                            + "--skip LEAVER=0 "
                            + "--skip KILLSUM=0 "
                            + "--skip INCOME=0 "
                            + "--skip ARMY=0 "
                            + "--alignment=" + alignment + " "
                            + "--stats_file=\"" + today_csv + "\"";
                        ;

                        Process doit2 = new Process();

                        if (File.Exists(ExecutableFilePath2))
                        {
                            doit2 = System.Diagnostics.Process.Start(ExecutableFilePath2, Arguments2);
                            doit2.WaitForExit();

                        }


                        if (File.Exists(today_png))
                        {

                            /// bool indahouse = false;
                            /// Dispatcher.Invoke(new Action<Grid>(Grid => indahouse = gr_onthefly.Children.Contains(myImage)), gr_onthefly);

                            if (gr_onthefly.Children.Contains(myImage))

                            {

                                myImage.Source = null;
                                gr_onthefly.Children.Remove(myImage);

                                // Create a BitmapSource  
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.UriSource = new Uri(today_png);
                                bitmap.EndInit();

                                // Set Image.Source  
                                myImage.Source = bitmap;
                                lb_onthefly.Content = today_png;
                                lb_onthefly.UpdateLayout();
                                gr_onthefly.Children.Add(myImage);

                            }
                            else
                            {

                                myImage = mw.CreateViewImageDynamically(today_png);
                                lb_onthefly.Content = today_png;
                                lb_onthefly.UpdateLayout();
                                gr_onthefly.Children.Add(myImage);
                                

                            }

                        }

                    }
                });
                doit = true;
                CancellationToken ct = tokenSource2.Token;
                watcherTask = Task.Factory.StartNew(() => { startWatching(replay_path, ct); }, tokenSource2.Token);
            }
        }

        private void win_onthefly_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            doit = false;
            RoutedEventArgs ne = null;
            bt_onthefly_stop_Click(sender, ne);
            tokenSource2.Dispose();
        }
    }
}
