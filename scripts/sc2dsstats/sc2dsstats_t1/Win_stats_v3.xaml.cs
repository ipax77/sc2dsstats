using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using sc2dsstats;
using sc2dsstats_t1;

namespace sc2dsstats
{
    /// <summary>
    /// Interaktionslogik für Win_stats_v3.xaml
    /// </summary>
    public partial class Win_stats_v3 : Window
    {

        MainWindow mw = new MainWindow(); /// fix neede (maybe)
        private bool dt_handle = true;
        private bool vs_handle = true;
        string player_name = "bab";
        List<dsreplay> replays = new List<dsreplay>();
        public ObservableCollection<KeyValuePair<string, double>> Items { get; set; }
        public ObservableCollection<KeyValuePair<string, double>> Items_sorted { get; set; }
        public List<KeyValuePair<string, double>> Cdata { get; set; }
        Chart dynChart = new Chart() { Background = Brushes.FloralWhite };
        string[] s_races = new string[17];

        public Win_stats_v3()
        {
            InitializeComponent();


            // xaml

            var appSettings = ConfigurationManager.AppSettings;
            player_name = appSettings["PLAYER"];


            if (appSettings["SKIP_NORMAL"] != null && appSettings["SKIP_NORMAL"] == "1")
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

            replays = LoadData();

            Items = new ObservableCollection<KeyValuePair<string, double>>();
            Items_sorted = new ObservableCollection<KeyValuePair<string, double>>();

            SetChartStyle("MVP %");
            dynChart.MouseMove += new MouseEventHandler(dyn_image_Move);
            ContextMenu win_cm = new ContextMenu();
            MenuItem win_saveas = new MenuItem();
            win_saveas.Header = "Save as ...";
            win_saveas.Click += new RoutedEventHandler(win_SaveAs_Click);
            win_cm.Items.Add(win_saveas);
            dynChart.ContextMenu = win_cm;
        }


        private dsselect GetSelection(dsstats sum, dsstats sum_pl, dsmvp sum_mvp, dsmvp sum_mvp_pl, dsdps sum_dps, dsdps sum_dps_pl)
        {

            List<dsstats_race> data = new List<dsstats_race>();
            dsselect selection = new dsselect();
            int ggames = 0;
            double gwr = 0;
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

                    }
                    else
                    {
                        
                        data = sum_pl.LRACE;
                        ggames = sum_pl.GAMES;
                        gwr = sum_pl.GetWR();

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

                    }
                    else
                    {
                        
                        data = sum.LRACE;
                        ggames = sum.GAMES;
                        gwr = sum.GetWR();

                    }

                }

            } else if (cb_mode.SelectedItem.ToString() == "MVP")
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
                    }
                    else { 
                        
                        data = sum_mvp_pl.LRACE;
                        ggames = sum_mvp_pl.GAMES;
                        gwr = sum_mvp_pl.GetWR();
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

                    }
                    else
                    {
                  
                        data = sum_mvp.LRACE;

                        ggames = sum_mvp.GAMES;
                        gwr = sum_mvp.GetWR();
                    }

                }


            } else if (cb_mode.SelectedItem.ToString() == "Damage")
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
                    } else if (rb_dpm.IsChecked == true)
                    {
                        add = " (DPM)";
                        y_axis = "income / MineralValueKilled";
                        wr = cmdr.GetDPM();
                    } else if (rb_dpv.IsChecked == true)
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
                if (wr > 0)
                {
                    KeyValuePair<string, double> ent = new KeyValuePair<string, double>(cmdr.RACE + " (" + cmdr.RGAMES + ") ", wr);
                    cdata.Add(ent);
                }
            }
            Title += add;
            cdata.Sort(delegate (KeyValuePair<string, double> x, KeyValuePair<string, double> y)
            {
                if (x.Value == 0 && y.Value == 0) return 0;
                else if (x.Value == 0) return -1;
                else if (y.Value == 0) return 1;
                else return x.Value.CompareTo(y.Value);
            });

            string grace = "TOTAL";
            if (gwr > 0) cdata.Insert(0, new KeyValuePair<string, double>(grace + " (" + ggames.ToString() + ") ", gwr));

            selection.LIST = data;
            selection.CLIST = cdata;
            selection.GAMES = ggames;
            selection.WINS = gwr;
            selection.TITLE = Title;
            selection.YAXIS = y_axis;

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

            foreach (dsreplay dsrep in replays)
            {
                if (RepFilter(dsrep, fil, sd_int, ed_int, duration, leaver, killsum, army, income)) continue;

                dsplayer mvp = new dsplayer();

                foreach (dsplayer pl in dsrep.PLAYERS)
                {

                    if (pl.KILLSUM == dsrep.MAXKILLSUM)
                    {
                        mvp = pl;
                    }
                    sum.AddGame(pl.RACE, dsrep.GetOpp(pl.POS).RACE);
                    sum_mvp.AddGame(pl.RACE, dsrep.GetOpp(pl.POS).RACE);
                    sum_dps.AddGame(pl, dsrep.GetOpp(pl.POS));

                    if (pl.TEAM == dsrep.WINNER)
                    {
                        sum.AddWin(pl.RACE, dsrep.GetOpp(pl.POS).RACE);
                        sum_dps.AddWin(pl, dsrep.GetOpp(pl.POS));
                    }

                    if (pl.NAME == player_name)
                    {
                        sum_pl.AddGame(pl.RACE, dsrep.GetOpp(pl.POS).RACE);
                        sum_mvp_pl.AddGame(pl.RACE, dsrep.GetOpp(pl.POS).RACE);
                        sum_dps_pl.AddGame(pl, dsrep.GetOpp(pl.POS));

                        if (pl.TEAM == dsrep.WINNER)
                        {
                            sum_pl.AddWin(pl.RACE, dsrep.GetOpp(pl.POS).RACE);
                            sum_dps_pl.AddWin(pl, dsrep.GetOpp(pl.POS));
                        }

                    }
                }

                if (mvp.NAME == player_name)
                {
                    sum_mvp_pl.AddWin(mvp.RACE, dsrep.GetOpp(mvp.POS).RACE);
                } else
                {
                    sum_mvp.AddWin(mvp.RACE, dsrep.GetOpp(mvp.POS).RACE);
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



            Items = new ObservableCollection<KeyValuePair<string, double>>(cdata);
            SetChartStyle(yaxis);

            dynChart.Title = Title;





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
            if(cb_duration.IsChecked == true && skip == false)
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


        // chart

        public void UpdateGraph(object sender)
        {

            if (Items != null)
            {
                Items.Clear();
                ///dynChart = null;
                ///dynChart = new Chart() { Background = Brushes.FloralWhite };
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

        private void SetChartStyle_on(string y_Title)
        { }

            private void SetChartStyle(string y_Title)
        {

            dynChart.Series.Clear();
            dynChart.Axes.Clear();

            Style style = new Style { TargetType = typeof(Grid) };
            style.Setters.Add(new Setter(Grid.BackgroundProperty, Brushes.LightBlue));
            dynChart.PlotAreaStyle = style;



            





            ///dynChart.LegendTitle = "MVP";

            Style styleLegand = new Style { TargetType = typeof(Control) };
            styleLegand.Setters.Add(new Setter(Control.WidthProperty, 0d));
            styleLegand.Setters.Add(new Setter(Control.HeightProperty, 0d));

            dynChart.LegendStyle = styleLegand;
            

            


            CategoryAxis axisX = new CategoryAxis()
            {
                Orientation = AxisOrientation.X,
                Title = "Commanders (generated by https://github.com/ipax77/sc2dsstats)",
                Foreground = Brushes.Brown
            };

            style = new Style { TargetType = typeof(AxisLabel) };
            style.Setters.Add(new Setter(AxisLabel.LayoutTransformProperty, new RotateTransform() { Angle = -90 }));
            style.Setters.Add(new Setter(AxisLabel.FontFamilyProperty, new FontFamily("Arial")));
            style.Setters.Add(new Setter(AxisLabel.FontSizeProperty, 15.0));
            style.Setters.Add(new Setter(AxisLabel.ForegroundProperty, Brushes.Black));
            axisX.AxisLabelStyle = style;


            LinearAxis axisY = new LinearAxis()
            {
                Orientation = AxisOrientation.Y,
                Title = y_Title,
                Foreground = Brushes.Blue
            };
            style = new Style { TargetType = typeof(AxisLabel) };
            style.Setters.Add(new Setter(AxisLabel.FontSizeProperty, 15.0));
            style.Setters.Add(new Setter(AxisLabel.ForegroundProperty, Brushes.Black));
            axisY.AxisLabelStyle = style;

            dynChart.Axes.Add(axisX);
            dynChart.Axes.Add(axisY);
        }





        /// read in csv
        /// 

        private List<dsreplay> LoadData()
        {

            string csv = mw.GetmyVAR("myStats_csv");
            string line;
            ///string pattern = @"^(\d+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+);";
            string[] myline = new string[12];

            string id = null;
            char[] cTrim = { ' ' };
            List<dscsv> single_replays = new List<dscsv>();
            System.IO.StreamReader file_c = new System.IO.StreamReader(csv);
            int i = 0;
            while (file_c.ReadLine() != null) { i++; ; }
            int j = 0;
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








            return replays;
        }

        private void CollectData(List<dscsv> single_replays)
        {
            dsreplay game = new dsreplay();
            dsplayer player = new dsplayer();
            List<dsplayer> gameplayer = new List<dsplayer>();

            foreach (dscsv srep in single_replays)
            {
                if (String.Equals(srep.NAME, player_name))
                {

                    game.ID = srep.ID;
                    game.REPLAY = srep.REPLAY;
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



        /// xaml
        /// 


        private void Button_Click(object sender, RoutedEventArgs e)
        {

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

        private void mnu_Log(object sender, RoutedEventArgs e)
        { }

        private void mnu_Log_scan(object sender, RoutedEventArgs e)
        { }

        private void mnu_Exit(object sender, RoutedEventArgs e)
        { }


        private void mnu_Database(object sender, RoutedEventArgs e)
        {

            ///ClearImage();
            Win_regex win5 = new Win_regex();
            win5.Show();

        }

        private void cb_all_Click(object sender, RoutedEventArgs e)
        {

            if (cb_all.IsChecked == true)
            {
                gr_date.Visibility = Visibility.Hidden;
            }
            else if (cb_all.IsChecked == false)
            {
                gr_date.Visibility = Visibility.Visible;
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
            }
            else if (gr_filter2.Visibility == Visibility.Visible)
            {
                gr_filter2.Visibility = Visibility.Hidden;
                gr_chart.Margin = new Thickness(0, 80, 0, 0);
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

        private void tb_fl2_rb_horizontal_Click(object sender, RoutedEventArgs e)
        {

            dynChart.Series.Clear();

            //Horizontal        
            ColumnSeries columnseries = new ColumnSeries();
            columnseries.ItemsSource = Items;
            columnseries.DependentValuePath = "Value";
            columnseries.IndependentValuePath = "Key";

            //columnseries.DataPointStyle = (Style)this.Resources["ColumnDataPointStyle"];
            
            
            Style style = new Style { TargetType = typeof(ColumnDataPoint) };
            style.Setters.Add(new Setter(ColumnDataPoint.IsTabStopProperty, false));
            style.Setters.Add(new Setter(ColumnDataPoint.BorderBrushProperty, Brushes.Red));
            style.Setters.Add(new Setter(ColumnDataPoint.BackgroundProperty, Brushes.DarkBlue));
            //style.Setters.Add(new Setter(ColumnDataPoint.WidthProperty, 20d));
            //style.Setters.Add(new Setter(ColumnDataPoint.HeightProperty, 20d));
            //style.Setters.Add(new Setter(ColumnDataPoint.IsValueShownAsLabel, true));
            columnseries.DataPointStyle = style;
            dynChart.Series.Add(columnseries);


            







            style = new Style { TargetType = typeof(DataPoint) };
            style.Setters.Add(new Setter(DataPoint.IsTabStopProperty, false));
            style.Setters.Add(new Setter(DataPoint.BorderBrushProperty, Brushes.Red));
            style.Setters.Add(new Setter(DataPoint.BackgroundProperty, Brushes.DarkBlue));
            style.Setters.Add(new Setter(DataPoint.WidthProperty, 20d));
            style.Setters.Add(new Setter(DataPoint.HeightProperty, 20d));


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


        private void dyn_image_Move(object sender, MouseEventArgs e)
        {

            if (dynChart != null && e.LeftButton == MouseButtonState.Pressed)
            {
                BitmapImage dropBitmap = new BitmapImage();
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    (int)dynChart.ActualWidth,
                    (int)dynChart.ActualHeight,
                    96d,
                    96d,
                    PixelFormats.Pbgra32
                    );

                double cwidth = dynChart.ActualWidth;
                double cheight = dynChart.ActualHeight;
                ///gr_chart.Width = cwidth;
                ///gr_chart.Height = cheight;


                gr_chart.Measure(new Size(cwidth, cheight));

                gr_chart.Arrange(new Rect(new Size(cwidth, cheight)));

                gr_chart.UpdateLayout();

                renderBitmap.Render(gr_chart);

                var png = new PngBitmapEncoder();

                png.Frames.Add(BitmapFrame.Create(renderBitmap));

                // Save the bitmap into a file.

                string drop = mw.GetTempPNG();


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

                string[] files = new string[1];
                BitmapImage[] dBitmaps = new BitmapImage[1];
                files[0] = drop;
                dBitmaps[0] = dropBitmap;



                DataObject dropObj = new DataObject(DataFormats.FileDrop, files);
                ///dropObj.SetData(DataFormats.Text, files[0]);
                dropObj.SetData(DataFormats.Bitmap, dBitmaps[0]);




                ///DragDrop.DoDragDrop(myImage, dps_png, DragDropEffects.Copy);
                DragDrop.DoDragDrop(dynChart, dropObj, DragDropEffects.Copy);

            }
        }


        private void win_SaveAs_Click(object sender, RoutedEventArgs e)
        {


            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)dynChart.ActualWidth,
                (int)dynChart.ActualHeight,
                96d,
                96d,
                PixelFormats.Pbgra32
                );
    
            double cwidth = dynChart.ActualWidth;
            double cheight = dynChart.ActualHeight;
            ///gr_chart.Width = cwidth;
            ///gr_chart.Height = cheight;
            

            gr_chart.Measure(new Size(cwidth, cheight));

            gr_chart.Arrange(new Rect(new Size(cwidth, cheight)));

            gr_chart.UpdateLayout();

            renderBitmap.Render(gr_chart);

            var png = new PngBitmapEncoder();

            png.Frames.Add(BitmapFrame.Create(renderBitmap));

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
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    encoder.Save(stream);
                }


            }
        }

    }


}


