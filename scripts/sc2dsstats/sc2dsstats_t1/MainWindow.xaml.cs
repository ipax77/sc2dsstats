using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Configuration;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls.Primitives;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;


namespace sc2dsstats_t1
{

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public Image dynamicImage = null;
        public TextBox dynamicText = null;

        public MainWindow()
        {
            InitializeComponent();

            /// First run?
            var appSettings = ConfigurationManager.AppSettings;


            if (string.Equals(appSettings["FIRST_RUN"], "1")) {



                /// MessageBox.Show("Welcome to sc2dsstats - this is your first run so we need to know some things to make this work ..");
                fr_InputBox.Visibility = System.Windows.Visibility.Visible;


                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Remove("FIRST_RUN");
                config.AppSettings.Settings.Add("FIRST_RUN", "0");
                config.Save();
                ConfigurationManager.RefreshSection("appSettings");



            }




        }

        private List<string> FirstRun()
        {
            string dir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            string drive = "C:\\";
            List<string> dsreplays = new List<string>();

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
            if (Directory.Exists(appSettings["REPLAY_PATH"]))
            {
                string[] replays = Directory.GetFiles(appSettings["REPLAY_PATH"]);
                foreach (string fileName in replays)
                {
                    Regex frx = new Regex(@"^Direct Strike|^Desert Strike");

                    string p = Path.GetFileName(fileName);
                    MatchCollection fmatches = frx.Matches(p);
                    if (fmatches.Count > 0)
                    {
                        foreach (Match match in fmatches)
                        {
                            dsreplays.Add(fileName);
                            i++;

                        }
                    }
                }
            }

            string analyzes = dir + "/analyzes";

            int j = 0;
            if (Directory.Exists(analyzes))
            {
                string[] replays = Directory.GetFiles(analyzes);
                foreach (string fileName in replays)
                {
                    Regex frx = new Regex(@"^Direct Strike|^Desert Strike");

                    string p = Path.GetFileName(fileName);
                    MatchCollection fmatches = frx.Matches(p);
                    if (fmatches.Count > 0)
                    {
                        foreach (Match match in fmatches)
                            j++;
                    }
                }
            }

            if (String.Equals(appSettings["SKIP_MSG"], "0"))
            {
                j /= 2;
            } else {
                j /= 3;
            }

            int newrep = i - j;

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
            doit_TextBox1.Text += "Expected disk space needed: " + st_size + " GB" + Environment.NewLine;
            doit_TextBox1.Text += "(Your current free disk space is " + st_fs + " GB)" + Environment.NewLine;

            if (nsize > fs)
            {
                doit_TextBox1.Text += "WARNING: There might be not enough Diskspace available!!!" + Environment.NewLine;
            }
            doit_TextBox1.Text += Environment.NewLine;

            doit_TextBox1.Text += "You can always quit the prozess, next time it will continue at the last position." + Environment.NewLine;

            /// 100 Replays =~ 647265846 Bytes, 720 sec


            return dsreplays;

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

            MessageBox.Show("Now we are good to go - have fun :) (There are more options available at File->Options)");
            
        }

        private void ClearImage()
        {
            if (stackpanel1.Children.Contains(dynamicImage))
            {
                dynamicImage.Source = null;
                stackpanel1.Children.Clear();
            }

            if (stackpanel1.Children.Contains(dynamicText))
            {
                dynamicText = null;
                stackpanel1.Children.Clear();
            }

            dp_config.Visibility = Visibility.Collapsed;
            otf_stats.Visibility = Visibility.Collapsed;
            doit_grid.Visibility = Visibility.Collapsed;
            gr_details.Visibility = Visibility.Collapsed;

        }

        private void CreateViewImageDynamically(string imgPath)
        {
            // Create Image and set its width and height  
            dynamicImage = new Image();
            dynamicImage.Stretch = Stretch.Fill;
            dynamicImage.StretchDirection = StretchDirection.Both;
            dynamicImage.Width = 1610;
            dynamicImage.Height = 610;

            // Create a BitmapSource  
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(@imgPath);
            bitmap.EndInit();

            // Set Image.Source  
            dynamicImage.Source = bitmap;

            // Add Image to Window  
            stackpanel1.Children.Add(dynamicImage);
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
            List<string> files = new List<string>();
            files = FirstRun();




        }


        private void doit_Button_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();

            string logfile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            logfile += "\\log.txt";

            CreateViewTextDynamically(logfile);

            

            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\doit.cmd";
            string ExecutableFilePath = s_doit;
            string Arguments = @" ";

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
            } else
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
            stats += "\\stats.png";

            if (File.Exists(stats))
            {
                CreateViewImageDynamically(stats);
                label1.Text = stats;
                label1.UpdateLayout();

            } else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }
        }

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

        


        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ClearImage();
        }

        private void mnu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

            InitializeComponent();

            dp_config.Visibility = Visibility.Visible;

        }

        private void bt_save_Click (object sender, RoutedEventArgs e)
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


                    } else
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
            string show_std = "1";
            
            if (otf_std.IsChecked == true)
            {
                show_std = "0";
            } 

            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_worker.exe";
 
            string ExecutableFilePath = s_doit;
            string Arguments = sd + " " + ed + " " + show_std;


            List<string> files = new List<string>();
            files = FirstRun();


            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }

            string otf_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            otf_png += "\\otf.png";

   
            if (File.Exists(otf_png))
            {
                // Create a BitmapSource  
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(@otf_png);
                bitmap.EndInit();

                // Set Image.Source  
                otf_image.Source = bitmap;
                label1.Text = otf_png;
                label1.UpdateLayout();

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }
        



    }

        private void bt_Time_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
            otf_enddate.SelectedDate = DateTime.Today;
            otf_startdate.SelectedDate = DateTime.Today;
            otf_stats.Visibility = Visibility.Visible;

        }

        private void bt_details_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
            System.DateTime sd = new DateTime(2018, 1, 1);
            dt_enddate.SelectedDate = DateTime.Today;
            dt_startdate.SelectedDate = sd;

            string[] cmdrs = new string[]
            {
                "Abathur",
                 "Alarak",
                 "Artanis",
                 "Dehaka",
                 "Fenix",
                 "Tychus",
                 "Horner",
                 "Karax",
                 "Kerrigan",
                 "Raynor",
                 "Stukov",
                 "Swann",
                 "Nova",
                 "Vorazun",
                 "Zagara",
                 "Protoss",
                 "Terran",
                 "Zerg"
            };
            foreach (string cmdr in cmdrs) {
                dt_ComboBox.Items.Add(cmdr);
            }

            dt_ComboBox.SelectedItem = dt_ComboBox.Items[0];
            gr_details.Visibility = Visibility.Visible;

        }

        private void dt_showButton_Click(object sender, RoutedEventArgs e)
        {


            ///otf_image.Source = null;

            string sd = dt_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            string ed = dt_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "000000";
            string show_std = "1";

            if (dt_std.IsChecked == true)
            {
                show_std = "0";
            }

            string s_doit = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            s_doit += "\\scripts\\sc2dsstats_worker.exe";

            string ExecutableFilePath = s_doit;
            string cmdr = dt_ComboBox.Items[dt_ComboBox.SelectedIndex].ToString();
            string Arguments = sd + " " + ed + " " + show_std + " " + cmdr;


            List<string> files = new List<string>();
            files = FirstRun();


            Process doit = new Process();

            if (File.Exists(ExecutableFilePath))
            {
                doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                doit.WaitForExit();

            }

            string dt_png = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            dt_png += "\\dt.png";


            if (File.Exists(dt_png))
            {
                // Create a BitmapSource  
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(@dt_png);
                bitmap.EndInit();

                // Set Image.Source  
                dt_image.Source = bitmap;
                label1.Text = dt_png;
                label1.UpdateLayout();

            }
            else
            {
                MessageBox.Show("No Data found :( - Did you press the 'doit' button?");
            }




        }
    }
}
