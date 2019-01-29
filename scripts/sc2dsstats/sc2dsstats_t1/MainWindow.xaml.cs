using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using sc2dsstats;
using Microsoft.Win32;
using System.Collections;
using System.Windows.Input;

namespace sc2dsstats_t1
{

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public Image myImage = null;
        public TextBox dynamicText = null;
        private bool dt_handle = true;
        private bool dps_handle = true;
        ArrayList imgGarbage = new ArrayList();


        public MainWindow()
        {
            InitializeComponent();

            /// First run?
            var appSettings = ConfigurationManager.AppSettings;


            if (string.Equals(appSettings["FIRST_RUN"], "1"))
            {


                /// MessageBox.Show("Welcome to sc2dsstats - this is your first run so we need to know some things to make this work ..");
                fr_InputBox.Visibility = System.Windows.Visibility.Visible;
                gr_buttons.Visibility = Visibility.Collapsed;
                gr_menu.Visibility = Visibility.Collapsed;





            }

            string[] cmdrs = new string[]
{
                "Abathur",
                 "Alarak",
                 "Artanis",
                 "Dehaka",
                 "Fenix",
                 "Horner",
                 "Karax",
                 "Kerrigan",
                 "Nova",
                 "Raynor",
                 "Stukov",
                 "Swann",
                 "Tychus",
                 "Vorazun",
                 "Zagara",
                 "Protoss",
                 "Terran",
                 "Zerg"
};
            foreach (string cmdr in cmdrs)
            {
                dps_ComboBox.Items.Add(cmdr);
                dt_ComboBox.Items.Add(cmdr);
            }

        }

        private void FirstRun()
        {
            string dir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            string drive = "C:\\";

            Hashtable dsreplays = new Hashtable();


            Regex rx = new Regex(@"(\w:\\)");

            MatchCollection matches = rx.Matches(dir);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                    drive = match.Value.ToString();
            }

            /// MessageBox.Show(drive);

            string Laufwerksbuchstabe = drive;
            DriveInfo[] Drives = DriveInfo.GetDrives();
            DriveInfo Drive = Drives.Where(x => x.Name == Laufwerksbuchstabe).SingleOrDefault();

            /// MessageBox.Show(Drive.TotalFreeSpace.ToString());
            double fs = Drive.TotalFreeSpace;

            var appSettings = ConfigurationManager.AppSettings;

            int i = 0;
            int newrep = 0;

            string csv = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            csv += "\\stats.csv";


            if (File.Exists(csv))
            {

                string line;
                string pattern = @"^(\d+); ([^;]+);";
                ///string pattern = @"^(\d+);";
                System.IO.StreamReader file = new System.IO.StreamReader(csv);
                while ((line = file.ReadLine()) != null)
                {
                    foreach (Match m in Regex.Matches(line, pattern))
                    {
                        string value1 = m.Groups[2].ToString() + ".SC2Replay";
                        if (dsreplays.ContainsKey(value1))
                        {


                        } else {
                            dsreplays.Add(value1, "1");

                        }
                    }
                }

                file.Close();
            }

            if (Directory.Exists(appSettings["REPLAY_PATH"]))
            {
                string[] replays = Directory.GetFiles(appSettings["REPLAY_PATH"]);
                foreach (string fileName in replays)
                {
                    ///string rx_id = @"(Direct Strike.*)\.SC2Replay$|(Desert Strike.*)\.SC2Replay$";
                    string rx_id = @"(Direct Strike.*)\.SC2Replay";
                    string rx_id2 = @"(Desert Strike.*)\.SC2Replay";

                    Match m = Regex.Match(fileName, rx_id, RegexOptions.IgnoreCase);
                    Match m2 = Regex.Match(fileName, rx_id2, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        i++;
                        if (dsreplays.ContainsKey(m.Value))
                        {
                        } else
                        {
                            newrep++;

                        }

                    }
                    if (m2.Success)
                    {
                        i++;
                        if (dsreplays.ContainsKey(m2.Value))
                        {
                        } else
                        {
                            newrep++;
                        }
                    }
                }
            }




            
            ///MessageBox.Show(i.ToString() + " new Replays found :)");
            
            doit_TextBox1.Text = "We found " + newrep.ToString() + " new Replays (total: " + i.ToString() + ")" + Environment.NewLine;

            double scalc = 6472659;
            double nsize = newrep * scalc;
            double time = newrep * 7.2;

            nsize /= 1024;
            nsize /= 1024;
            nsize /= 1024;

            fs /= 1024;
            fs /= 1024;
            fs /= 1024;

            time /= 60;
            time /= 60;

            string st_size = string.Format("{0:0.00}", nsize);
            string st_fs = string.Format("{0:0.00}", fs);
            string st_time = string.Format("{0:0.00}", time);

            doit_TextBox1.Text += Environment.NewLine;
            doit_TextBox1.Text += "Expected time needed: " + st_time + " h" + Environment.NewLine;

            if (String.Equals(appSettings["KEEP"], "1"))
            {
                doit_TextBox1.Text += "Expected disk space needed: " + st_size + " GB" + Environment.NewLine;
                doit_TextBox1.Text += "(Your current free disk space is " + st_fs + " GB)" + Environment.NewLine;


                if (nsize > fs)
                {
                    doit_TextBox1.Text += "WARNING: There might be not enough Diskspace available!!!" + Environment.NewLine;
                }
            }
            doit_TextBox1.Text += Environment.NewLine;

            doit_TextBox1.Text += "You can always quit the prozess, next time it will continue at the last position." + Environment.NewLine;

            /// 100 Replays =~ 647265846 Bytes, 720 sec


           

        }

        private void ib_BrowseButton_Click(object sender, RoutedEventArgs e)
        {

            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            /// MessageBox.Show("Thank you. Now we need to know where the SC2Replays are - please select one Replay in your folder. Usually it is something like C:\\Users\\<username>\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer");
            string filename = "unknown";
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".SC2Replay";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();



            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                filename = Path.GetDirectoryName(filename);
            }

            fr_InputTextBox2.Text = filename;

        }


        private void ib_OkButton_Click(object sender, RoutedEventArgs e)
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            fr_InputBox.Visibility = System.Windows.Visibility.Collapsed;

            // Do something with the Input
            String input = fr_InputTextBox.Text;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("PLAYER");
            config.AppSettings.Settings.Add("PLAYER", input);
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");

            // Clear InputBox.
            fr_InputTextBox.Text = String.Empty;

            string filename = fr_InputTextBox2.Text;


            config.AppSettings.Settings.Remove("REPLAY_PATH");
            config.AppSettings.Settings.Add("REPLAY_PATH", filename);
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");


            config.AppSettings.Settings.Remove("FIRST_RUN");
            config.AppSettings.Settings.Add("FIRST_RUN", "0");
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
            MessageBox.Show("Now we are good to go - have fun :) (There are more options available at File->Options)");

            gr_buttons.Visibility = Visibility.Visible;
            gr_menu.Visibility = Visibility.Visible;

        }

        private void ClearImage()
        {


            if (stackpanel1.Children.Contains(dynamicText))
            {
                dynamicText = null;
                stackpanel1.Children.Clear();
            }

            if (stackpanel1.Children.Contains(myImage))
            {
                myImage.Source = null;
                stackpanel1.Children.Remove(myImage);
            }
            if (otf_stats.Children.Contains(myImage))
            {
                myImage.Source = null;
                otf_stats.Children.Remove(myImage);
            }
            if (gr_details.Children.Contains(myImage))
            {
                myImage.Source = null;
                gr_details.Children.Remove(myImage);
            }
            if (gr_damage.Children.Contains(myImage))
            {
                myImage.Source = null;
                gr_damage.Children.Remove(myImage);
            }

            dp_config.Visibility = Visibility.Collapsed;
            otf_stats.Visibility = Visibility.Collapsed;
            doit_grid.Visibility = Visibility.Collapsed;
            gr_details.Visibility = Visibility.Collapsed;
            gr_damage.Visibility = Visibility.Collapsed;

            
           
            foreach (string img in imgGarbage) { 
                if (File.Exists(img))
                {
                    try
                    {
                        File.Delete(img);
                    }
                    catch (System.IO.IOException)
                    {

                    }
                }
            }
        }

        private Image CreateViewImageDynamically(string imgPath)
        {
            // Create Image and set its width and height  
            Image dynamicImage = new Image();
            dynamicImage.Stretch = Stretch.Fill;
            dynamicImage.StretchDirection = StretchDirection.Both;
            dynamicImage.Width = 1610;
            dynamicImage.Height = 610;
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


        private void CreateViewTextDynamically(string imgPath)
        {


            //                 < TextBox HorizontalAlignment = "Left" Height = "23" Margin = "652,12,0,0" TextWrapping = "Wrap" Text = "TextBox" VerticalAlignment = "Top" Width = "120" 
            //RenderTransformOrigin = "4.147,-2.623" Background = "#FF83868F" FontFamily = "Courier New" Cursor = "No" IsEnabled = "False" IsReadOnlyCaretVisible = "True" />


            dynamicText = new TextBox();

            dynamicText.Width = 1610;
            dynamicText.Height = 610;
            //dynamicText.Background = "#FF83868F";
            //dynamicText.FontFamily = "Courier New";
            dynamicText.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            dynamicText.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            dynamicText.TextWrapping = TextWrapping.Wrap;
            dynamicText.AcceptsReturn = true;
            dynamicText.Background = new LinearGradientBrush(Colors.LightBlue, Colors.SlateBlue, 90);
            dynamicText.FontFamily = new FontFamily("Courier New");





            stackpanel1.Children.Add(dynamicText);




        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
            doit_grid.Visibility = Visibility.Visible;
            FirstRun();




        }


        private void doit_Button_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();

            string logfile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            logfile += "\\log.txt";

            CreateViewTextDynamically(logfile);



            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_scan.exe";
            string ExecutableFilePath = s_doit;
            string Arguments = @" ";

            doit_TextBox1.Text += Environment.NewLine;
            doit_TextBox1.Text += "Processing Replays ..." + Environment.NewLine;

            List<string> files = new List<string>();

            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }

            if (File.Exists(logfile))
            {
                StreamReader reader = new StreamReader(logfile, Encoding.UTF8, true);


                dynamicText.Text = "";
                byte[] bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

                dynamicText.Text = Encoding.Default.GetString(bytes);


                reader.Close();

                /*
                foreach (string bab in files) {
                    dynamicText.Text += bab + Environment.NewLine;
                }
                */

                dynamicText.Focus();
                dynamicText.SelectionStart = dynamicText.Text.Length;
                dynamicText.ScrollToEnd();
            }
            else
            {
                MessageBox.Show("No logfile found :(");
            }






            /*
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = @"C:/temp/sc2_ds_stats";
            watcher.Filter = "log.txt";
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
            */

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            ClearImage();


            string stats = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            stats += "\\worker.png";

            if (File.Exists(stats))
            {
                myImage = CreateViewImageDynamically(stats);
                stackpanel1.Children.Add(myImage);
                label1.Text = stats;
                label1.UpdateLayout();

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }
        }

        /*
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

            ClearImage();

            string dps = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            dps += "\\dps.png";

            if (File.Exists(dps))
            {
                CreateViewImageDynamically(dps);
                label1.Text = dps;
                label1.UpdateLayout();

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }
        }
        */



        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ClearImage();
        }

        private void mnu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mnu_Log(object sender, RoutedEventArgs e)
        {
            Window2 win2 = new Window2();
            string logfile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            logfile += "\\log_worker.txt";

            if (File.Exists(logfile))
            {
                StreamReader reader = new StreamReader(logfile, Encoding.UTF8, true);


                win2.win_Log_Textbox_Log.Text = "";
                byte[] bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

                win2.win_Log_Textbox_Log.Text = Encoding.Default.GetString(bytes);
                reader.Close();

                win2.win_Log_Textbox_Log.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                win2.win_Log_Textbox_Log.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                win2.win_Log_Textbox_Log.TextWrapping = TextWrapping.Wrap;
                win2.win_Log_Textbox_Log.AcceptsReturn = true;
                win2.win_Log_Textbox_Log.Background = new LinearGradientBrush(Colors.LightBlue, Colors.SlateBlue, 90);
                win2.win_Log_Textbox_Log.FontFamily = new FontFamily("Courier New");

                win2.win_Log_Textbox_Log.Focus();
                win2.win_Log_Textbox_Log.SelectionStart = win2.win_Log_Textbox_Log.Text.Length;
                win2.win_Log_Textbox_Log.ScrollToEnd();
                win2.Show();
            } else
            {
                MessageBox.Show("No logfile found :(");
            }


        }

            private void mnu_Options(object sender, RoutedEventArgs e)
        {

            ClearImage();

            ///MessageBox.Show("Options");
            var appSettings = ConfigurationManager.AppSettings;
            dataGrid1.ItemsSource = appSettings.Keys;

            dataGrid1.ItemsSource = LoadCollectionData();

            var column = dataGrid1.Columns[0];

            // Clear current sort descriptions
            dataGrid1.Items.SortDescriptions.Clear();

            // Add the new sort description
            dataGrid1.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));

            // Apply sort
            foreach (var col in dataGrid1.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = ListSortDirection.Ascending;

            // Refresh items to display sort
            dataGrid1.Items.Refresh();

            ///InitializeComponent();

            ///dp_config.Visibility = Visibility.Visible;

            Window3 win3 = new Window3();
            win3.Show();

        }

        private void bt_save_Click(object sender, RoutedEventArgs e)
        {

            string aba = "bab" + Environment.NewLine;

            dataGrid1.SelectAll();

            Int32 selectedCellCount = dataGrid1.SelectedItems.Count;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = ConfigurationManager.AppSettings;
            for (int i = 0; i < selectedCellCount; i++)
            {

                string row = "bab";
                row = dataGrid1.SelectedItems[i].ToString();
                if (string.Equals("{NewItemPlaceholder}", row))
                {

                }
                else
                {

                    myConfig myCfg = (myConfig)dataGrid1.SelectedItems[i];

                    sb.Append(myCfg.key.ToString());
                    sb.Append(" => ");
                    sb.Append(myCfg.value.ToString());
                    sb.Append(Environment.NewLine);


                    if (String.Equals(myCfg.value.ToString(), appSettings[myCfg.key]))
                    {


                    }
                    else
                    {
                        config.AppSettings.Settings.Remove(myCfg.key);
                        config.AppSettings.Settings.Add(myCfg.key, myCfg.value);
                        config.Save();

                    }
                }
            }
            ConfigurationManager.RefreshSection("appSettings");

            MessageBox.Show(sb.ToString());


        }

        public class myConfig
        {
            public string key { get; set; }
            public string value { get; set; }
            public string info { get; set; }
        }

        private List<myConfig> LoadCollectionData()
        {
            List<myConfig> configs = new List<myConfig>();
            var appSettings = ConfigurationManager.AppSettings;
            string cdesc = "Und es war Sommer";

            foreach (var ckey in appSettings.AllKeys)
            {
                cdesc = "";
                if (String.Equals(ckey, "REPLAY_PATH"))
                    cdesc = "# Path where all the replays are located - IMPORTANT: You have to use / instead of \\";
                if (String.Equals(ckey, "PLAYER"))
                    cdesc = "# Starcraft 2 Player name (without Clan tags)";
                if (String.Equals(ckey, "SKIP_NORMAL"))
                    cdesc = "# If you want to skip stats for normal games (Zerg, Terran, Protoss) set this to 1";
                if (String.Equals(ckey, "SKIP"))
                    cdesc = "# Skip games with gametime below 240sec";
                if (String.Equals(ckey, "START_DATE"))
                    cdesc = "# start_date - only compute replays with timestamp greater than it - format YYYYmmDDHHMMSS";
                if (String.Equals(ckey, "END_DATE"))
                    cdesc = "# end_date - only compute replays with timestamp lower than it - format YYYYmmDDHHMMSS";
                if (String.Equals(ckey, "DEBUG"))
                    cdesc = "# Debug level (2=all, 1=info, 0=error only)";
                if (String.Equals(ckey, "SKIP_MSG"))
                    cdesc = "# Activate Skipmessage - If you type 'skipdsstats' in the ingame chat in the first 60sec of the game it will not be computed.";
                if (String.Equals(ckey, "DAILY"))
                    cdesc = "# On the fly stats (experimental)";

                configs.Add(new myConfig()
                {
                    key = ckey,
                    value = appSettings[ckey],
                    info = cdesc

                });
            }
            return configs;
        }

        private void otf_ShowButton_Click(object sender, RoutedEventArgs e)
        {

            ///otf_image.Source = null;

            string sd = otf_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            string ed = otf_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "000000";
            string skip_std = "1";

            if (otf_std.IsChecked == true)
            {
                skip_std = "0";
            }

            string otf_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            otf_png += "\\temp\\";

            if (!System.IO.Directory.Exists(otf_png))
            {
                System.IO.Directory.CreateDirectory(otf_png);
            }
            long msec = DateTime.Now.Ticks;
            otf_png += "sc2dsstats_stats_" + msec.ToString() + ".png";

            imgGarbage.Add(otf_png);

            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_worker.exe";

            string ExecutableFilePath = s_doit;
            string Arguments = "--start_date=" + sd + " "
                + "--end_date=" + ed + " "
                + "--skip STD=" + skip_std + " "
                + "--player_only "
                + "--png=\"" + otf_png + "\" "
                ;

            /**
                + "--alignment=" + alignment + " ";
            if (String.Equals(player_only, "1"))
            {
                Arguments += "--player_only ";

            }
            **/


            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }



            if (File.Exists(otf_png))
            {


                if (otf_stats.Children.Contains(myImage))
                {

                    myImage.Source = null;

                    // Create a BitmapSource  
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(@otf_png);
                    bitmap.EndInit();

                    // Set Image.Source  
                    myImage.Source = bitmap;
                    label1.Text = otf_png;
                    label1.UpdateLayout();
                    
                }
                else
                {


                    myImage = CreateViewImageDynamically(otf_png);
                    label1.Text = otf_png;
                    label1.UpdateLayout();
                    otf_stats.Children.Add(myImage);
                }

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }




        }

        private void bt_Time_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
            System.DateTime sd_t = new DateTime(2018, 11, 1);
            DateTime today = DateTime.Today;
            otf_enddate.SelectedDate = today.AddDays(1);
            otf_startdate.SelectedDate = sd_t;
            otf_stats.Visibility = Visibility.Visible;

            string sd = otf_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            string ed = otf_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "000000";
            string skip_std = "1";

            if (otf_std.IsChecked == true)
            {
                skip_std = "0";
            }

            string otf_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            otf_png += "\\temp\\";

            if (!System.IO.Directory.Exists(otf_png))
            {
                System.IO.Directory.CreateDirectory(otf_png);
            }
            long msec = DateTime.Now.Ticks;
            otf_png += "sc2dsstats_stats_" + msec.ToString() + ".png";
            imgGarbage.Add(otf_png);

            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_worker.exe";

            string ExecutableFilePath = s_doit;
            string Arguments = "--start_date=" + sd + " "
                + "--end_date=" + ed + " "
                + "--skip STD=" + skip_std + " "
                + "--player_only "
                + "--png=\"" + otf_png + "\" "
                ;

            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }


            if (File.Exists(otf_png))
            {


                if (otf_stats.Children.Contains(myImage))
                {

                    myImage.Source = null;

                    // Create a BitmapSource  
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(@otf_png);
                    bitmap.EndInit();

                    // Set Image.Source  
                    myImage.Source = bitmap;
                    label1.Text = otf_png;
                    label1.UpdateLayout();

                }
                else
                {


                    myImage = CreateViewImageDynamically(otf_png);
                    label1.Text = otf_png;
                    label1.UpdateLayout();
                    otf_stats.Children.Add(myImage);
                }

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }

        }

        private void bt_details_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
            System.DateTime sd = new DateTime(2018, 1, 1);
            DateTime today = DateTime.Today;
            dt_enddate.SelectedDate = today.AddDays(1);
            dt_startdate.SelectedDate = sd;



            dt_ComboBox.SelectedItem = dt_ComboBox.Items[0];
            gr_details.Visibility = Visibility.Visible;

        }

        private void dt_ComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            dt_handle = !cmb.IsDropDownOpen;
            Handle(sender, e);
        }

        private void dt_ComboBox_Closed(object sender, EventArgs e)
        {
            if (dt_ComboBox.SelectedItem == null)
            {
                dt_ComboBox.SelectedItem = dt_ComboBox.Items[0];
            }
            else
            {
                try
                {
                    if (dt_handle) Handle(sender, e);
                    dt_handle = true;
                }
                catch (InvalidCastException ex)
                {

                }
            }
        }

        private void Handle(object sender, EventArgs e)
        {
            RoutedEventArgs re = (RoutedEventArgs)e;

            dt_showButton_Click(sender, re);
        }


        private void dt_showButton_Click(object sender, RoutedEventArgs e)
        {

   
            ///otf_image.Source = null;

            string sd = dt_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            string ed = dt_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "000000";
            string skip_std = "1";

            if (dt_std.IsChecked == true)
            {
                skip_std = "0";
            }
            string player_only = "1";
            if (dt_player.IsChecked == false)
            {
                player_only = "0";
            }

            string alignment = "horizontal";
            if (dt_rb_vertical.IsChecked == true)
            {
                alignment = "vertical";
            }

            string dt_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            dt_png += "\\temp\\";

            if (!System.IO.Directory.Exists(dt_png))
            {
                System.IO.Directory.CreateDirectory(dt_png);
            }
            long msec = DateTime.Now.Ticks;
            dt_png += "sc2dsstats_details_" + msec.ToString() + ".png";
            imgGarbage.Add(dt_png);

            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_worker.exe";

            string ExecutableFilePath = s_doit;


            if (dt_ComboBox.SelectedItem == null)
            {
                dt_ComboBox.SelectedItem = dt_ComboBox.Items[0];
            }

            string cmdr = dt_ComboBox.Items[dt_ComboBox.SelectedIndex].ToString();


            string Arguments = "--start_date=" + sd + " "
                            + "--end_date=" + ed + " "
                            + "--skip STD=" + skip_std + " "
                            + "--alignment=" + alignment + " "
                            + "--cmdr=" + cmdr + " "
                            + "--png=\"" + dt_png + "\" "
                            ;
            if (String.Equals(player_only, "1"))
            {
                Arguments += "--player_only ";

            }
            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }

 
            if (File.Exists(dt_png))
            {

                if (gr_details.Children.Contains(myImage))
                {
                    // Create a BitmapSource  
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(@dt_png);
                    bitmap.EndInit();

                    // Set Image.Source  
                    myImage.Source = bitmap;
                    label1.Text = dt_png;
                    label1.UpdateLayout();
                }
                else
                {

                    myImage = CreateViewImageDynamically(dt_png);
                    label1.Text = dt_png;
                    label1.UpdateLayout();
                    gr_details.Children.Add(myImage);
                }

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }




        }

        private void bt_damage_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
            System.DateTime sd = new DateTime(2018, 1, 1);
            DateTime today = DateTime.Today;
            dps_enddate.SelectedDate = today.AddDays(1);
            dps_startdate.SelectedDate = sd;



            dps_ComboBox.SelectedItem = dps_ComboBox.Items[0];
            gr_damage.Visibility = Visibility.Visible;

        }


        private void dps_ComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            dps_handle = !cmb.IsDropDownOpen;
            dps_Handle(sender, e);
        }

        private void dps_ComboBox_Closed(object sender, EventArgs e)
        {
            if (dps_ComboBox.SelectedItem == null)
            {
                dps_ComboBox.SelectedItem = dps_ComboBox.Items[0];
            }
            else
            {
                try
                {
                    if (dt_handle) Handle(sender, e);
                    dt_handle = true;
                }
                catch (InvalidCastException ex)
                {

                }
            }
        }

        private void dps_Handle(object sender, EventArgs e)
        {
            RoutedEventArgs re = (RoutedEventArgs)e;

            dps_showButton_Click(sender, re);
        }


        private void dps_showButton_Click(object sender, RoutedEventArgs e)
        {

  
            ///otf_image.Source = null;

            string sd = dps_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            string ed = dps_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "000000";
            string skip_std = "1";

            if (dps_std.IsChecked == true)
            {
                skip_std = "0";
            }

            string player_only = "0";
            if (dps_player.IsChecked == true)
            {
                player_only = "1";
            }

            string basedon = "army";
            if (dps_rb_income.IsChecked == true)
            {
                basedon = "income";
            }
            else if (dps_rb_time.IsChecked == true)
            {
                basedon = "time";
            }
            else if (dps_rb_army.IsChecked == true)
            {
                basedon = "army";
            }

            string alignment = "horizontal";
            if (dps_rb_vertical.IsChecked == true)
            {
                alignment = "vertical";
            }

            string opp = "0";
            if (dps_opp.IsChecked == true)
            {
                opp = "1";
            }

            string dps_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            dps_png += "\\temp\\";

            if (!System.IO.Directory.Exists(dps_png))
            {
                System.IO.Directory.CreateDirectory(dps_png);
            }
            long msec = DateTime.Now.Ticks;
            dps_png += "sc2dsstats_damage_" + msec.ToString() + ".png";
            imgGarbage.Add(dps_png);


            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_worker.exe";

            string ExecutableFilePath = s_doit;

            if (dps_ComboBox.SelectedItem == null)
            {
                dps_ComboBox.SelectedItem = dps_ComboBox.Items[0];
            }
            string cmdr = dps_ComboBox.Items[dps_ComboBox.SelectedIndex].ToString();
            string Arguments = "--start_date=" + sd + " "
                            + "--end_date=" + ed + " "
                            + "--skip STD=" + skip_std + " "
                            + "--dmg=" + basedon + " "
                            + "--alignment=" + alignment + " "
                            + "--png=\"" + dps_png + "\" "
                            ;
               if (String.Equals(opp, "1")) {
                    Arguments += "--cmdr=" + cmdr + " ";
               }
               if (String.Equals(player_only, "1"))
               {  
                    Arguments += "--player_only ";
                
                }

            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }




            if (File.Exists(dps_png))
            {
                if (gr_damage.Children.Contains(myImage))
                {

                    myImage.Source = null;

                    // Create a BitmapSource  
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(@dps_png);
                    bitmap.EndInit();

                    // Set Image.Source  
                    myImage.Source = bitmap;
                    label1.Text = dps_png;
                    label1.UpdateLayout();
                }
                else
                {

                    myImage = CreateViewImageDynamically(dps_png);
                    label1.Text = dps_png;
                    label1.UpdateLayout();
                    gr_damage.Children.Add(myImage);
                }


            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }




        }

        private void dps_rb_army_Click(object sender, RoutedEventArgs e)
        {
            if (dps_rb_army.IsChecked == true)
            {
                dps_rb_income.IsChecked = false;
            }
        }

        private void dps_rb_income_Click(object sender, RoutedEventArgs e)
        {
            if (dps_rb_income.IsChecked == true)
            {
                dps_rb_army.IsChecked = false;
            }
        }

        private void dps_rb_Horizontal_Click(object sender, RoutedEventArgs e)
        {
            if (dps_rb_horizontal.IsChecked == true)
            {
                dps_rb_vertical.IsChecked = false;
            }
        }

        private void dps_rb_Vertical_Click(object sender, RoutedEventArgs e)
        {
            if (dps_rb_vertical.IsChecked == true)
            {
                dps_rb_horizontal.IsChecked = false;
            }
        }


        private void dps_opp_Click(object sender, RoutedEventArgs e)
        {
            dps_ComboBox.Visibility = Visibility.Visible;
            if (dps_opp.IsChecked == false)
            {
                dps_ComboBox.Visibility = Visibility.Collapsed;
            }
        }

        private void dps_image_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Und es war SOmmer");
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

                    win1.Closing += new CancelEventHandler(win1_img_Closing);

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

        void win1_img_Closing(object sender, CancelEventArgs e)
        {

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

   

        private void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (string img in imgGarbage)
            {
                if (File.Exists(img))
                {
                    try
                    {
                        File.Delete(img);
                    }
                    catch (System.IO.IOException)
                    {

                    }
                }
            }
        }
    }
}



