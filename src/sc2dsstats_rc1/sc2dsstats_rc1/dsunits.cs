using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace sc2dsstats_rc1
{




    public class dsunits
    {
        public Dictionary<string, List<dsunits_player>> UNITLIST { get; set; } = new Dictionary<string, List<dsunits_player>>();
        public MainWindow MW;

        public static int MIN5 { get; } = 6720;
        public static int MIN10 { get; } = 13440;
        public static int MIN15 { get; } = 20160;

        public int GAMES { get; set; } = 0;
        public double WR { get; set; } = 0;
        public string DURATION { get; set; } = "";
        public string GAMETIME { get; set; } = "";
        public List<dsunits_info> INFO { get; set; }
        public dsunits()
        {

        }

        public dsunits(MainWindow mw) : this()
        {
            MW = mw;
        }

        public List<dsunits_grid> Sum (string interest, string vs, string gametime)
        {
            string sum = "";

            DSfilter dsfil = new DSfilter(MW);
            List<dsreplay> filtered_replays = new List<dsreplay>();
            filtered_replays = dsfil.Filter(MW.replays);

            List<dsreplay> replay_sorted = new List<dsreplay>();
            replay_sorted = filtered_replays.Where(x => x.RACES.Contains(interest)).OrderBy(o => o.GAMETIME).ToList();

            Dictionary<string, double> unit_sum = new Dictionary<string, double>();

            int mingametime = 0;
            if (gametime == "5min") mingametime = MIN5;
            if (gametime == "10min") mingametime = MIN10;
            if (gametime == "15min") mingametime = MIN15;

            int i = 0;
            int wins = 0;
            double duration = 0;
            foreach (dsreplay rep in replay_sorted)
            {
                if (mingametime > 0 && rep.DURATION < mingametime) continue;

                if (UNITLIST.ContainsKey(rep.REPLAY))
                {
                    foreach (dsplayer pl in rep.PLAYERS)
                    {
                        if (pl.RACE == interest)
                        {
                            if ((MW.cb_player.IsChecked == true && MW.player_list.Contains(pl.NAME)) || MW.cb_player.IsChecked == false)
                            {
                                List<dsunits_player> pl_units = UNITLIST[rep.REPLAY].Where(x => int.Parse(x.PLAYERID) == pl.POS).ToList();
                                if (pl_units.Count > 0)
                                {
                                    if (pl_units.ElementAt(0).UNITS.ContainsKey(gametime))
                                    {
                                        List<KeyValuePair<string, int>> units = pl_units.ElementAt(0).UNITS[gametime];
                                        if (units.Count > 0)
                                        {
                                            if (vs != null)
                                            {
                                                if (rep.GetOpp(pl.POS).RACE != vs)
                                                {
                                                    continue;
                                                }
                                            }
                                            i++;
                                            if (pl.TEAM == rep.WINNER) wins++;
                                            duration += rep.DURATION;
                                            foreach (KeyValuePair<string, int> unit in units)
                                            {
                                                if (unit_sum.ContainsKey(unit.Key))
                                                {
                                                    unit_sum[unit.Key] += unit.Value;
                                                }
                                                else
                                                {
                                                    unit_sum[unit.Key] = unit.Value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            double wr = 0;
            if (i > 0)
            {
                wr = (double)wins * 100 / (double)i;
                wr = Math.Round(wr, 2);
                duration /= i;
                duration /= 22.4;
                duration = Math.Round(duration, 2);
            }
            TimeSpan t = TimeSpan.FromSeconds(duration);
            INFO = new List<dsunits_info>();
            WR = wr;
            INFO.Add(new dsunits_info("winrate", wr.ToString()));
            GAMES = i;
            INFO.Add(new dsunits_info("games", i.ToString()));
            DURATION = t.Minutes + ":" + t.Seconds.ToString("D2") + " min";
            INFO.Add(new dsunits_info("duration", DURATION));
            GAMETIME = "startdate: " + MW.otf_startdate.SelectedDate.Value.ToString("yyyyMMdd") + "; enddate: " + MW.otf_enddate.SelectedDate.Value.ToString("yyyyMMdd");
            INFO.Add(new dsunits_info("gametime", GAMETIME));
            char average = '\u2300';
            sum = "cmdr: " + interest + "; gametime: " + gametime + "; games: " + i.ToString();
            if (vs != null) sum += "; vs: " + vs;
            sum += Environment.NewLine;
            sum += "startdate: " + MW.otf_startdate.SelectedDate.Value.ToString("yyyyMMdd") + "; enddate: " + MW.otf_enddate.SelectedDate.Value.ToString("yyyyMMdd") + Environment.NewLine;
            sum += "winrate: " + wr + "%; " + average.ToString() + " duration: " + t.Minutes + ":" + t.Seconds.ToString("D2") + " min" + Environment.NewLine;
            sum += "-----------------------------------------------------" + Environment.NewLine;
            MW.Dispatcher.Invoke(() =>
            {
                MW.tb_build.Text = sum;
            });
                var list = unit_sum.Keys.ToList();
            list.Sort();
            List<dsunits_grid> list_grid = new List<dsunits_grid>();
            foreach (string unit in list)
            {
                if (unit.StartsWith("Hybrid")) continue;
                if (unit.StartsWith("MercCamp")) continue;
                if (i > 0)
                {
                    double unit_count = 0;
                    unit_count = (double)unit_sum[unit] / (double)i;
                    unit_count = Math.Round(unit_count, 2);
                    if (MW.cb_build_sum.IsChecked == true) unit_count = unit_sum[unit];
                    //sum += unit + " => " + unit_count.ToString() + Environment.NewLine;
                    dsunits_grid dsgrid = new dsunits_grid();
                    dsgrid.UNIT = unit;
                    dsgrid.COUNT = unit_count;
                    list_grid.Add(dsgrid);
                }
            }
            MW.dg_build.ItemsSource = list_grid;
            MW.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(ProcessRows_units));
            return list_grid;
        }

        public void SumSum ()
        {

            Dictionary<string, List<dsunits_grid>> dlist = new Dictionary<string, List<dsunits_grid>>();
            List<dsunits_grid> global_sum = new List<dsunits_grid>();

            foreach (string cmdr in MW.s_races)
            {
                List<dsunits_grid> temp = new List<dsunits_grid>();
                temp = Sum(cmdr, null, "fin");
                global_sum.AddRange(temp);
                dlist.Add(cmdr, temp);
            }
            MW.dg_build.ItemsSource = global_sum;

            MW.Dispatcher.Invoke(() =>
            {
                MW.tb_build.Text = "Global summary";
            });


        }

        public void GenJson ()
        {
            string outdir = @"C:\temp\";

            Dictionary<string, Dictionary<string, List<dsunits_grid>>> bp_list = new Dictionary<string, Dictionary<string, List<dsunits_grid>>>();
            bp_list.Add("5min", null);
            bp_list.Add("10min", null);
            bp_list.Add("15min", null);
            bp_list.Add("fin", null);

            
            foreach (string cmdr in MW.s_races)
            {
                string jsonfile = outdir + "units_Feralan_" + cmdr + ".json";
                string json = "{" + Environment.NewLine;
                json += "\"" + cmdr + "\": {" + Environment.NewLine;
                foreach (string bp in bp_list.Keys)
                {
                    json += "\"" + bp + "\": {" + Environment.NewLine;
                    List<dsunits_grid> temp = new List<dsunits_grid>();
                    temp = Sum(cmdr, null, bp);
                    
                    json += "\"" + cmdr + "\": ";
                    json += JsonConvert.SerializeObject(temp);
                    json += "," + Environment.NewLine;

                    json += "\"" + cmdr + "_info\": ";
                    json += JsonConvert.SerializeObject(INFO);
                    json += "," + Environment.NewLine;

                    foreach (string cmdr_vs in MW.s_races)
                    {
                        json += "\"" + cmdr + "_vs_" + cmdr_vs + "\": ";
                        json += JsonConvert.SerializeObject(Sum(cmdr, cmdr_vs, bp));
                        json += "," + Environment.NewLine;

                        json += "\"" + cmdr + "_vs_" + cmdr_vs + "_info\": ";
                        json += JsonConvert.SerializeObject(INFO);
                        if (cmdr_vs == "Zerg") json += Environment.NewLine;
                        else json += "," + Environment.NewLine;

                    }
                    if (bp != "fin") json += "}," + Environment.NewLine;
                    else json += "}" + Environment.NewLine;

                }
                //if (cmdr == "Zerg") json += "}" + Environment.NewLine;
                //else json += "}," + Environment.NewLine;
                json += "}" + Environment.NewLine;
                json += "}" + Environment.NewLine;
                System.IO.File.WriteAllText(jsonfile, json);
            }
            

            //Console.WriteLine(json);
        }


        public void ProcessRows_units()
        {

            int itct = MW.dg_build.Items.Count;

            double max1 = 0;
            double max2 = 0;
            double max3 = 0;

            List<double> list = new List<double>();
            for (int i = 0; i < itct; i++)
            {
                ///var row = dg_player.ItemContainerGenerator.ContainerFromItem(pl) as DataGridRow;

                dsunits_grid unit = MW.dg_build.Items[i] as dsunits_grid;
                list.Add(unit.COUNT);
            }

            list.Sort(delegate (double x, double y)
            {
                if (x == 0 && y == 0) return 0;
                else if (x == 0) return -1;
                else if (y == 0) return 1;
                else return x.CompareTo(y);
            });

            if (list.Count >= 3)
            {
                max1 = list.ElementAt(list.Count - 1);
                max2 = list.ElementAt(list.Count - 2);
                max3 = list.ElementAt(list.Count - 3);
            }

            for (int i = 0; i < itct; i++)
            {
                ///var row = dg_player.ItemContainerGenerator.ContainerFromItem(pl) as DataGridRow;

                dsunits_grid unit = MW.dg_build.Items[i] as dsunits_grid;

                var row = MW.dg_build.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null)
                {
                    if (unit.COUNT >= max1)
                    {
                        row.Background = Brushes.GreenYellow;
                    }
                    else if (unit.COUNT >= max2)
                    {
                        row.Background = Brushes.YellowGreen;
                    }
                    else if (unit.COUNT >= max3)
                    {
                        row.Background = Brushes.Yellow;
                    } else
                    {
                        //row.Background = Brushes.DarkSlateGray;
                    }
                }
            }

        }

        public void GetData (string csv)
        {
            if (File.Exists(csv))
            {
                System.IO.StreamReader file = new System.IO.StreamReader(csv);
                string line;

                while ((line = file.ReadLine()) != null)
                {
                    int j = 0;
                    

                    Regex rx_fin = new Regex(@"fin");
                    Regex rx_5 = new Regex(@"5min");
                    Regex rx_10 = new Regex(@"10min");
                    Regex rx_15 = new Regex(@"15min");

                    List<dsunits_player> pl_list = new List<dsunits_player>();
                    string id = null;
                    string player_id = null;
                    string bp = null;
                    List<KeyValuePair<string, int>> bp_list = new List<KeyValuePair<string, int>>();
                    Dictionary<string, List<KeyValuePair<string, int>>> units = new Dictionary<string, List<KeyValuePair<string, int>>>();
                    int fin = 0;

                    foreach (string ent in line.Split(';'))
                    {
                        if (j == 0)
                        {
                            id = ent;
                        } else
                        {
                            string[] entent = ent.Split(',');
                            if (entent.Length == 1) 
                            {
                                if (bp != null)
                                {
                                    units.Add(bp, new List<KeyValuePair<string, int>>(bp_list));
                                    dsunits_player pl = new dsunits_player();
                                    pl.ID = id;
                                    pl.PLAYERID = player_id;
                                    pl.UNITS = new Dictionary<string, List<KeyValuePair<string, int>>>(units);
                                    pl_list.Add(pl);
                                    if (fin > 0) pl.FIN = fin;

                                    bp_list.Clear();
                                    units.Clear();
                                    player_id = null;
                                    bp = null;

                                }
                                player_id = ent;
                                
                            } else { 
                            
                                Match m_fin = rx_fin.Match(ent);
                                Match m_5 = rx_5.Match(ent);
                                Match m_10 = rx_10.Match(ent);
                                Match m_15 = rx_15.Match(ent);
                                if (m_fin.Success || m_5.Success || m_10.Success || m_15.Success)
                                {
                                    if (bp != null)
                                    {
                                        units.Add(bp, new List<KeyValuePair<string, int>>(bp_list));
                                        bp_list.Clear();
                                    }
                                    bp = entent[0];

                                    if (m_fin.Success) fin = int.Parse(entent[1]);
                                }
                                else
                                {

                                    if (bp != null && bp.Length > 1)
                                    {
                                        KeyValuePair<string, int> unit = new KeyValuePair<string, int>(entent[0], int.Parse(entent[1]));
                                        bp_list.Add(unit);
                                    }
                                }
                            }



                        }

                        j++;
                    }
                    if (bp != null)
                    {
                        dsunits_player plf = new dsunits_player();
                        plf.ID = id;
                        plf.PLAYERID = player_id;
                        plf.UNITS = new Dictionary<string, List<KeyValuePair<string, int>>>(units);
                        pl_list.Add(plf);
                    }

                    UNITLIST.Add(id, pl_list);

                }
            }
        }
    }

    public class dsunits_player
    {
        public string ID { get; set; } = null;
        public string PLAYERID { get; set; } = null;
        public int FIN { get; set; }
        public Dictionary<string, List<KeyValuePair<string, int>>> UNITS { get; set; } = new Dictionary<string, List<KeyValuePair<string, int>>>();
    }

    public class dsunits_grid
    {
        public string UNIT { get; set; } = "";
        public double COUNT { get; set; } = 0;

        public dsunits_grid ()
        {

        }
    }

    public class dsunits_info
    {
        public string ENT { get; set; } = "";
        public string VALUE { get; set; } = "";

        public dsunits_info()
        {

        }

        public dsunits_info(string ent, string val) : this()
        {
            ENT = ent;
            VALUE = val;
        }
    }
}

