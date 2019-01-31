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

namespace sc2dsstats
{
    /// <summary>
    /// Interaktionslogik für Window4.xaml
    /// </summary>
    public partial class Window4 : Window
    {

        DateTime today = new DateTime();
        string today_stats = "";
        string store_path = "";
        public Image myImage = null;
        public Task watcherThread = null;
        public CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        Task watcherTask = null;
        string replay_path = "";
        bool doit = false;

        public Window4()
        {
            InitializeComponent();
            var appSettings = ConfigurationManager.AppSettings;
            replay_path = appSettings["replay_path"];
            dp_onthefly_startdate.SelectedDate = DateTime.Today;
        }

        private Image CreateViewImageDynamically(string imgPath)
        {
            // Create Image and set its width and height  
            Image dynamicImage = new Image();
            dynamicImage.Stretch = Stretch.Fill;
            dynamicImage.StretchDirection = StretchDirection.Both;
            dynamicImage.MaxWidth = 1600;
            dynamicImage.MaxHeight = 1660;
            dynamicImage.MouseDown += new System.Windows.Input.MouseButtonEventHandler(dyn_image_Click);

            dynamicImage.AllowDrop = true;
            dynamicImage.MouseMove += new MouseEventHandler(dyn_image_Move);

            /**
            dynamicImage.MouseDown += new System.Windows.Input.MouseEventHandler(this.ListDragSource_MouseDown);
            dynamicImage.QueryContinueDrag += new System.Windows.Input.QueryCursorEventHandler(this.ListDragSource_QueryContinueDrag);
            dynamicImage.MouseUp += new System.Windows.Input.MouseEventHandler(this.ListDragSource_MouseUp);
            dynamicImage.MouseMove += new System.Windows.Input.MouseEventHandler(this.ListDragSource_MouseMove);
            **/



            // Create a BitmapSource  
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(@imgPath);
            bitmap.EndInit();

            // Set Image.Source  
            dynamicImage.Source = bitmap;

            // Add Image to Window  
            /// stackpanel1.Children.Add(dynamicImage);
            /// 

            ContextMenu cm = new ContextMenu();
            MenuItem saveas = new MenuItem();
            saveas.Header = "Save as ...";
            cm.Items.Add(saveas);
            saveas.Click += new System.Windows.RoutedEventHandler(SaveAs_Click);




            dynamicImage.ContextMenu = cm;





            return dynamicImage;
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {

            string dps_png = myImage.Source.ToString();
            dps_png = new Uri(dps_png).LocalPath;
            BitmapImage bitmap = new BitmapImage();

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PNG Image|*.png";
            saveFileDialog1.Title = "Save PNG Image File";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(@dps_png);
                bitmap.EndInit();

                // Save the bitmap into a file.
                using (FileStream stream =
                    new FileStream(saveFileDialog1.FileName, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                }


            }
        }

        private void dyn_image_Click(object sender, MouseEventArgs e)
        {
            /// MessageBox.Show("Und es war SOmmer");

            if (e is MouseEventArgs)
            {

                if (e.RightButton == MouseButtonState.Released)
                {
                    Window1 win1 = new Window1();
                    BitmapImage bitmap = new BitmapImage();
                    string dps_png = myImage.Source.ToString();
                    dps_png = new Uri(dps_png).LocalPath;


                    win1.Title = dps_png;

                    /// System.Windows.Controls.Image pupdynImage = new System.Windows.Controls.Image();

                    var imageStream = File.OpenRead(@dps_png);
                    var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                    win1.Height = decoder.Frames[0].PixelHeight;
                    win1.Width = decoder.Frames[0].PixelWidth;






                    if (File.Exists(dps_png))
                    {


                        string targetPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        targetPath += "\\temp";

                        string fileName = "worker_tmp0.png";


                        // Use Path class to manipulate file and directory paths.
                        string sourceFile = dps_png;
                        string destFile = System.IO.Path.Combine(targetPath, fileName);

                        int i = 0;
                        while (File.Exists(destFile))
                        {
                            i++;
                            fileName = "worker_tmp" + i.ToString() + ".png";
                            destFile = System.IO.Path.Combine(targetPath, fileName);
                        }

                        // To copy a folder's contents to a new location:
                        // Create a new target folder, if necessary.
                        if (!System.IO.Directory.Exists(targetPath))
                        {
                            System.IO.Directory.CreateDirectory(targetPath);
                        }

                        // To copy a file to another location and 
                        // overwrite the destination file if it already exists.
                        System.IO.File.Copy(sourceFile, destFile, true);



                        bitmap.BeginInit();
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(@destFile);
                        bitmap.EndInit();
                        // Set Image.Source  
                        win1.win_dps_img1.Source = bitmap;
                        win1.win_dps_grid.Visibility = Visibility.Visible;

                    }
                    win1.win_dps_grid.Visibility = Visibility.Visible;

                    win1.Show();
                    ///win1.Close();



                }

                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    /**
                    /// save as
                
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.Filter = "PNG Image|*.png";
                    saveFileDialog1.Title = "Save PNG Image File";
                    saveFileDialog1.ShowDialog();

                    if (saveFileDialog1.FileName != "")
                    {
                        bitmap.BeginInit();
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(@dps_png);
                        bitmap.EndInit();

                        // Save the bitmap into a file.
                        using (FileStream stream =
                            new FileStream(saveFileDialog1.FileName, FileMode.Create))
                        {
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            encoder.Save(stream);
                        }


                    }
                    **/
                }
            }
        }

        private void dyn_image_Move(object sender, MouseEventArgs e)
        {

            BitmapImage dropBitmap = new BitmapImage();

            try
            {
                Image dropImage = sender as Image;
                string drop = dropImage.Source.ToString();
                drop = new Uri(drop).LocalPath;


                dropBitmap.BeginInit();
                dropBitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                dropBitmap.CacheOption = BitmapCacheOption.OnLoad;
                dropBitmap.UriSource = new Uri(drop);
                dropBitmap.EndInit();

                string[] files = new string[1];
                BitmapImage[] dBitmaps = new BitmapImage[1];
                files[0] = drop;
                dBitmaps[0] = dropBitmap;



                DataObject dropObj = new DataObject(DataFormats.FileDrop, files);
                ///dropObj.SetData(DataFormats.Text, files[0]);
                dropObj.SetData(DataFormats.Bitmap, dBitmaps[0]);

                if (myImage != null && e.LeftButton == MouseButtonState.Pressed)
                {


                    ///DragDrop.DoDragDrop(myImage, dps_png, DragDropEffects.Copy);
                    DragDrop.DoDragDrop(myImage, dropObj, DragDropEffects.Copy);

                }
            }
            catch (System.IO.FileNotFoundException)
            {

            }
            finally
            {
                dropBitmap = null;
            }
        }


        private void bt_onthefly_start_Click(object sender, RoutedEventArgs e)
        {

            doit = true;

            lb_onthefly.Content = "Watching ..";
            lb_onthefly.UpdateLayout();

            today = DateTime.Today;
            today_stats = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            store_path = today_stats + "\\analyzes";
            today_stats += "\\temp\\";

            if (!System.IO.Directory.Exists(today_stats))
            {
                System.IO.Directory.CreateDirectory(today_stats);
            }
            today_stats += "today_stats.csv";



            ScanReplays(replay_path);

            

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
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Änderungsereignisse
            ///MessageBox.Show(e.FullPath + " wurde geändert");
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {

            // Ereignisbehandlungsroutine für Erstellungsereignisse
            Task.Factory.StartNew(() => { ScanReplays(replay_path); }, TaskCreationOptions.AttachedToParent);

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
            today_stats = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            today_stats += "\\temp\\";

            string today_csv = today_stats + "today_stats.csv";
            string today_png = today_stats + "today_stats.png";

            CancellationToken ct = tokenSource2.Token;
            tokenSource2.Cancel();
            

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

        private void ScanReplays(string replay_path)
        {

            if (doit)
            {
                doit = false;
                System.Threading.Thread.Sleep(4000);

                Dispatcher.Invoke(() =>
                {

                    string logfile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    logfile += "\\log.txt";

                    DateTime end = DateTime.Today;
                    end = end.AddDays(2);
                    string sd = dp_onthefly_startdate.SelectedDate.Value.ToString("yyyyMMdd");
                    sd += "000000";
                    ///sd = "20190127000000";

                    string ed = end.ToString("yyyyMMdd");
                    ed += "000000";
                    string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    s_doit += "\\scripts\\sc2dsstats_scan.exe";
                    string ExecutableFilePath = s_doit;
                    string Arguments = "--start_date=" + sd + " "
                        + "--end_date=" + ed + " "
                        + "--stats_file=\"" + today_stats + "\" "
                        ;

                    List<string> files = new List<string>();

                    Process doit = new Process();

                    if (File.Exists(ExecutableFilePath))
                    {
                        doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                        doit.WaitForExit();

                    }

                    if (File.Exists(today_stats))
                    {


                        string today_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        today_png += "\\temp\\today_stats.png";


                        string s_doit2 = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        s_doit2 += "\\scripts\\sc2dsstats_worker.exe";

                        string alignment = "horizontal";
                        if (rb_otf_vertical.IsChecked == true)
                        {
                            alignment = "vertical";
                        }

                        string ExecutableFilePath2 = s_doit2;
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
                            + "--stats_file=\"" + today_stats + "\"";
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

                                myImage = CreateViewImageDynamically(today_png);
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
