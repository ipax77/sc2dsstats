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

        public dsdbfilter(Win_regex wr) : this()
        {
            WR = wr;
        }

        public List<myGame> Filter()
        {
            List<myGame> fil_games = new List<myGame>();

            string race = WR.tb_filter.Text;
            fil_games = WR.games.Where(x => x.RACES.Contains(race)).ToList();
            return fil_games;
        }

        public List<myGame> OTF_Filter()
        {
            List<myGame> fil_games = new List<myGame>();
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
                    fil_games = WR.games.Where(x => x.RACES.Contains(race)).ToList();
                    WR.lb_filter.Content += "Race => " + race + " (" + fil_games.Count() + ") ";


                }
            } else if (WR.cb_filter.SelectedItem.ToString() == "DURATION")
            {
                string mod = WR.tb_filter.Text.Substring(0, 1);
                string snum = WR.tb_filter.Text.Substring(1, WR.tb_filter.Text.Length - 1);
                double num = 0;
                try
                {
                    num = double.Parse(snum);
                } catch { }

                if (mod == ">")
                {
                    fil_games = WR.games.Where(x => x.DURATION >= num).ToList();
                    WR.lb_filter.Content += "Duration => >" + snum + " (" + fil_games.Count() + ") ";
                } else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.DURATION <= num).ToList();
                    WR.lb_filter.Content += "Duration => <" + snum + " (" + fil_games.Count() + ") ";
                } else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    } catch { }
                    fil_games = WR.games.Where(x => x.DURATION == num).ToList();
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
                    fil_games = WR.games.Where(x => x.ID >= num).ToList();
                    WR.lb_filter.Content += "ID => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.ID <= num).ToList();
                    WR.lb_filter.Content += "ID => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = WR.games.Where(x => x.ID == num).ToList();
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
                    fil_games = WR.games.Where(x => x.GAMETIME >= num).ToList();
                    WR.lb_filter.Content += "GAMETIME => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.GAMETIME <= num).ToList();
                    WR.lb_filter.Content += "GAMETIME => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    string repregex = Regex.Escape(WR.tb_filter.Text).Replace("\\?", ".").Replace("\\*", ".*");
                    fil_games = WR.games.Where(x => Regex.IsMatch(x.GAMETIME.ToString(), repregex)).ToList();
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
                    fil_games = WR.games.Where(x => x.MAXLEAVER >= num).ToList();
                    WR.lb_filter.Content += "MAXLEAVER => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.MAXLEAVER <= num).ToList();
                    WR.lb_filter.Content += "MAXLEAVER => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = WR.games.Where(x => x.MAXLEAVER == num).ToList();
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
                    fil_games = WR.games.Where(x => x.MINKILLSUM >= num).ToList();
                    WR.lb_filter.Content += "MINKILLSUM => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.MINKILLSUM <= num).ToList();
                    WR.lb_filter.Content += "MINKILLSUM => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = WR.games.Where(x => x.MINKILLSUM == num).ToList();
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
                    fil_games = WR.games.Where(x => x.MININCOME >= num).ToList();
                    WR.lb_filter.Content += "MININCOME => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.MININCOME <= num).ToList();
                    WR.lb_filter.Content += "MININCOME => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = WR.games.Where(x => x.MININCOME == num).ToList();
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
                    fil_games = WR.games.Where(x => x.MINARMY >= num).ToList();
                    WR.lb_filter.Content += "MINARMY => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.MINARMY <= num).ToList();
                    WR.lb_filter.Content += "MINARMY => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = WR.games.Where(x => x.MINARMY == num).ToList();
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
                    fil_games = WR.games.Where(x => x.WINNER >= num).ToList();
                    WR.lb_filter.Content += "WINNER => >" + snum + " (" + fil_games.Count() + ") ";
                }
                else if (mod == "<")
                {
                    fil_games = WR.games.Where(x => x.WINNER <= num).ToList();
                    WR.lb_filter.Content += "WINNER => <" + snum + " (" + fil_games.Count() + ") ";
                }
                else
                {
                    try
                    {
                        num = double.Parse(WR.tb_filter.Text);
                    }
                    catch { }
                    fil_games = WR.games.Where(x => x.WINNER == num).ToList();
                    WR.lb_filter.Content += "WINNER => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
                }
            }
            else if (WR.cb_filter.SelectedItem.ToString() == "REPLAY")
            {
                string repregex = Regex.Escape(WR.tb_filter.Text).Replace("\\?", ".").Replace("\\*", ".*");
                fil_games = WR.games.Where(x => Regex.IsMatch(x.REPLAY, repregex)).ToList();
                WR.lb_filter.Content += "REPLAY => " + WR.tb_filter.Text + " (" + fil_games.Count() + ") ";
            }



            return fil_games;
        }
    }
}
