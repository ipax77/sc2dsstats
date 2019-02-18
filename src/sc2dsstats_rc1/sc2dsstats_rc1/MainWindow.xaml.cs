using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;


namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool dt_handle = true;
        private bool vs_handle = true;
        private bool scan_running = false;
        string player_name = "bab";
        public List<dsreplay> replays = new List<dsreplay>();
        public ObservableCollection<KeyValuePair<string, double>> Items { get; set; }
        public ObservableCollection<KeyValuePair<string, double>> Items_sorted { get; set; }
        public List<KeyValuePair<string, double>> Cdata { get; set; }
        Chart dynChart = new Chart() { Background = System.Windows.Media.Brushes.FloralWhite };
        public string[] s_races = new string[17];
        dsotf otf = new dsotf();
        public System.Diagnostics.Process p = new System.Diagnostics.Process();

        public TextBox dynamicText = null;
        //private bool dps_handle = true;
        public string myScan_exe = null;
        public string myScan_log = null;
        public string myWorker_exe = null;
        public string myWorker_log = null;
        public string myStats_csv = null;
        public string mySkip_csv = null;
        public string myTemp_png = null;
        public string myWorker_png = null;
        public List<string> myTempfiles_col = new List<string>();
        public string myTemp_dir = null;
        public string myData_dir = null;
        public string myAppData_dir = null;
        public string myReplay_Path = null;
        public string mySample_csv = null;
        public string myS2cli_exe = null;
        public string myDoc_pdf = null;

        public MainWindow()
        {
            InitializeComponent();

            // config
            string exedir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            myS2cli_exe = exedir + "\\s2_cli.exe";
            myScan_exe = exedir + "\\sc2dsstats.exe";
            
            myWorker_exe = exedir + "\\scripts\\sc2dsstats_worker.exe";
            myWorker_log = exedir + "\\log_worker.txt";
            myStats_csv = exedir + "\\stats.csv";
            //myTemp_dir = System.IO.Path.GetTempPath() + "\\sc2dsstats\\";
            myAppData_dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats";
            myData_dir = myAppData_dir + "\\analyzes";
            mySkip_csv = myAppData_dir + "\\skip.csv";
            myStats_csv = myAppData_dir + "\\stats.csv";
            myTemp_dir = myAppData_dir + "\\";
            mySample_csv = exedir + "\\sample.csv";
            myDoc_pdf = exedir + "\\doc.pdf";
            myScan_log = myAppData_dir + "\\log.txt";

            if (!System.IO.Directory.Exists(myTemp_dir))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(myAppData_dir);
                } catch {
                    MessageBox.Show("Failed to create DataDir " + myAppData_dir + ". Please check your options.", "sc2dsstats");
                }
            }
            var appSettings = ConfigurationManager.AppSettings;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (appSettings["STORE_PATH"] == null)
            {
                config.AppSettings.Settings.Add("STORE_PATH", myData_dir);
            }
            else if (appSettings["STORE_PATH"] == "0")
            {
                config.AppSettings.Settings.Remove("STORE_PATH");
                config.AppSettings.Settings.Add("STORE_PATH", myData_dir);
            } else
            {
                myData_dir = appSettings["STORE_PATH"];
            }

            if (appSettings["STORE_PATH"] == null)
            {
                config.AppSettings.Settings.Add("STORE_PATH", myData_dir);
            }
            else if (appSettings["STORE_PATH"] == "0")
            {
                config.AppSettings.Settings.Remove("STORE_PATH");
                config.AppSettings.Settings.Add("STORE_PATH", myData_dir);
            }
            else
            {
                myData_dir = appSettings["STORE_PATH"];
            }
            if (!Directory.Exists(myData_dir))
            {
                try
                {
                    Directory.CreateDirectory(myData_dir);
                } catch (System.IO.IOException)
                {
                    MessageBox.Show("Failed to create DataDir " + myData_dir + ". Please check your options.", "sc2dsstats");
                }
            }

            if (appSettings["STATS_FILE"] == null)
            {
                config.AppSettings.Settings.Add("STATS_FILE", myStats_csv);
            }
            else if (appSettings["STATS_FILE"] == "0")
            {
                config.AppSettings.Settings.Remove("STATS_FILE");
                config.AppSettings.Settings.Add("STATS_FILE", myStats_csv);
            }
            else
            {
                myStats_csv = appSettings["STATS_FILE"];
            }

            if (appSettings["STATS_FILE"] == null)
            {
                config.AppSettings.Settings.Add("STATS_FILE", myStats_csv);
            }
            else if (appSettings["STATS_FILE"] == "0")
            {
                config.AppSettings.Settings.Remove("STATS_FILE");
                config.AppSettings.Settings.Add("STATS_FILE", myStats_csv);
            }
            else
            {
                myStats_csv = appSettings["STATS_FILE"];
            }

            if (appSettings["SKIP_FILE"] == null)
            {
                config.AppSettings.Settings.Add("SKIP_FILE", mySkip_csv);
            }
            else if (appSettings["SKIP_FILE"] == "0")
            {
                config.AppSettings.Settings.Remove("SKIP_FILE");
                config.AppSettings.Settings.Add("SKIP_FILE", mySkip_csv);
            }
            else
            {
                mySkip_csv = appSettings["SKIP_FILE"];
            }
            if (!File.Exists(mySkip_csv))
            {
                try
                {
                    File.Create(mySkip_csv);
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("Failed to create DataDir " + mySkip_csv + ". Please check your options.", "sc2dsstats");
                }
            }
            int cpus = Environment.ProcessorCount;
            if (appSettings["CORES"] == null) 
            {
                cpus /= 2;
                config.AppSettings.Settings.Add("CORES", cpus.ToString());
            } else if (appSettings["CORES"] == "0" || appSettings["CORES"] == "")
            {
                cpus /= 2;
                config.AppSettings.Settings.Remove("CORES");
                config.AppSettings.Settings.Add("CORES", cpus.ToString());
            } else {
                if (int.Parse(appSettings["CORES"]) > cpus)
                {
                    config.AppSettings.Settings.Remove("CORES");
                    config.AppSettings.Settings.Add("CORES", cpus.ToString());
                }
            }
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");

            if (!File.Exists(myStats_csv))
            {
                try
                {
                    File.Create(myStats_csv);
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("Failed to create DataDir " + myStats_csv + ". Please check your options.", "sc2dsstats");
                }
            }

            cpus /= 2;

            int usedCpus = 1;
            cb_doit_cpus.Items.Add(usedCpus.ToString());

            while (usedCpus < cpus)
            {
                cb_doit_cpus.Items.Add(usedCpus.ToString());
                usedCpus += 1;
            }
            cb_doit_cpus.SelectedItem = cb_doit_cpus.Items[0];

            if (appSettings["REPLAY_PATH"] == null || appSettings["REPLAY_PATH"] == "0" || appSettings["REPLAY_PATH"] == "")
            {
                FirstRun();
            } else
            {
                if (!Directory.Exists(appSettings["REPLAY_PATH"]))
                {
                    FirstRun();
                } else
                {
                    myReplay_Path = appSettings["REPLAY_PATH"];
                }
            }

            // xaml


            player_name = appSettings["PLAYER"];

            if (appSettings["STATS_FILE"] != null && appSettings["STATS_FILE"] != "0")
            {
                myStats_csv = appSettings["STATS_FILE"];
            }

            otf.REPLAY_PATH = appSettings["REPLAY_PATH"];
            otf.MW = this;

            SetGUIFilter(null, null);

            cb_mode.Items.Add("Winrate");
            cb_mode.Items.Add("Damage");
            cb_mode.Items.Add("MVP");
            cb_mode.SelectedItem = cb_mode.Items[0];

            s_races = new string[]
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

            foreach (string r in s_races)
            {
                cb_vs.Items.Add(r);
            }
            cb_vs.SelectedItem = cb_vs.Items[0];

            if (File.Exists(myStats_csv))
            {
                replays = LoadData(myStats_csv);
                ScanPrep();
            }
            Items = new ObservableCollection<KeyValuePair<string, double>>();
            Items_sorted = new ObservableCollection<KeyValuePair<string, double>>();

            SetChartStyle("%", 100);
            //dynChart.MouseMove += new MouseEventHandler(dyn_image_Move);
            dynChart.MouseDown += new MouseButtonEventHandler(dyn_Chart_Click);
            ContextMenu win_cm = new ContextMenu();
            MenuItem win_saveas = new MenuItem();
            win_saveas.Header = "Save as ...";
            win_saveas.Click += new RoutedEventHandler(win_SaveAs_Click);
            win_cm.Items.Add(win_saveas);
            dynChart.ContextMenu = win_cm;
        }

        public void FirstRun()
        {
            gr_filter1.Visibility = System.Windows.Visibility.Hidden;
            gr_mode.Visibility = Visibility.Hidden;
            bt_show.IsEnabled = false;
            cb_sample.IsEnabled = false;
            bt_filter2.IsEnabled = false;
            dp_menu.IsEnabled = false;

            gr_firstrun.Visibility = Visibility.Visible;

        }

        public void ScanPrep()
        {
            dsscan scan = new dsscan(myReplay_Path, myStats_csv, this);

        }
            

        private void SetGUIFilter(object sender, EventArgs e)
        {
            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings["SKIP_STD"] != null && appSettings["SKIP_STD"] == "1")
            {
                cb_std.IsChecked = false;
            }
            else
            {
                cb_std.IsChecked = true;
            }

            if (appSettings["BETA"] != null && appSettings["BETA"] == "1")
            {
                cb_beta.IsChecked = true;
            }
            else
            {
                cb_beta.IsChecked = true;
            }

            if (appSettings["HOTS"] != null && appSettings["HOTS"] == "1")
            {
                cb_hots.IsChecked = true;
            }
            else
            {
                cb_hots.IsChecked = true;
            }

            cb_duration.IsChecked = false;
            tb_duration.Text = "0";
            tb_duration.IsEnabled = false;
            if (appSettings["DURATION"] != null)
            {
                if (appSettings["DURATION"] != "0")
                {
                    cb_duration.IsChecked = true;
                    tb_duration.Text = appSettings["DURATION"];
                    tb_duration.IsEnabled = true;
                }
            }

            cb_leaver.IsChecked = false;
            tb_leaver.Text = "0";
            tb_leaver.IsEnabled = false;
            if (appSettings["LEAVER"] != null)
            {
                if (appSettings["LEAVER"] != "0")
                {
                    cb_leaver.IsChecked = true;
                    tb_leaver.Text = appSettings["LEAVER"];
                    tb_leaver.IsEnabled = true;
                }
            }

            cb_killsum.IsChecked = false;
            tb_killsum.Text = "0";
            tb_killsum.IsEnabled = false;
            if (appSettings["KILLSUM"] != null)
            {
                if (appSettings["KILLSUM"] != "0")
                {
                    cb_killsum.IsChecked = true;
                    tb_killsum.Text = appSettings["KILLSUM"];
                    tb_killsum.IsEnabled = true;
                }
            }

            cb_income.IsChecked = false;
            tb_income.Text = "0";
            tb_income.IsEnabled = false;
            if (appSettings["INCOME"] != null)
            {
                if (appSettings["INCOME"] != "0")
                {
                    cb_income.IsChecked = true;
                    tb_income.Text = appSettings["INCOME"];
                    tb_income.IsEnabled = true;
                }
            }

            cb_army.IsChecked = false;
            tb_army.Text = "0";
            tb_army.IsEnabled = false;
            if (appSettings["ARMY"] != null)
            {
                if (appSettings["ARMY"] != "0")
                {
                    cb_army.IsChecked = true;
                    tb_army.Text = appSettings["ARMY"];
                    tb_army.IsEnabled = true;
                }
            }
        }

        private dsselect GetSelection(dsstats sum, dsstats sum_pl, dsmvp sum_mvp, dsmvp sum_mvp_pl, dsdps sum_dps, dsdps sum_dps_pl)
        {

            List<dsstats_race> data = new List<dsstats_race>();
            dsselect selection = new dsselect();
            int ggames = 0;
            double gwr = 0;
            double gdr = 0;
            string interest = cb_vs.SelectedItem.ToString();
            string y_axis = "%";

            if (cb_mode.SelectedItem.ToString() == "Winrate")
            {
                Title = "Winrate";
                if (cb_player.IsChecked == true)
                {
                    Title += " player";

                    if (chb_vs.IsChecked == true)
                    {
                        Title += " " + interest + " vs";

                        dsstats_vs vs = new dsstats_vs();
                        dsstats_race cmdr = new dsstats_race();
                        cmdr = sum_pl.objRace(interest);
                        vs = cmdr.OPP;

                        data = vs.VS;

                        ggames = vs.GAMES;
                        gwr = vs.GetWR();
                        gdr = vs.GetDURATION(interest);

                    }
                    else
                    {

                        data = sum_pl.LRACE;
                        ggames = sum_pl.GAMES;
                        gwr = sum_pl.GetWR();
                        gdr = sum_pl.GetDURATION();

                    }
                }
                else
                {
                    Title += " world";
                    if (chb_vs.IsChecked == true)
                    {
                        Title += " " + interest + " vs";

                        dsstats_vs vs = new dsstats_vs();
                        dsstats_race cmdr = new dsstats_race();
                        cmdr = sum.objRace(interest);
                        vs = cmdr.OPP;

                        data = vs.VS;

                        ggames = vs.GAMES;
                        gwr = vs.GetWR();
                        gdr = vs.GetDURATION(interest);

                    }
                    else
                    {

                        data = sum.LRACE;
                        ggames = sum.GAMES;
                        gwr = sum.GetWR();
                        sum.GAMES /= 6; // dirty quick
                        gdr = sum.GetDURATION();
                    }

                }

            }
            else if (cb_mode.SelectedItem.ToString() == "MVP")
            {

                Title = "MVP";
                if (cb_player.IsChecked == true)
                {
                    Title += " player";

                    if (chb_vs.IsChecked == true)
                    {
                        Title += " " + interest + " vs";
                        dsstats_vs vs = new dsstats_vs();
                        dsstats_race cmdr = new dsstats_race();
                        cmdr = sum_mvp_pl.objRace(interest);
                        vs = cmdr.OPP;

                        data = vs.VS;

                        ggames = vs.GAMES;
                        gwr = vs.GetWR();
                        gdr = vs.GetDURATION(interest);
                    }
                    else
                    {

                        data = sum_mvp_pl.LRACE;
                        ggames = sum_mvp_pl.GAMES;
                        gwr = sum_mvp_pl.GetWR();
                        gdr = sum_mvp_pl.GetDURATION();
                    }

                }
                else
                {
                    Title += " world";
                    if (chb_vs.IsChecked == true)
                    {
                        Title += " " + interest + " vs";
                        dsstats_vs vs = new dsstats_vs();
                        dsstats_race cmdr = new dsstats_race();
                        cmdr = sum_mvp.objRace(interest);
                        vs = cmdr.OPP;

                        data = vs.VS;

                        ggames = vs.GAMES;
                        gwr = vs.GetWR();
                        gdr = vs.GetDURATION(interest);

                    }
                    else
                    {

                        data = sum_mvp.LRACE;

                        ggames = sum_mvp.GAMES;
                        gwr = sum_mvp.GetWR();
                        gdr = sum_mvp.GetDURATION();
                    }

                }


            }
            else if (cb_mode.SelectedItem.ToString() == "Damage")
            {
                Title = "Damage";
                if (cb_player.IsChecked == true)
                {
                    Title += " player";

                    if (chb_vs.IsChecked == true)
                    {
                        Title += " " + interest + " vs";

                        dsstats_vs vs = new dsstats_vs();
                        dsstats_race cmdr = new dsstats_race();
                        cmdr = sum_dps_pl.objRace(interest);
                        vs = cmdr.OPP;
                        data = vs.VS;

                    }
                    else
                    {
                        data = sum_dps_pl.LRACE;
                    }
                }
                else
                {
                    Title += " world";
                    if (chb_vs.IsChecked == true)
                    {
                        Title += " " + interest + " vs";

                        dsstats_vs vs = new dsstats_vs();
                        dsstats_race cmdr = new dsstats_race();
                        cmdr = sum_dps.objRace(interest);
                        vs = cmdr.OPP;
                        data = vs.VS;
                    }
                    else
                    {
                        data = sum_dps.LRACE;

                    }

                }

            }

            List<KeyValuePair<string, double>> cdata = new List<KeyValuePair<string, double>>();
            string add = "";

            double max = 0;
            foreach (dsstats_race cmdr in data)
            {
                double wr = 0;
                if (cb_mode.SelectedItem.ToString() == "Damage")
                {
                    if (rb_dps.IsChecked == true)
                    {
                        add = " (DPS)";
                        y_axis = "MineralValueKilled game / gameduration";
                        wr = cmdr.GetDPS();
                    }
                    else if (rb_dpm.IsChecked == true)
                    {
                        add = " (DPM)";
                        y_axis = "income / MineralValueKilled";
                        wr = cmdr.GetDPM();
                    }
                    else if (rb_dpv.IsChecked == true)
                    {
                        add = " (DPV)";
                        y_axis = "ArmyValue / MineralValueKilled";
                        wr = cmdr.GetDPV();
                    }
                }
                else
                {
                    wr = cmdr.GetWR();
                }
                
                if (cmdr.RGAMES > 0)
                {
                    if (!Double.IsInfinity(wr)) { // strange things happen ..
                        if (wr > max)
                        {
                            max = wr;
                        }
                        KeyValuePair<string, double> ent = new KeyValuePair<string, double>(cmdr.RACE + " (" + cmdr.RGAMES + ") ", wr);
                        cdata.Add(ent);
                    }
                }
            }
            Title += add;
            char average = '\u2300';
            if (gdr > 0)
            {
                TimeSpan t = TimeSpan.FromSeconds(gdr);
                Title += " (" + average.ToString() + " duration: " + t.Minutes + ":" + t.Seconds + " min)";
            }
            cdata.Sort(delegate (KeyValuePair<string, double> x, KeyValuePair<string, double> y)
            {
                if (x.Value == 0 && y.Value == 0) return 0;
                else if (x.Value == 0) return -1;
                else if (y.Value == 0) return 1;
                else return x.Value.CompareTo(y.Value);
            });

            string grace = average.ToString();
            if (gwr > 0) cdata.Insert(0, new KeyValuePair<string, double>(grace + " (" + ggames.ToString() + ") ", gwr));

            selection.LIST = data;
            selection.CLIST = cdata;
            selection.GAMES = ggames;
            selection.WINS = gwr;
            selection.TITLE = Title;
            selection.YAXIS = y_axis;
            selection.YMAX = (int)max;

            return selection;

        }

        public void GetWinrate()
        {

            string sd = otf_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            double sd_int = double.Parse(sd);
            string ed = otf_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "999999";
            double ed_int = double.Parse(ed);

            int duration = 0;
            int leaver = 0;
            int killsum = 0;
            int army = 0;
            int income = 0;

            List<string> races = new List<string>();
            dsfilter fil = new dsfilter();
            Dictionary<string, int> cmdrs_sum = new Dictionary<string, int>();

            try
            {
                duration = int.Parse(tb_duration.Text);
                leaver = int.Parse(tb_leaver.Text);
                killsum = int.Parse(tb_killsum.Text);
                army = int.Parse(tb_army.Text);
                income = int.Parse(tb_income.Text);
            }
            catch (FormatException)
            {

            }

            dsstats sum = new dsstats();
            sum.Init();
            dsstats sum_pl = new dsstats();
            sum_pl.Init();
            dsmvp sum_mvp = new dsmvp();
            sum_mvp.Init();
            dsmvp sum_mvp_pl = new dsmvp();
            sum_mvp_pl.Init();
            dsdps sum_dps = new dsdps();
            sum_dps.Init();
            dsdps sum_dps_pl = new dsdps();
            sum_dps_pl.Init();

            DSfilter dsfil = new DSfilter(this);
            List<dsreplay> filtered_replays = new List<dsreplay>();
            filtered_replays = dsfil.Filter(replays);

            //foreach (dsreplay dsrep in replays)
            foreach (dsreplay dsrep in filtered_replays)
            {
                if (dsrep.PLAYERCOUNT != 6) continue;
                //if (RepFilter(dsrep, fil, sd_int, ed_int, duration, leaver, killsum, army, income)) continue;

                dsplayer mvp = new dsplayer();

                double gdur = dsrep.DURATION;
                sum.AddGame(gdur);
                sum_pl.AddGame(gdur);
                sum_mvp.AddGame(gdur);
                sum_mvp_pl.AddGame(gdur);
                sum_dps.AddGame(gdur);
                sum_dps_pl.AddGame(gdur);

                foreach (dsplayer pl in dsrep.PLAYERS)
                {
                    if (cmdrs_sum.ContainsKey(pl.RACE))
                    {
                        cmdrs_sum[pl.RACE]++;
                    }
                    else
                    {
                        cmdrs_sum.Add(pl.RACE, 1);
                    }

                    if (pl.KILLSUM == dsrep.MAXKILLSUM)
                    {
                        mvp = pl;
                    }
                    sum.AddGame(pl, dsrep.GetOpp(pl.POS));
                    sum_mvp.AddGame(pl, dsrep.GetOpp(pl.POS));
                    sum_dps.AddGame(pl, dsrep.GetOpp(pl.POS));

                    if (pl.TEAM == dsrep.WINNER)
                    {
                        sum.AddWin(pl, dsrep.GetOpp(pl.POS));
                        sum_dps.AddWin(pl, dsrep.GetOpp(pl.POS));
                    }

                    if (pl.NAME == player_name)
                    {
                        sum_pl.AddGame(pl, dsrep.GetOpp(pl.POS));
                        sum_mvp_pl.AddGame(pl, dsrep.GetOpp(pl.POS));
                        sum_dps_pl.AddGame(pl, dsrep.GetOpp(pl.POS));

                        if (pl.TEAM == dsrep.WINNER)
                        {
                            sum_pl.AddWin(pl, dsrep.GetOpp(pl.POS));
                            sum_dps_pl.AddWin(pl, dsrep.GetOpp(pl.POS));
                        }

                    }
                }

                if (mvp.NAME == player_name)
                {
                    sum_mvp_pl.AddWin(mvp, dsrep.GetOpp(mvp.POS));
                }
                else
                {
                    sum_mvp.AddWin(mvp, dsrep.GetOpp(mvp.POS));
                }
            }

            List<dsstats_race> data = new List<dsstats_race>();
            List<KeyValuePair<string, double>> cdata = new List<KeyValuePair<string, double>>();
            dsselect sel = new dsselect();

            int ggames = 0;
            double gwr = 0;

            sel = GetSelection(sum, sum_pl, sum_mvp, sum_mvp_pl, sum_dps, sum_dps_pl);
            cdata = sel.CLIST;
            ggames = sel.GAMES;
            gwr = sel.WINS;
            Title = sel.TITLE;
            string yaxis = sel.YAXIS;
            int max_y = sel.YMAX;

            //lb_info.Text = fil.Info();
            lb_info.Text = dsfil.FIL.Info();
            lb_info.Text += Environment.NewLine;
            string cmdr_info = "";
            var ordered = cmdrs_sum.OrderBy(x => x.Value);
            foreach (var bab in ordered)
            {
                double per = 0;
                per = (double)bab.Value * 100 / (filtered_replays.Count * 6);
                per = Math.Round(per, 2);
                cmdr_info += bab.Key + " => " + bab.Value.ToString() + " (" + per.ToString() + "%); ";
            }
            lb_info.Text += cmdr_info;

            if (cb_add.IsChecked == false)
            {
                Items = new ObservableCollection<KeyValuePair<string, double>>(cdata);
            } else
            {
                foreach (var bab in cdata)
                {
                    Items.Add(bab);
                }
            }
            SetChartStyle(yaxis, max_y);

            //dynChart.Title = Title;
            dynChart.Title = new TextBlock
            {
                Text = Title,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.DarkBlue
            };





        }

        private bool RepFilter(dsreplay rep, dsfilter fil, double sd, double ed, int duration, int leaver, int killsum, int army, int income)
        {
            bool skip = false;
            fil.GAMES++;

            // Beta
            if (cb_beta.IsChecked == false && skip == false)
            {
                if (rep.REPLAY.Contains("Beta")) skip = true;
                if (skip) fil.Beta++;
            }

            //HotS
            if (cb_hots.IsChecked == false && skip == false)
            {
                if (rep.REPLAY.Contains("HotS")) skip = true;
                if (skip) fil.Hots++;
            }

            //gametime
            if (cb_all.IsChecked == false && skip == false)
            {
                if (rep.GAMETIME < sd) skip = true;
                if (rep.GAMETIME > ed) skip = true;
                if (skip) fil.Gametime++;
            }

            //duration
            if (cb_duration.IsChecked == true && skip == false)
            {
                if (rep.DURATION < duration) skip = true;
                if (skip) fil.Duration++;
            }

            //leaver
            if (cb_leaver.IsChecked == true && skip == false)
            {
                if (rep.MAXLEAVER > leaver) skip = true;
                if (skip) fil.Leaver++;
            }

            //killsum
            if (cb_killsum.IsChecked == true && skip == false)
            {
                if (rep.MINKILLSUM < killsum) skip = true;
                if (skip) fil.Killsum++;
            }

            //army
            if (cb_army.IsChecked == true && skip == false)
            {
                if (rep.MINARMY < army) skip = true;
                if (skip) fil.Army++;
            }

            //income
            if (cb_income.IsChecked == true && skip == false)
            {
                if (rep.MININCOME < income) skip = true;
                if (skip) fil.Income++;
            }

            //std
            if (cb_std.IsChecked == false && skip == false)
            {
                if (rep.RACES.Contains("Terran")) skip = true;
                if (rep.RACES.Contains("Protoss")) skip = true;
                if (rep.RACES.Contains("Zerg")) skip = true;
                if (skip) fil.Std++;
            }

            if (skip == true)
            {
                fil.FILTERED++;
            }

            return skip;

        }

        private void OFTScan()
        {



        }


        // chart

        public void UpdateGraph(object sender)
        {
            if (gr_doit.Visibility == Visibility.Visible)
            {
                gr_doit.Visibility = Visibility.Hidden;
            }

            if (gr_chart.Visibility == Visibility.Hidden)
            {
                gr_chart.Visibility = Visibility.Visible;
            }

            bool doit = true;
            if (cb_add.IsChecked == true && sender != null) doit = false;

            if (doit)
            {
                if (Items != null)
                {
                    if (cb_add.IsChecked == false) Items.Clear();
                    ///dynChart = null;
                    ///dynChart = new Chart() { Background = System.Windows.Media.Brushes.FloralWhite };
                    ///dynChart.Series.Clear();
                    GetWinrate();


                    if (rb_horizontal.IsChecked == true)
                    {
                        tb_fl2_rb_horizontal_Click(null, null);
                    }
                    else if (rb_vertical.IsChecked == true)
                    {
                        tb_fl2_rb_vertical_Click(null, null);
                    }

                    if (gr_chart.Children.Contains(dynChart))
                    {

                    }
                    else
                    {
                        gr_chart.Children.Add(dynChart);
                    }
                }
            }

        }

        private void SetChartStyle_on(string y_Title)
        { }

        private void SetChartStyle(string y_Title, int y_max)
        {

            if (cb_add.IsChecked == false)
            {
                dynChart.Series.Clear();
                dynChart.Axes.Clear();
            }

            Style style = new Style { TargetType = typeof(Grid) };
            //style.Setters.Add(new Setter(Grid.BackgroundProperty, System.Windows.Media.Brushes.LightBlue));
            style.Setters.Add(new Setter(Grid.BackgroundProperty, System.Windows.Media.Brushes.LightGray));

            dynChart.PlotAreaStyle = style;

            Style styleLegand = new Style { TargetType = typeof(Control) };
            styleLegand.Setters.Add(new Setter(Control.WidthProperty, 0d));
            styleLegand.Setters.Add(new Setter(Control.HeightProperty, 0d));

            dynChart.LegendStyle = styleLegand;

            CategoryAxis axisX = new CategoryAxis()
            {
                Orientation = AxisOrientation.X,
                Title = "Commanders (generated by https://github.com/ipax77/sc2dsstats)",
                Foreground = System.Windows.Media.Brushes.Brown
            };

            style = new Style { TargetType = typeof(AxisLabel) };
            style.Setters.Add(new Setter(AxisLabel.LayoutTransformProperty, new RotateTransform() { Angle = -90 }));
            style.Setters.Add(new Setter(AxisLabel.FontFamilyProperty, new System.Windows.Media.FontFamily("Arial")));
            style.Setters.Add(new Setter(AxisLabel.FontSizeProperty, 15.0));
            //style.Setters.Add(new Setter(AxisLabel.ForegroundProperty, System.Windows.Media.Brushes.Black));
            style.Setters.Add(new Setter(AxisLabel.ForegroundProperty, System.Windows.Media.Brushes.DarkBlue));
            axisX.AxisLabelStyle = style;


            LinearAxis axisY = new LinearAxis()
            {
                Orientation = AxisOrientation.Y,
                Title = y_Title,
                Foreground = System.Windows.Media.Brushes.Blue
            };
            if (cb_yscale.IsChecked == true)
            {
                axisY.Minimum = 0;
                axisY.Maximum = (double)y_max + (((double)y_max / 100) * 20);
                if (axisY.Maximum < 1)
                {
                    axisY.Maximum = 1;
                }
                //axisY.Maximum = 120;
            }
            style = new Style { TargetType = typeof(AxisLabel) };
            style.Setters.Add(new Setter(AxisLabel.FontSizeProperty, 15.0));
            style.Setters.Add(new Setter(AxisLabel.ForegroundProperty, System.Windows.Media.Brushes.Black));
            axisY.AxisLabelStyle = style;
            axisY.ShowGridLines = true;

            

            dynChart.Axes.Add(axisX);
            dynChart.Axes.Add(axisY);
        }

        private void tb_fl2_rb_horizontal_Click(object sender, RoutedEventArgs e)
        {
            /**
            System.Windows.Media.Brush arColor = JADEXCODEColor.JADEColor.HCBPAtoARGB(((double)i / a), 1.0, 0.5, 0.5, 0.0);
            // Setup ToolTip XamlString insert.

            string BarColorsAre = "Red = " + ((SolidColorBrush)BarColor).Color.R.ToString() + ", " +
                        "Green = " + ((SolidColorBrush)BarColor).Color.G.ToString() + ", " +
                        "Blue = " + ((SolidColorBrush)BarColor).Color.B.ToString() + ", " +
                        "Alpha = " + ((SolidColorBrush)BarColor).Color.A.ToString();
            **/
            string ctXamlString =
"<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:DVC=\"clr-namespace:System.Windows.Controls.DataVisualization.Charting;assembly=System.Windows.Controls.DataVisualization.Toolkit\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"DVC:ColumnDataPoint\">" +
"<Border x:Name=\"Root\" Opacity=\"0\" BorderBrush=\"{TemplateBinding BorderBrush}\" BorderThickness=\"{TemplateBinding BorderThickness}\">" +
"<VisualStateManager.VisualStateGroups>" +
"<VisualStateGroup x:Name=\"CommonStates\">" +
"<VisualStateGroup.Transitions>" +
"<VisualTransition GeneratedDuration=\"0:0:0.1\"/>" +
"</VisualStateGroup.Transitions>" +
"<VisualState x:Name=\"Normal\"/>" +
"<VisualState x:Name=\"MouseOver\">" +
"<Storyboard>" +
"<DoubleAnimation Duration=\"0\" Storyboard.TargetName=\"MouseOverHighlight\" Storyboard.TargetProperty=\"Opacity\" To=\"0.6\"/>" +
"</Storyboard>" +
"</VisualState>" +
"</VisualStateGroup>" +
"<VisualStateGroup x:Name=\"SelectionStates\">" +
"<VisualStateGroup.Transitions>" +
"<VisualTransition GeneratedDuration=\"0:0:0.1\"/>" +
"</VisualStateGroup.Transitions>" +
"<VisualState x:Name=\"Unselected\"/>" +
"<VisualState x:Name=\"Selected\">" +
"<Storyboard>" +
"<DoubleAnimation Duration=\"0\" Storyboard.TargetName=\"SelectionHighlight\" Storyboard.TargetProperty=\"Opacity\" To=\"0.6\"/>" +
"</Storyboard>" +
"</VisualState>" +
"</VisualStateGroup>" +
"<VisualStateGroup x:Name=\"RevealStates\">" +
"<VisualStateGroup.Transitions>" +
"<VisualTransition GeneratedDuration=\"0:0:0.5\"/>" +
"</VisualStateGroup.Transitions>" +
"<VisualState x:Name=\"Shown\">" +
"<Storyboard>" +
"<DoubleAnimation Duration=\"0\" Storyboard.TargetName=\"Root\" Storyboard.TargetProperty=\"Opacity\" To=\"1\"/>" +
"</Storyboard>" +
"</VisualState>" +
"<VisualState x:Name=\"Hidden\">" +
"<Storyboard>" +
"<DoubleAnimation Duration=\"0\" Storyboard.TargetName=\"Root\" Storyboard.TargetProperty=\"Opacity\" To=\"0\"/>" +
"</Storyboard>" +
"</VisualState>" +
"</VisualStateGroup>" +
"</VisualStateManager.VisualStateGroups>" +
"<Grid>" +
"<Rectangle Fill=\"{TemplateBinding Background}\" Stroke=\"DarkBlue\" />" +
"<Grid Margin=\"0 -20 0 0\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Top\"> " +
"<Border CornerRadius=\"2\" BorderBrush=\"#88888888\" BorderThickness=\"0.5\">" +
"<Border CornerRadius=\"2\" BorderBrush=\"#44888888\" BorderThickness=\"0.5\"/>" +
"</Border>" +
"<TextBlock Margin=\"2\">" +
//"<TextBlock Text=\"{TemplateBinding FormattedDependentValue}\" Margin=\"2\"/>" +
"<Run FontWeight=\"Bold\" Background=\"BlanchedAlmond\" Foreground=\"DarkRed\" FontFamily=\"Courier New\" FontSize=\"13\" Text=\"{TemplateBinding FormattedDependentValue}\"/>" +
"</TextBlock>" +
"</Grid>" +
"</Grid>" +
"</Border>" +
"</ControlTemplate>";

    dynChart.Series.Clear();

            //Horizontal        
            ColumnSeries columnseries = new ColumnSeries();
            columnseries.ItemsSource = Items;
            columnseries.DependentValuePath = "Value";
            columnseries.IndependentValuePath = "Key";

            Style style = new Style { TargetType = typeof(ColumnDataPoint) };
            //style.Setters.Add(new Setter(ColumnDataPoint.IsTabStopProperty, false));

            ControlTemplate ct;
            ct = (ControlTemplate)XamlReader.Parse(ctXamlString);
            style.Setters.Add(new Setter(ColumnDataPoint.TemplateProperty, ct));
            style.Setters.Add(new Setter(ColumnDataPoint.BorderBrushProperty, System.Windows.Media.Brushes.Red));
            style.Setters.Add(new Setter(ColumnDataPoint.BackgroundProperty, System.Windows.Media.Brushes.DarkSlateBlue));

            columnseries.DataPointStyle = style;

            dynChart.Series.Add(columnseries);
        }

        private void tb_fl2_rb_vertical_Click(object sender, RoutedEventArgs e)
        {
            dynChart.Series.Clear();

            //Vertical
            BarSeries barseries = new BarSeries();

            barseries.ItemsSource = Items;
            barseries.DependentValuePath = "Value";
            barseries.IndependentValuePath = "Key";

            dynChart.Series.Add(barseries);





        }



        /// read in csv
        /// 

        public List<dsreplay> LoadData(string csv)
        {
            replays.Clear();
            string line;
            ///string pattern = @"^(\d+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+);";
            string[] myline = new string[12];

            string id = null;
            char[] cTrim = { ' ' };
            List<dscsv> single_replays = new List<dscsv>();

            bool doit = false;
            if (!File.Exists(csv))
            {
                MessageBox.Show("No data found :( - Have you tried File->Scan?", "sc2dsstats");
            } else
            {
                doit = true;
            }

            if (doit)
            {
                System.IO.StreamReader file_c = new System.IO.StreamReader(csv);
                int i = 0;
                while (file_c.ReadLine() != null) { i++; ; }
                int j = 0;
                file_c.Close();
                System.IO.StreamReader file = new System.IO.StreamReader(csv);

                while ((line = file.ReadLine()) != null)
                {
                    j++;
                    myline = line.Split(';');

                    for (int k = 0; k <= 12; k++)
                    {
                        string result = myline[k].Trim(cTrim);
                        myline[k] = result;
                    }

                    if (myline[2].Contains("\\"))
                    {
                        //my $player = "\xd0\x94\xd0\xb0\xd0\xbc\xd0\xb8\xd1\x80";
                        // K\xc4\xb1l\xc4\xb1\xc3\xa7arslan
                        //string hex = Regex.Unescape(myline[2]);
                        //string hex = Regex.Replace(myline[2], "\\\\x", "=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        string hex = Regex.Replace(myline[2], "\\\\x", "\\x", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        // "0xd00x940xd00xb00xd00xbc0xd00xb80xd10x80"
                        //hex = hex.ToUpper();
                        string[] enc1 = hex.Split('\\');
                        List<byte> encl = new List<byte>();
                        int l = 0;
                        string name = "";
                        UTF8Encoding utf8 = new UTF8Encoding();
                        foreach (var x in enc1)
                        {
                            if (x == "") continue;
                            if (x.Substring(0, 1) == "x")
                            {
                                if (x.Length == 3)
                                {
                                    string con = x.Substring(1, 2);
                                    con.ToUpper();
                                    encl.Add(Convert.ToByte(con, 16));
                                }
                                else
                                {
                                    string con = x.Substring(1, 2);
                                    con.ToUpper();
                                    encl.Add(Convert.ToByte(con, 16));
                                    name += utf8.GetString(encl.ToArray());
                                    encl.Clear();
                                    name += x.Substring(3);
                                }
                            }
                            else
                            {
                                if (encl.Count > 0)
                                {
                                    name += utf8.GetString(encl.ToArray());
                                    encl.Clear();

                                }
                                name += x;
                            }
                            
                            l++;
                        }
                        if (encl.Count > 0)
                        {
                            name += utf8.GetString(encl.ToArray());
                        }

                        myline[2] = name;
                    }

                    dscsv rep = new dscsv()
                    {
                        ID = int.Parse(myline[0]),
                        REPLAY = myline[1],
                        NAME = myline[2],
                        RACE = myline[4],
                        TEAM = int.Parse(myline[5]),
                        RESULT = int.Parse(myline[6]),
                        KILLSUM = int.Parse(myline[7]),
                        DURATION = int.Parse(myline[8]),
                        GAMETIME = double.Parse(myline[9]),
                        PLAYERID = int.Parse(myline[10]),
                        INCOME = double.Parse(myline[11], CultureInfo.InvariantCulture),
                        ARMY = int.Parse(myline[12])
                    };

                    if (id == null)
                    {
                        id = rep.REPLAY;
                    }

                    if (String.Equals(id, rep.REPLAY))
                    {
                        single_replays.Add(rep);
                    }
                    else
                    {
                        CollectData(single_replays);
                        id = rep.REPLAY;
                        single_replays.Clear();
                        single_replays.Add(rep);
                    }

                    if (j == i)
                    {
                        CollectData(single_replays);
                    }
                }

                file.Close();
            }

            return replays;
        }

        private void CollectData(List<dscsv> single_replays)
        {
            dsreplay game = new dsreplay();
            dsplayer player = new dsplayer();
            List<dsplayer> gameplayer = new List<dsplayer>();

            foreach (dscsv srep in single_replays)
            {
                if (game.ID == 0) game.ID = srep.ID;
                if (game.REPLAY == null) game.REPLAY = srep.REPLAY;
                if (game.GAMETIME == 0) game.GAMETIME = srep.GAMETIME;

                if (String.Equals(srep.NAME, player_name))
                {

                    game.GAMETIME = srep.GAMETIME;
                    player.POS = srep.PLAYERID;
                    player.RACE = srep.RACE;
                    player.NAME = srep.NAME;
                    player.KILLSUM = srep.KILLSUM;
                    player.PDURATION = srep.DURATION;
                    player.INCOME = srep.INCOME;
                    player.ARMY = srep.ARMY;
                    player.RESULT = 2;
                    player.REPLAY = srep.REPLAY;
                    player.ID = srep.ID;

                    game.DURATION = srep.DURATION;
                    int result = srep.RESULT;
                    if (srep.PLAYERID <= 3)
                    {
                        player.TEAM = 0;
                        if (srep.RESULT == 1)
                        {
                            player.RESULT = 1;
                            game.WINNER = 0;
                        }
                        else
                        {
                            game.WINNER = 1;
                        }
                    }
                    else if (srep.PLAYERID > 3)
                    {
                        player.TEAM = 1;
                        if (srep.RESULT == 1)
                        {
                            player.RESULT = 1;
                            game.WINNER = 1;
                        }
                        else
                        {
                            game.WINNER = 0;
                        }
                    }
                }
                else
                {

                }


            }

            int minkillsum = 0;
            int maxkillsum = 0;
            int minarmy = 0;
            double minincome = 0;
            int maxleaver = 0;
            List<string> races = new List<string>();
            dsplayer MVP = new dsplayer();

            foreach (dscsv srep in single_replays)
            {

                game.PLAYERCOUNT++;
                if (minkillsum == 0)
                {
                    minkillsum = srep.KILLSUM;
                }
                else
                {
                    if (srep.KILLSUM < minkillsum) minkillsum = srep.KILLSUM;

                }
                if (maxkillsum == 0)
                {
                    maxkillsum = srep.KILLSUM;
                }
                else
                {
                    if (srep.KILLSUM > maxkillsum) maxkillsum = srep.KILLSUM;
                }

                if (minincome == 0)
                {
                    minincome = srep.INCOME;
                }
                else
                {
                    if (srep.INCOME < minincome) minincome = srep.INCOME;

                }
                if (minarmy == 0)
                {
                    minarmy = srep.ARMY;
                }
                else
                {
                    if (srep.ARMY < minarmy) minarmy = srep.ARMY;

                }
                int leaver = game.DURATION - srep.DURATION;

                if (maxleaver == 0)
                {
                    maxleaver = leaver;
                }
                else
                {
                    if (leaver > maxleaver) maxleaver = leaver;

                }
                races.Add(srep.RACE);

                if (String.Equals(srep.NAME, player_name))
                {
                    gameplayer.Add(player);
                }
                else
                {
                    dsplayer mplayer = new dsplayer();
                    mplayer.POS = srep.PLAYERID;
                    mplayer.RACE = srep.RACE;
                    mplayer.NAME = srep.NAME;
                    mplayer.KILLSUM = srep.KILLSUM;
                    mplayer.PDURATION = srep.DURATION;
                    mplayer.INCOME = srep.INCOME;
                    mplayer.ARMY = srep.ARMY;
                    mplayer.REPLAY = srep.REPLAY;
                    mplayer.ID = srep.ID;
                    mplayer.RESULT = 2;
                    if (srep.PLAYERID <= 3)
                    {
                        mplayer.TEAM = 0;
                        if (game.WINNER == 0)
                        {
                            mplayer.RESULT = 1;
                        }
                    }
                    else if (srep.PLAYERID > 3)
                    {
                        mplayer.TEAM = 1;
                        if (game.WINNER == 1)
                        {
                            mplayer.RESULT = 1;
                        }
                    }

                    gameplayer.Add(mplayer);
                }
            }

            game.MAXLEAVER = maxleaver;
            game.MINARMY = minarmy;
            game.MININCOME = minincome;
            game.MINKILLSUM = minkillsum;
            game.MAXKILLSUM = maxkillsum;
            game.RACES = new List<string>(races);
            game.PLAYERS = new List<dsplayer>(gameplayer);
            replays.Add(game);


            gameplayer.Clear();


        }

        public string GetTempPNG()
        {
            string rng = myTemp_dir + Guid.NewGuid().ToString() + ".png";
            myTempfiles_col.Add(rng);
            return rng;
        }


        /// xaml
        /// 


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            replays.Clear();
            replays = LoadData(myStats_csv);
            UpdateGraph(null);

            if (gr_chart.Children.Contains(dynChart))
            {

            }
            else
            {
                gr_chart.Children.Add(dynChart);
            }

        }

        private void mnu_Options(object sender, RoutedEventArgs e)
        {

            ///ClearImage();
            Win_config win3 = new Win_config();
            win3.Show();

        }

        public void mnu_Scanpre(object sender, RoutedEventArgs e)
        {
            gr_chart.Visibility = Visibility.Hidden;

            if (gr_filter1.Visibility == Visibility.Visible)
            {
                //gr_doit.Margin = new Thickness(10,160,15,0);

            }
            if (gr_filter2.Visibility == Visibility.Visible)
            {
                //gr_doit.Margin = new Thickness(10, 240, 15, 0);
            }

            gr_doit.Visibility = Visibility.Visible;
            var appSettings = ConfigurationManager.AppSettings;
            dsscan scan = new dsscan(appSettings["REPLAY_PATH"], appSettings["STATS_FILE"], this);
            scan.GetInfo();
            

            doit_TextBox1.Text = "We found " + scan.NEWREP + " new Replays (total: " + scan.TOTAL + ")" + Environment.NewLine;
            doit_TextBox1.Text += Environment.NewLine;
            doit_TextBox1.Text += Environment.NewLine;

            doit_TextBox1.Text += "Expected time needed: " + scan.ESTTIME + " h" + Environment.NewLine;
            doit_TextBox1.Text += "(can be decresed by setting more CPUs at the cost of the computers workload)" + Environment.NewLine;
            doit_TextBox1.Text += Environment.NewLine;
            if (String.Equals(appSettings["KEEP"], "1"))
            {
                doit_TextBox1.Text += "Expected disk space needed: " + scan.ESTSPACE + " GB" + Environment.NewLine;
                doit_TextBox1.Text += "(Your current free disk space is " + scan.FREESPACE + " GB)" + Environment.NewLine;


                if (double.Parse(scan.ESTSPACE) > double.Parse(scan.FREESPACE))
                {
                    doit_TextBox1.Text += "WARNING: There might be not enough Diskspace available!!!" + Environment.NewLine;
                }
            }
            doit_TextBox1.Text += Environment.NewLine;

            doit_TextBox1.Text += "You can always quit the process, next time it will continue at the last position." + Environment.NewLine;
            doit_TextBox1.Text += Environment.NewLine;
            doit_TextBox1.Text += "You can reach this info at 'File->Scan preview' at any time." + Environment.NewLine;

            if (scan_running)
            {
                doit_TextBox1.Text += Environment.NewLine;
                doit_TextBox1.Text += "Prozess already running. Please wait." + Environment.NewLine;
            }

            //gr_doit.Visibility = Visibility.Hidden;
        }

        public void mnu_Scan(object sender, RoutedEventArgs e)
        {
            var appSettings = ConfigurationManager.AppSettings;
            int cores = 2;
            if (appSettings["CORES"] != null && appSettings["CORES"] != "0")
            {
                cores = int.Parse(appSettings["CORES"]);
            }

            if (scan_running == false)
            {
                scan_running = true;
                string ExecutableFilePath = myScan_exe;
                string Arguments = @"--priority=" + "NORMAL" + " "
                                    + "--cores=" + cores.ToString() + " "
                                    + "--player=\"" + appSettings["PLAYER"] + "\" "
                                    + "--stats_file=\"" + myStats_csv + "\" "
                                    + "--replay_path=\"" + appSettings["REPLAY_PATH"] + "\" "
                                    + "--DEBUG=" + appSettings["DEBUG"] + " "
                                    + "--keep=" + appSettings["KEEP"] + " "
                                    + "--store_path=\"" + appSettings["STORE_PATH"] + "\" "
                                    + "--skip_file=\"" + appSettings["SKIP_FILE"] + "\" "
                                    + "--log_file=\"" + myScan_log + "\" "
                                    + "--s2_cli=\"" + myS2cli_exe + "\" "
                                    + "--num_file=\"" + myAppData_dir + "\\num.txt" + "\" "
                                   ;
                //MessageBox.Show(Arguments);

                Task.Factory.StartNew(() =>
                {
                    Process doit = new Process();

                    if (File.Exists(ExecutableFilePath))
                    {
                        doit = System.Diagnostics.Process.Start(ExecutableFilePath, Arguments);
                        doit.WaitForExit();

                        replays.Clear();
                        replays = LoadData(myStats_csv);
                        
                        UpdateGraph(null);
                        scan_running = false;

                    }

                    //MessageBox.Show("Scanning complete.", "sc2dsstats");

                    Dispatcher.Invoke(() =>
                    {
                        if (File.Exists(myScan_log))
                        {
                            string log = "";
                            StreamReader reader = new StreamReader(myScan_log, Encoding.UTF8, true);
                            log = "Log:" + Environment.NewLine;
                            byte[] bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

                            log += Encoding.Default.GetString(bytes);
                            reader.Close();
                            lb_info.Text = log;
                            
                        }

                    });
                }, TaskCreationOptions.AttachedToParent);
            } else
            {
                MessageBox.Show("Scan already running. Please wait. (You can do 'File->Reload data' to see the processed data)", "sc2dsstats");
            }
        }

        private void mnu_LoadData_Click(object sender, RoutedEventArgs e)
        {
            replays.Clear();
            if (File.Exists(myStats_csv))
            {
                replays = LoadData(myStats_csv);
                UpdateGraph(null);
            } else
            {
                MessageBox.Show("No data found :(", "sc2dsstats");
            }
        }

        private void mnu_log_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(myScan_log))
            {
                string log = "";
                try
                {
                    StreamReader reader = new StreamReader(myScan_log, Encoding.UTF8, true);

                    log = "Log:" + Environment.NewLine;
                    byte[] bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

                    log += Encoding.Default.GetString(bytes);
                    reader.Close();
                } catch { }

                Win_log lw = new Win_log();
                lw.win_Log_Textbox_Log.Text = log;
                lw.Show();
            }
        }

        private void mnu_Log_scan(object sender, RoutedEventArgs e)
        { }

        private void mnu_Exit(object sender, RoutedEventArgs e)
        {
            main_Closing(null, null);
        }

        private void mnu_doc(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(myDoc_pdf);
        }

        private void mnu_info(object sender, RoutedEventArgs e)
        {
            Win_log wlog = new Win_log();
            wlog.Title = "Info";
            wlog.win_Log_Textbox_Log.Visibility = Visibility.Collapsed;
            wlog.rtb_info.Visibility = Visibility.Visible;
            wlog.rtb_info.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(wlog.Hyperlink_RequestNavigate));            


            wlog.Show();
        }

        private void mnu_Database(object sender, RoutedEventArgs e)
        {

            ///ClearImage();
            Win_regex win5 = new Win_regex();
            win5.Show();

        }

        private void doit_Button_Click(object sender, RoutedEventArgs e)
        {
            mnu_Scan(null, null);
        }

        private void cb_all_Click(object sender, RoutedEventArgs e)
        {

            if (cb_all.IsChecked == true)
            {
                gr_date.IsEnabled = false;
            }
            else if (cb_all.IsChecked == false)
            {
                gr_date.IsEnabled = true;
            }

        }

        private void cb_std_Click(object sender, RoutedEventArgs e)
        {
            UpdateGraph(sender);
        }

        private void cb_player_Click(object sender, RoutedEventArgs e)
        {
            UpdateGraph(sender);
        }

        private void bt_filter2_Click(object sender, RoutedEventArgs e)
        {
            if (gr_filter2.Visibility == Visibility.Hidden)
            {
                gr_filter2.Visibility = Visibility.Visible;
                gr_chart.Margin = new Thickness(0, 140, 0, 0);
                gr_doit.Margin = new Thickness(10, 140, 15, 0);
            }
            else if (gr_filter2.Visibility == Visibility.Visible)
            {
                if (gr_info.Visibility == Visibility.Visible)
                {
                    bt_filter3_Click(null, null);
                }
                gr_filter2.Visibility = Visibility.Hidden;
                gr_chart.Margin = new Thickness(0, 80, 0, 0);
                gr_doit.Margin = new Thickness(10, 80, 15, 0);
            }
        }

        private void bt_filter3_Click(object sender, RoutedEventArgs e)
        {
            if (gr_info.Visibility == Visibility.Hidden)
            {
                gr_info.Visibility = Visibility.Visible;
                gr_chart.Margin = new Thickness(0, 240, 0, 0);
                gr_doit.Margin = new Thickness(10, 240, 15, 0);
            }
            else if (gr_info.Visibility == Visibility.Visible)
            {
                gr_info.Visibility = Visibility.Hidden;
                gr_chart.Margin = new Thickness(0, 140, 0, 0);
                gr_doit.Margin = new Thickness(10, 140, 15, 0);
            }
        }

        private void cb_otf_Click (object sender, RoutedEventArgs e)
        {
            if (cb_otf.IsChecked == true)
            {
                otf.Start();

            } else
            {
                otf.Stop();
            }
        }

        public class ChartItem
        {
            public string Title { get; set; } // coil456

            public double Value { get; set; } // 334

            public string TooltipLabel
            {
                get { return string.Format("{0}({1})", this.Title, this.Value); } // coil456(334)
            }
        }



        private void tb_fl2_EnterClick(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                UpdateGraph(sender);
            }
        }

        private void chb_vs_Click(object sender, RoutedEventArgs e)
        {
            if (chb_vs.IsChecked == true)
            {
                cb_vs.Visibility = Visibility.Visible;
            }
            else if (chb_vs.IsChecked == false)
            {
                cb_vs.Visibility = Visibility.Hidden;
            }
            UpdateGraph(sender);
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (cb_add.IsChecked == true)
            {
                bt_show.Content = "Add";
            }
            else if (cb_add.IsChecked == false)
            {
                bt_show.Content = "Show";
            }
        }

        private void dt_ComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            dt_handle = !cmb.IsDropDownOpen;
            Handle(sender, e);
        }

        private void dt_ComboBox_Closed(object sender, EventArgs e)
        {
            if (cb_mode.SelectedItem == null)
            {
                cb_mode.SelectedItem = cb_mode.Items[0];
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

        private void vs_ComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            vs_handle = !cmb.IsDropDownOpen;
            Handle(sender, e);
        }

        private void vs_ComboBox_Closed(object sender, EventArgs e)
        {
            if (cb_vs.SelectedItem == null)
            {
                cb_vs.SelectedItem = cb_vs.Items[0];
            }
            else
            {
                try
                {
                    if (vs_handle) Handle(sender, e);
                    vs_handle = true;
                }
                catch (InvalidCastException ex)
                {

                }
            }
        }

        private void Handle(object sender, EventArgs e)
        {
            RoutedEventArgs re = (RoutedEventArgs)e;

            if (cb_mode.SelectedItem == cb_mode.Items[1])
            {
                gr_dps.IsEnabled = true;
            } else
            {
                gr_dps.IsEnabled = false;
            }

            UpdateGraph(sender);
        }


        private void tb_fl2_Click(object sender, RoutedEventArgs e)
        {

            if (sender is CheckBox)
            {
                CheckBox cb = sender as CheckBox;
                if (cb != null)
                {
                    if (cb.Name == "cb_duration")
                    {
                        if (cb.IsChecked == true)
                        {
                            tb_duration.IsEnabled = true;
                        }
                        else if (cb.IsChecked == false)
                        {
                            tb_duration.IsEnabled = false;
                        }
                    }
                    else if (cb.Name == "cb_leaver")
                    {
                        if (cb.IsChecked == true)
                        {
                            tb_leaver.IsEnabled = true;
                        }
                        else if (cb.IsChecked == false)
                        {
                            tb_leaver.IsEnabled = false;
                        }
                    }
                    else if (cb.Name == "cb_killsum")
                    {
                        if (cb.IsChecked == true)
                        {
                            tb_killsum.IsEnabled = true;
                        }
                        else if (cb.IsChecked == false)
                        {
                            tb_killsum.IsEnabled = false;
                        }
                    }
                    else if (cb.Name == "cb_income")
                    {
                        if (cb.IsChecked == true)
                        {
                            tb_income.IsEnabled = true;
                        }
                        else if (cb.IsChecked == false)
                        {
                            tb_income.IsEnabled = false;
                        }
                    }
                    else if (cb.Name == "cb_army")
                    {
                        if (cb.IsChecked == true)
                        {
                            tb_army.IsEnabled = true;
                        }
                        else if (cb.IsChecked == false)
                        {
                            tb_army.IsEnabled = false;
                        }
                    }
                }
            }


            UpdateGraph(sender);
        }

        private void ib_OkButton_Click(object sender, RoutedEventArgs e)
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.


            // Do something with the Input
            String input = fr_InputTextBox.Text;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("PLAYER");
            config.AppSettings.Settings.Add("PLAYER", input);
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
            player_name = input;
            // Clear InputBox.
            fr_InputTextBox.Text = String.Empty;

            string filename = fr_InputTextBox2.Text;


            config.AppSettings.Settings.Remove("REPLAY_PATH");
            config.AppSettings.Settings.Add("REPLAY_PATH", filename);
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");

            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
            MessageBox.Show("Now we are good to go - have fun :) (There are more options available at File->Options)", "sc2dsstats");

            gr_filter1.Visibility = System.Windows.Visibility.Visible;
            gr_mode.Visibility = Visibility.Visible;
            bt_show.IsEnabled = true;
            cb_sample.IsEnabled = true;
            bt_filter2.IsEnabled = true;
            dp_menu.IsEnabled = true;

            gr_firstrun.Visibility = Visibility.Collapsed;
            mnu_Scanpre(null, null);

        }

        private void sample_Click(object sender, RoutedEventArgs e)
        {
            replays.Clear();
            Items.Clear();
            if (cb_sample.IsChecked == true)
            {
                player_name = "PAX";
                if (File.Exists(mySample_csv)) replays = LoadData(mySample_csv);
                GetWinrate();
                UpdateGraph(null);
            } else if (cb_sample.IsChecked == false)
            {
                var appSettings = ConfigurationManager.AppSettings;
                player_name = appSettings["PLAYER"];
                if (File.Exists(myStats_csv)) replays = LoadData(myStats_csv);
                GetWinrate();
                UpdateGraph(null);
            }
            
        }

        private void bt_chart_Click (object sender, RoutedEventArgs e)
        {
            Win_chart chartwin = new Win_chart(this);
            chartwin.Show();
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

        private void dyn_image_Move(object sender, MouseEventArgs e)
        {

            if (dynChart != null && e.LeftButton == MouseButtonState.Pressed)
            {

                try
                {
                    BitmapImage dropBitmap = new BitmapImage();
                    System.Windows.Controls.Image myImage = new System.Windows.Controls.Image();

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)gr_chart.ActualWidth,
                        (int)gr_chart.ActualHeight,
                        96d,
                        96d,
                        PixelFormats.Pbgra32
                        );

                    double cwidth = gr_chart.ActualWidth;
                    double cheight = gr_chart.ActualHeight;

                    gr_chart.Measure(new System.Windows.Size(cwidth, cheight));

                    gr_chart.Arrange(new Rect(new System.Windows.Size(cwidth, cheight)));

                    gr_chart.UpdateLayout();

                    renderBitmap.Render(gr_chart);

                    var png = new PngBitmapEncoder();

                    png.Frames.Add(BitmapFrame.Create(renderBitmap));

                    // Save the bitmap into a file.

                    string drop = GetTempPNG();


                    using (FileStream stream =
                        new FileStream(drop, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        encoder.Save(stream);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        dropBitmap.BeginInit();
                        dropBitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        dropBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        dropBitmap.UriSource = new Uri(drop);
                        dropBitmap.EndInit();

                        myImage.Source = dropBitmap;
                        myImage.AllowDrop = true;
                    });

                    string[] files = new string[1];
                    BitmapImage[] dBitmaps = new BitmapImage[1];
                    files[0] = drop;
                    dBitmaps[0] = dropBitmap;



                    DataObject dropObj = new DataObject(DataFormats.FileDrop, files);
                    ///dropObj.SetData(DataFormats.Text, files[0]);
                    dropObj.SetData(DataFormats.Bitmap, dBitmaps[0]);

                    ///DragDrop.DoDragDrop(myImage, dps_png, DragDropEffects.Copy);
                    DragDrop.DoDragDrop(gr_chart, dropObj, DragDropEffects.Copy);
                }
                catch { }

            }
        }

        public BitmapImage GraphToBitmap()
        {
            BitmapImage crBitmap = new BitmapImage();
            BitmapImage dropBitmap = new BitmapImage();

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
             (int)gr_chart.ActualWidth,
             (int)gr_chart.ActualHeight,
             96d,
             96d,
             PixelFormats.Pbgra32
             );

            double cwidth = gr_chart.ActualWidth;
            double cheight = gr_chart.ActualHeight;

            gr_chart.Measure(new System.Windows.Size(cwidth, cheight));
            gr_chart.Arrange(new Rect(new System.Windows.Size(cwidth, cheight)));
            gr_chart.UpdateLayout();
            renderBitmap.Render(gr_chart);

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(renderBitmap));

            // Save the bitmap into a file.

            string drop = GetTempPNG();
            using (FileStream stream =
                new FileStream(drop, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(stream);
            }

            dropBitmap.BeginInit();
            dropBitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            dropBitmap.CacheOption = BitmapCacheOption.OnLoad;
            dropBitmap.UriSource = new Uri(drop);
            dropBitmap.EndInit();

            if (File.Exists(drop))
            {
                //Graphics g = Graphics.FromImage(dropBitmap);

                int crop = 80;
                if (gr_filter2.Visibility == Visibility.Visible)
                {
                    crop = 140;
                }
                if (gr_info.Visibility == Visibility.Visible)
                {
                    crop = 240;
                }

                Bitmap bitmap = new Bitmap(drop);
                Rectangle rect = new Rectangle(0, crop, (int)gr_chart.ActualWidth, (int)gr_chart.ActualHeight);
                Bitmap cropped = bitmap;
                if (crop < (int)gr_chart.ActualHeight)
                {
                    try
                    {
                        cropped = bitmap.Clone(rect, bitmap.PixelFormat);
                    }
                    catch (OutOfMemoryException)
                    {

                    }
                }

                string crdrop = GetTempPNG();
                cropped.Save(crdrop);

                crBitmap.BeginInit();
                crBitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                crBitmap.CacheOption = BitmapCacheOption.OnLoad;
                crBitmap.UriSource = new Uri(crdrop);
                crBitmap.EndInit();

                bitmap.Dispose();
                cropped.Dispose();
                dropBitmap = null;
            }


            return crBitmap;

        }


        public void dyn_Chart_Click(object sender, MouseEventArgs e)
        {
            /// MessageBox.Show("Und es war SOmmer");

            System.Windows.Controls.Image objImage = sender as System.Windows.Controls.Image;
            BitmapImage crBitmap = new BitmapImage();

            if (e is MouseEventArgs)
            {

                if (e.RightButton == MouseButtonState.Released)
                {
                    Win_pupup win1 = new Win_pupup();
                    System.Windows.Controls.Image myImage = new System.Windows.Controls.Image();

                    crBitmap = GraphToBitmap();

                    win1.Height = crBitmap.Height;
                    win1.Width = crBitmap.Width;
                    // Set Image.Source  
                    win1.win_dps_img1.Source = crBitmap;
                    win1.win_dps_grid.Visibility = Visibility.Visible;


                    win1.win_dps_grid.Visibility = Visibility.Visible;
                    win1.Show();


                    this.Height = this.Height - 1;
                    this.Height = this.Height + 1;



                }

                else if (e.LeftButton == MouseButtonState.Pressed)
                {

                }
            }
        }


        private void win_SaveAs_Click(object sender, RoutedEventArgs e)
        {

            BitmapImage crBitmap = new BitmapImage();
            crBitmap = GraphToBitmap();

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PNG Image|*.png";
            saveFileDialog1.Title = "Save PNG Image File";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                // Save the bitmap into a file.
                using (FileStream stream =
                    new FileStream(saveFileDialog1.FileName, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(crBitmap));
                    encoder.Save(stream);
                }
            }
        }

        public void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (string img in myTempfiles_col)
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
            Application.Current.Shutdown();
        }

    }


}


