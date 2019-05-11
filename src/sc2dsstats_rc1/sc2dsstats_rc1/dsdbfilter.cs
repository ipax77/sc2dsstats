using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static sc2dsstats_rc1.Win_regex;

namespace sc2dsstats_rc1
{


    class dsdbfilter
    {
        public Win_regex WR { get; set; }
        public MainWindow MW { get; set; }
        public string[] RACES { get; set; }

        public dsdbfilter()
        {

            RACES = new string[]
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
        }

        public dsdbfilter(Win_regex wr, MainWindow mw) : this()
        {
            WR = wr;
            MW = mw;
        }

        public List<dsreplay> Filter()
        {
            List<dsreplay> fil_games = new List<dsreplay>();

            string race = WR.tb_filter.Text;
            fil_games = MW.replays.Where(x => x.RACES.Contains(race)).ToList();
            return fil_games;
        }

        public List<dsreplay> OTF_Filter()
        {
            List<dsreplay> fil_games = new List<dsreplay>();
            string prace = WR.tb_filter.Text;
            string race = "";
            WR.lb_filter.Content = "Filter: ";
            if (WR.cb_filter.SelectedItem.ToString() == "RACE")
            {
                foreach (var bab in RACES)
                {
                    if (bab.ToUpper().Contains(prace.ToUpper()))
                    {
                        race = bab;
                        break;
                    }
                }
                if (race != "")
                {
                    fil_games = MW.replays.Where(x => x.RACES.Contains(race)).ToList();
                    WR.lb_filter.Content += "Race => " + race + " (" + fil_games.Count() + ") ";


                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "DURATION")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.DURATION >= num).ToList();
                    WR.lb_filter.Content += "Duration => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.DURATION <= num).ToList();
                    WR.lb_filter.Content += "Duration => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.DURATION == num).ToList();
                    WR.lb_filter.Content += "Duration => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "ID")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.ID >= num).ToList();
                    WR.lb_filter.Content += "ID => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.ID <= num).ToList();
                    WR.lb_filter.Content += "ID => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.ID == num).ToList();
                    WR.lb_filter.Content += "ID => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "GAMETIME")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.GAMETIME >= num).ToList();
                    WR.lb_filter.Content += "GAMETIME => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.GAMETIME <= num).ToList();
                    WR.lb_filter.Content += "GAMETIME => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    string repregex = Regex.Escape(WR.tb_filter.Text).Replace("\\?", ".").Replace("\\*", ".*");
                    fil_games = MW.replays.Where(x => Regex.IsMatch(x.GAMETIME.ToString(), repregex)).ToList();
                    WR.lb_filter.Content += "GAMETIME => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "MAXLEAVER")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.MAXLEAVER >= num).ToList();
                    WR.lb_filter.Content += "MAXLEAVER => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.MAXLEAVER <= num).ToList();
                    WR.lb_filter.Content += "MAXLEAVER => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.MAXLEAVER == num).ToList();
                    WR.lb_filter.Content += "MAXLEAVER => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "MINKILLSUM")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.MINKILLSUM >= num).ToList();
                    WR.lb_filter.Content += "MINKILLSUM => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.MINKILLSUM <= num).ToList();
                    WR.lb_filter.Content += "MINKILLSUM => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.MINKILLSUM == num).ToList();
                    WR.lb_filter.Content += "MINKILLSUM => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "MININCOME")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.MININCOME >= num).ToList();
                    WR.lb_filter.Content += "MININCOME => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.MININCOME <= num).ToList();
                    WR.lb_filter.Content += "MININCOME => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.MININCOME == num).ToList();
                    WR.lb_filter.Content += "MININCOME => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "MINARMY")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.MINARMY >= num).ToList();
                    WR.lb_filter.Content += "MINARMY => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.MINARMY <= num).ToList();
                    WR.lb_filter.Content += "MINARMY => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.MINARMY == num).ToList();
                    WR.lb_filter.Content += "MINARMY => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "WINNER")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    fil_games = MW.replays.Where(x => x.WINNER >= num).ToList();
                    WR.lb_filter.Content += "WINNER => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = MW.replays.Where(x => x.WINNER <= num).ToList();
                    WR.lb_filter.Content += "WINNER => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = MW.replays.Where(x => x.WINNER == num).ToList();
                    WR.lb_filter.Content += "WINNER => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "REPLAY")
            {
                string repregex = Regex.Escape(WR.tb_filter.Text).Replace("\\?", ".").Replace("\\*", ".*");
                fil_games = MW.replays.Where(x => Regex.IsMatch(x.REPLAY, repregex)).ToList();
                WR.lb_filter.Content += "REPLAY => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "MATCHUP" || WR.cb_filter.SelectedItem.ToString() == "MATCHUP PLAYER") 
            {
                string filter = WR.tb_filter.Text;
                List<string> matchup = new List<string>(filter.Split('|').ToList());

                if (matchup.Count == 2)
                {
                    List<dsreplay> temp = new List<dsreplay>();
                    foreach (dsreplay rep in MW.replays)
                    {
                        foreach (dsplayer pl in rep.PLAYERS)
                        {
                            if (MW.player_list.Contains(pl.NAME) || WR.cb_filter.SelectedItem.ToString() == "MATCHUP")
                            {
                                if (pl.RACE == matchup[0])
                                {
                                    if (rep.GetOpp(pl.REALPOS).RACE == matchup[1])
                                    {
                                        temp.Add(rep);
                                    }
                                }
                            }
                        }
                    }
                    fil_games = new List<dsreplay>(temp);
                }
            }



                return fil_games;
        }




    }

    class DSfilter
    {
        public MainWindow MW { get; set; }
        public dsfilter FIL { get; set; }

        public DSfilter()
        {
            FIL = new dsfilter();
        }

        public DSfilter(MainWindow mw) : this()
        {
            MW = mw;
        }

        public List<dsreplay> Filter(List<dsreplay> replays)
        {
            List<dsreplay> fil_replays = new List<dsreplay>(replays);
            List<dsreplay> tmprep = new List<dsreplay>();
            FIL.GAMES = replays.Count;

            if (MW.cb_beta.IsChecked == true)
            {
                FIL.Beta = replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.REPLAY.Contains("Beta")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Beta -= fil_replays.Count;
            }

            if (MW.cb_hots.IsChecked == true)
            {
                FIL.Hots = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.REPLAY.Contains("HotS")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Hots -= fil_replays.Count;
            }

            if (MW.cb_std.IsChecked == false)
            {
                FIL.Std = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => !x.PLAYERS.Exists(y => y.RACE == "Protoss" || y.RACE == "Terran" || y.RACE == "Zerg")).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Std -= fil_replays.Count;
            }

            if (MW.cb_all.IsChecked == false)
            {
                string sd = MW.otf_startdate.SelectedDate.Value.ToString("yyyyMMdd");
                sd += "000000";
                double sd_int = double.Parse(sd);
                string ed = MW.otf_enddate.SelectedDate.Value.ToString("yyyyMMdd");
                ed += "999999";
                double ed_int = double.Parse(ed);

                FIL.Gametime = fil_replays.Count;
                tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME > sd_int)).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME < ed_int)).ToList());
                fil_replays = new List<dsreplay>(tmprep);
                FIL.Gametime -= fil_replays.Count;

            }

            if (MW.cb_duration.IsChecked == true)
            {
                string mod = MW.tb_duration.Text.Substring(0, 1);
                string snum = MW.tb_duration.Text.Substring(1, MW.tb_duration.Text.Length - 1);
                double num = 0;
                FIL.Duration = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(MW.tb_duration.Text);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.DURATION > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Duration -= tmprep.Count;
            }

            if (MW.cb_leaver.IsChecked == true)
            {
                string mod = MW.tb_leaver.Text.Substring(0, 1);
                string snum = MW.tb_leaver.Text.Substring(1, MW.tb_leaver.Text.Length - 1);
                double num = 0;
                FIL.Leaver = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(MW.tb_leaver.Text);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MAXLEAVER < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Leaver -= tmprep.Count;
            }

            if (MW.cb_army.IsChecked == true)
            {
                string mod = MW.tb_army.Text.Substring(0, 1);
                string snum = MW.tb_army.Text.Substring(1, MW.tb_army.Text.Length - 1);
                double num = 0;
                FIL.Army = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.ARMY > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.ARMY < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(MW.tb_army.Text);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MINARMY > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Army -= tmprep.Count;
            }

            if (MW.cb_income.IsChecked == true)
            {
                string mod = MW.tb_income.Text.Substring(0, 1);
                string snum = MW.tb_income.Text.Substring(1, MW.tb_income.Text.Length - 1);
                double num = 0;
                FIL.Income = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.INCOME > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.INCOME < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(MW.tb_income.Text);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MININCOME > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Income -= tmprep.Count;
            }

            if (MW.cb_killsum.IsChecked == true)
            {
                string mod = MW.tb_killsum.Text.Substring(0, 1);
                string snum = MW.tb_killsum.Text.Substring(1, MW.tb_killsum.Text.Length - 1);
                double num = 0;
                FIL.Killsum = tmprep.Count;

                try
                {
                    num = double.Parse(snum);
                }
                catch { }

                if (mod == ">")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.KILLSUM > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else if (mod == "<")
                {
                    tmprep = new List<dsreplay>(fil_replays.Where(x => x.PLAYERS.Exists(y => y.KILLSUM < num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);
                }
                else
                {
                    try
                    {
                        num = double.Parse(MW.tb_killsum.Text);
                    }
                    catch { }
                    tmprep = new List<dsreplay>(fil_replays.Where(x => (x.MINKILLSUM > num)).ToList());
                    fil_replays = new List<dsreplay>(tmprep);

                }
                FIL.Killsum -= tmprep.Count;
            }




            //fil_replays = fil_replays.Distinct().ToList();
            return fil_replays;
        }
    }
}
