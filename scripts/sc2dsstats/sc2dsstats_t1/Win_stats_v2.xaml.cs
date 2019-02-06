using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using sc2dsstats_t1;
using sc2dsstats;
using static sc2dsstats.Win_regex;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Windows.Controls.DataVisualization.Charting;
using System.IO;

namespace sc2dsstats
{
    /// <summary>
    /// Interaktionslogik für Win_stats_v2.xaml
    /// </summary>
    public partial class Win_stats_v2 : Window
    {
        Win_regex wr = new Win_regex();
        List<myGame> games = new List<myGame>();
        List<CheckBox> myCbs = new List<CheckBox>();

        List<string> races = new List<string>();
        string player_name = "bab";
        public ObservableCollection<KeyValuePair<string, double>> Items { get; set; }
        public ObservableCollection<KeyValuePair<string, double>> Items_sorted { get; set; }
        Chart dynChart = new Chart() { Background = Brushes.FloralWhite };

        private bool dt_handle = true;

        public Win_stats_v2()
        {
            InitializeComponent();

            var appSettings = ConfigurationManager.AppSettings;
            player_name = appSettings["PLAYER"];

            /**
                 <add key="REPLAY_PATH" value="" />
     <add key="PLAYER" value="" />
     <add key="SKIP_NORMAL" value="1" />
     <add key="SKIP" value="240" />
     <add key="START_DATE" value="0" />
     <add key="END_DATE" value="0" />
     <add key="DEBUG" value="1" />
     <add key="SKIP_MSG" value="0" />
     <add key="FIRST_RUN" value="1" />
     <add key="BETA" value="1" />
     <add key="HOTS" value="1" />
     <add key="DURATION" value="5376" />
     <add key="LEAVER" value="2000" />
     <add key="KILLSUM" value="1500" />
     <add key="INCOME" value="1500" />
     <add key="ARMY" value="1500" />
     <add key="KEEP" value="1" />

            **/

            if (appSettings["SKIP_NORMAL"] != null && appSettings["SKIP_NORMAL"] == "1")
            {
                otf_std.IsChecked = false;
            }
            else
            {
                otf_std.IsChecked = true;
            }

            if (appSettings["BETA"] != null && appSettings["BETA"] == "1")
            {
                cb_beta.IsChecked = false;
            }
            else
            {
                cb_beta.IsChecked = true;
            }

            if (appSettings["HOTS"] != null && appSettings["HOTS"] == "1")
            {
                cb_hots.IsChecked = false;
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

            string[] s_races = new string[]
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

            games = wr.LoadCollectionData();
            ///GetData(games);

            Items = new ObservableCollection<KeyValuePair<string, double>>();
            Items_sorted = new ObservableCollection<KeyValuePair<string, double>>();

            SetChartStyle("MVP %");
           

            /**
            cmdr_data test = new cmdr_data();
            test = cmdrs.Find(x => x.RACE == "Swann");
            MessageBox.Show(test.PGAMES + " => " + test.PMVP);
            **/
        }


        public List<cmdr_data> GetData(List<myGame> games)
        {
            string[] s_races = new string[]
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

            List<cmdr_data> cmdrs = new List<cmdr_data>();
            List<cmdr_data> cmdrs_vs = new List<cmdr_data>();
            gamePlayer opp = new gamePlayer();

            foreach (string r in s_races)
            {
                cmdr_data cmdr = new cmdr_data();
                cmdr_data cmdr_vs = new cmdr_data();
                cmdr.RACE = r;
                cmdrs.Add(cmdr);
                cmdr_vs.RACE = "vs " + r;
                cmdrs_vs.Add(cmdr_vs);
            }
            
            foreach (cmdr_data temp in cmdrs)
            {
                temp.VS = new List<cmdr_data>(cmdrs_vs);
            }
            

            foreach (myGame game in games)
            {
                gamePlayer mvp = new gamePlayer();
                cmdr_data sum = new cmdr_data();
                cmdr_data sum_vs = new cmdr_data();
                int maxkillsum = 0;
                foreach (gamePlayer player in game.PLAYERS)
                {
                    if (player.POS == 1) opp = game.PLAYERS.Find(x => x.POS == 4);
                    if (player.POS == 2) opp = game.PLAYERS.Find(x => x.POS == 5);
                    if (player.POS == 3) opp = game.PLAYERS.Find(x => x.POS == 6);
                    if (player.POS == 4) opp = game.PLAYERS.Find(x => x.POS == 1);
                    if (player.POS == 5) opp = game.PLAYERS.Find(x => x.POS == 2);
                    if (player.POS == 6) opp = game.PLAYERS.Find(x => x.POS == 3);
                    

                    string race = player.RACE;
                    sum = cmdrs.Find(x => x.RACE == race);
                    sum_vs = sum.VS.Find(x => x.RACE == "vs " + opp.RACE);

                    sum.GGAMES++;
                    sum_vs.GGAMES++;
                    if (player.TEAM == game.WINNER)
                    {
                        sum.GWIN++;
                        sum_vs.GWIN++;
                    }
                    if (player.NAME == player_name)
                    {
                        sum.PGAMES++;
                        sum_vs.PGAMES++;
                        if (player.TEAM == game.WINNER)
                        {
                            sum.PWIN++;
                            sum_vs.PWIN++;
                        }
                    }

                    if (player.KILLSUM > maxkillsum)
                    {
                        mvp = player;
                        maxkillsum = player.KILLSUM;
                    }

                    sum_vs = sum.VS.Find(x => x.RACE == opp.RACE);

                }
                sum = cmdrs.Find(x => x.RACE == mvp.RACE);
                if (mvp.POS == 1) opp = game.PLAYERS.Find(x => x.POS == 4);
                if (mvp.POS == 2) opp = game.PLAYERS.Find(x => x.POS == 5);
                if (mvp.POS == 3) opp = game.PLAYERS.Find(x => x.POS == 6);
                if (mvp.POS == 4) opp = game.PLAYERS.Find(x => x.POS == 1);
                if (mvp.POS == 5) opp = game.PLAYERS.Find(x => x.POS == 2);
                if (mvp.POS == 6) opp = game.PLAYERS.Find(x => x.POS == 3);
                

                if (sum != null)
                {
                    sum_vs = sum.VS.Find(x => x.RACE == "vs " + opp.RACE);

                    sum.GMVP++;
                    sum_vs.GMVP++;
                    if (mvp.NAME == player_name)
                    {
                        sum.PMVP++;
                        sum_vs.PMVP++;
                    }
                }

            }
            return cmdrs;
        }

        private void cb_otf_all_Click(object sender, RoutedEventArgs e)
        {

            if (otf_all.IsChecked == true)
            {
                gr_date.Visibility = Visibility.Hidden;
            }
            else if (otf_all.IsChecked == false)
            {
                gr_date.Visibility = Visibility.Visible;
            }

        }

        private void cb_otf_std_Click(object sender, RoutedEventArgs e)
        {
            UpdateGraph(sender);
        }

        private void cb_otf_player_Click(object sender, RoutedEventArgs e)
        {
            UpdateGraph(sender);
        }

        public List<myGame> GUIDataFilter(List<myGame> games)
        {

            List<myGame> filtered_games = new List<myGame>();

            string sd = otf_startdate.SelectedDate.Value.ToString("yyyyMMdd");
            sd += "000000";
            double sd_int = double.Parse(sd);
            string ed = otf_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            ed += "000000";
            double ed_int = double.Parse(ed);

            foreach (myGame game in games)
            {
                if (otf_all.IsChecked == false)
                {
                    if (game.GAMETIME < sd_int) break;
                    if (game.GAMETIME > ed_int) break;
                }




                filtered_games.Add(game);
            }



            return filtered_games;

        }


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

        private void UpdateGraph(object sender)
        {
            if (cb_add.IsChecked == false)
            {
                if (Items != null)
                {
                    Items.Clear();
                    dynChart = null;
                    dynChart = new Chart() { Background = Brushes.FloralWhite };
                }
            }

            

            if (cb_add.IsChecked == true && sender != null)
            {
                // no auto update on add mode
            }
            else
            {
                List<myGame> filtered_games = new List<myGame>();
                List<cmdr_data> cmdrs = new List<cmdr_data>();

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
                    MessageBox.Show("Please Check you filters.", "Filter error");
                }

                foreach (myGame game in games)
                {
                    // Beta
                    if (cb_beta.IsChecked == false)
                    {
                        if (game.REPLAY.Contains("Beta")) continue;

                    }

                    //HotS
                    if (cb_hots.IsChecked == false)
                    {
                        if (game.REPLAY.Contains("HotS")) continue;
                    }

                    //gametime
                    if (otf_all.IsChecked == false)
                    {
                        if (game.GAMETIME < sd_int) continue;
                        if (game.GAMETIME > ed_int) continue;
                    }

                    //duration
                    if (cb_duration.IsChecked == true)
                    {
                        if (game.DURATION < duration) continue;
                    }

                    //leaver
                    if (cb_leaver.IsChecked == true)
                    {
                        if (game.MAXLEAVER > leaver) continue;
                    }

                    //killsum
                    if (cb_killsum.IsChecked == true)
                    {
                        if (game.MINKILLSUM < killsum) continue;
                    }

                    //army
                    if (cb_army.IsChecked == true)
                    {
                        if (game.MINARMY < army) continue;
                    }

                    //income
                    if (cb_income.IsChecked == true)
                    {
                        if (game.MININCOME < income) continue;
                    }

                    //std
                    if (otf_std.IsChecked == false)
                    {
                        races = game.RACES;
                        if (races.Contains("Terran")) continue;
                        if (races.Contains("Protoss")) continue;
                        if (races.Contains("Zerg")) continue;
                    }
                    filtered_games.Add(game);
                }

                cmdrs = GetData(filtered_games);

                string add = " global";
                
                if (chb_vs.IsChecked == false)
                {
                    add = " global";
                    if (otf_player.IsChecked == true)
                    {
                        add = " player";
                    }
                } else
                {
                    add += " vs " + cb_vs.SelectedItem.ToString();
                }

                
                if (chb_vs.IsChecked == true)
                {
                    cmdr_data cmdr_vs = new cmdr_data();
                    cmdr_vs = cmdrs.Find(x => x.RACE == cb_vs.SelectedItem.ToString());
                    cmdrs = cmdr_vs.VS;
                }
   

                if (cb_mode.SelectedIndex == 0)
                {
                    dynChart.Title = "Winrate " + add;
                    SetChartStyle("winrate %");

                    if (otf_player.IsChecked == true)
                    {
                        cmdrs.Sort(delegate (cmdr_data x, cmdr_data y)
                        {
                            if (x.GetPWR() == 0 && y.GetPWR() == 0) return 0;
                            else if (x.GetPWR() == 0) return -1;
                            else if (y.GetPWR() == 0) return 1;
                            else return x.GetPWR().CompareTo(y.GetPWR());
                        });
                    }
                    else
                    {
                        cmdrs.Sort(delegate (cmdr_data x, cmdr_data y)
                        {
                            if (x.GetGWR() == 0 && y.GetGWR() == 0) return 0;
                            else if (x.GetGWR() == 0) return -1;
                            else if (y.GetGWR() == 0) return 1;
                            else return x.GetGWR().CompareTo(y.GetGWR());
                        });

                    }
                }
                else if (cb_mode.SelectedIndex == 1)
                {
                    dynChart.Title = "Damage " + add;
                    SetChartStyle("dps");

                }
                else if (cb_mode.SelectedIndex == 2)
                {
                    dynChart.Title = "MVP " + add;
                    SetChartStyle("MVP %");

                    if (otf_player.IsChecked == true)
                    {
                        cmdrs.Sort(delegate (cmdr_data x, cmdr_data y)
                        {
                            if (x.GetPMVP() == 0 && y.GetPMVP() == 0) return 0;
                            else if (x.GetPMVP() == 0) return -1;
                            else if (y.GetPMVP() == 0) return 1;
                            else return x.GetPMVP().CompareTo(y.GetPMVP());
                        });
                    } else
                    {
                        cmdrs.Sort(delegate (cmdr_data x, cmdr_data y)
                        {
                            if (x.GetGMVP() == 0 && y.GetGMVP() == 0) return 0;
                            else if (x.GetGMVP() == 0) return -1;
                            else if (y.GetGMVP() == 0) return 1;
                            else return x.GetGMVP().CompareTo(y.GetGMVP());
                        });

                    }

                }

                int tgames = 0;

                foreach (cmdr_data cmdr in cmdrs)
                {
                    double y = 0;

                    string race = cmdr.RACE;

                    // winrate
                    if (cb_mode.SelectedIndex == 0)
                    {
                        if (otf_player.IsChecked == true)
                        {
                            tgames = cmdr.PGAMES;
                            y = cmdr.GetPWR();
                        }
                        else if (otf_player.IsChecked == false)
                        {
                            tgames = cmdr.GGAMES;
                            y = cmdr.GetGWR();
                        }
                        // damage
                    }
                    else if (cb_mode.SelectedIndex == 1)
                    {
                        tgames = cmdr.PGAMES;
                        // MVP
                    }
                    else if (cb_mode.SelectedIndex == 2)
                    {

                        if (otf_player.IsChecked == true)
                        {
                            tgames = cmdr.PGAMES;
                            y = cmdr.GetPMVP();
                        }
                        else if (otf_player.IsChecked == false)
                        {
                            tgames = cmdr.GGAMES;
                            y = cmdr.GetGMVP();
                        }
                    }

                    if (y != 0)
                    {

                        Items.Add(new KeyValuePair<string, double>(race + " (" + tgames.ToString() + ") ", y));


                    }
                }


                ///dynChart.Axes.Clear();



                //dynChart.Series.Clear();

                if (rb_horizontal.IsChecked == true)
                {
                    tb_fl2_rb_horizontal_Click(null, null);
                }
                else if (rb_vertical.IsChecked == true)
                {
                    tb_fl2_rb_vertical_Click(null, null);
                }

            }

            

            ///Items.OrderBy(x => x.Value).ToList();



        }
        private void tb_fl2_rb_horizontal_Click(object sender, RoutedEventArgs e)
        {

            dynChart.Series.Clear();

            //Horizontal        
            ColumnSeries columnseries = new ColumnSeries();
            columnseries.ItemsSource = Items;
            columnseries.DependentValuePath = "Value";
            columnseries.IndependentValuePath = "Key";
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

        private void SetYAxis (string y_Title)
        {
            dynChart.Axes.Clear();
            Style style = new Style { TargetType = typeof(AxisLabel) };
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
            style.Setters.Add(new Setter(AxisLabel.FontFamilyProperty, new FontFamily("Arial")));
            style.Setters.Add(new Setter(AxisLabel.FontSizeProperty, 15.0));
            style.Setters.Add(new Setter(AxisLabel.ForegroundProperty, Brushes.Black));
            axisY.AxisLabelStyle = style;

            dynChart.Axes.Add(axisX);
            dynChart.Axes.Add(axisY);
        }

        private void SetChartStyle(string y_Title)
        {
            Style style = new Style { TargetType = typeof(Grid) };
            style.Setters.Add(new Setter(Grid.BackgroundProperty, Brushes.LightBlue));
            dynChart.PlotAreaStyle = style;

            ColumnSeries columnseries = new ColumnSeries();
            style = new Style { TargetType = typeof(ColumnDataPoint) };
            style.Setters.Add(new Setter(ColumnDataPoint.BorderBrushProperty, Brushes.Red));
            style.Setters.Add(new Setter(ColumnDataPoint.BackgroundProperty, Brushes.DarkBlue));
            columnseries.DataPointStyle = style;

            ///dynChart.LegendTitle = "MVP";
            Style styleLegand = new Style { TargetType = typeof(Control) };
            styleLegand.Setters.Add(new Setter(Control.WidthProperty, 0d));
            styleLegand.Setters.Add(new Setter(Control.HeightProperty, 0d));
            dynChart.LegendStyle = styleLegand;

            dynChart.Series.Add(columnseries);
            

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

        private void mnu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mnu_Log(object sender, RoutedEventArgs e)
        {
            Win_log win2 = new Win_log();
            string logfile = "C:/sc2dsstats_prod/log.txt";

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
            }
            else
            {
                MessageBox.Show("No logfile found :(", "sc2dsstats");
            }


        }

        private void mnu_Log_scan(object sender, RoutedEventArgs e)
        {
            Win_log win2 = new Win_log();
            string logfile = "C:/sc2dsstats_prod/log.txt";

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
            }
            else
            {
                MessageBox.Show("No logfile found :(", "sc2dsstats");
            }


        }
        private void mnu_Options(object sender, RoutedEventArgs e)
        {

            ///ClearImage();
            Win_config win3 = new Win_config();
            win3.Show();

        }

        private void mnu_Database(object sender, RoutedEventArgs e)
        {

            ///ClearImage();
            Win_regex win5 = new Win_regex();
            win5.Show();

        }

        private void Bt_filter2_Click(object sender, RoutedEventArgs e)
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
            } else if (chb_vs.IsChecked == false)
            {
                cb_vs.Visibility = Visibility.Hidden;
            }
            UpdateGraph(sender);
        }

        private void add_Click (object sender, RoutedEventArgs e)
        {
            if (cb_add.IsChecked == true)
            {
                bt_show.Content = "Add";
            } else if (cb_add.IsChecked == false)
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
                        } else if (cb.IsChecked == false)
                        {
                            tb_duration.IsEnabled = false;
                        }
                    } else if (cb.Name == "cb_leaver")
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
    }
}
