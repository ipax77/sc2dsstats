using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats_rc1
{
    class dsradar
    {
        string HT_PRE { get; set; }
        string HT_DATA { get; set; }
        string HT_POST { get; set; }
        MainWindow MW { get; set; }


        public dsradar()
        {
            string curDir = Directory.GetCurrentDirectory();

            string myhtml_pre =
"<!DOCTYPE html>" + Environment.NewLine +
"<html>" + Environment.NewLine +
"<head>" + Environment.NewLine +
"<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" charset=\"UTF-8\" />" + Environment.NewLine +
"<title> externes JavaScript in HTML einbinden</title>" + Environment.NewLine +
"</head>" + Environment.NewLine +
"<script src=\"" + curDir + "/Scripts/Chart.min.js" + "\"></script>" + Environment.NewLine +
"<body style=\"background - color:BlanchedAlmond; \">" + Environment.NewLine +
 "<div id=\"chartContainer\"></div >" + Environment.NewLine +
"<div style=\"width:100%\">" + Environment.NewLine +
"<canvas id=\"cannvas\"></canvas>" + Environment.NewLine +
"</div>" + Environment.NewLine +
"<script type=\"text/javascript\">" + Environment.NewLine +
"var randomScalingFactor = function() {" + Environment.NewLine +
"return Math.round(Math.random() * 100);" + Environment.NewLine +
"};" + Environment.NewLine +
"var ctx=document.getElementById(\"cannvas\").getContext(\"2d\");" + Environment.NewLine +
"var color = Chart.helpers.color;" + Environment.NewLine +
"var config = {" + Environment.NewLine +
"type: 'radar'," + Environment.NewLine;

string myhtml_data =
"data:" + Environment.NewLine +
"{" + Environment.NewLine +
"labels: [['Eating', 'Dinner'], ['Drinking', 'Water'], 'Sleeping', ['Designing', 'Graphics'], 'Coding', 'Cycling', 'Running']," + Environment.NewLine +
"datasets: [{" + Environment.NewLine +
"label: 'My First dataset'," + Environment.NewLine +
"backgroundColor: 'rgba(255, 99, 132, 0.2)'," + Environment.NewLine +
"borderColor: 'rgba(255,99,132,1)'," + Environment.NewLine +
"pointBackgroundColor: 'rgba(255, 159, 64, 0.2)'," + Environment.NewLine +
"data: [" + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()" + Environment.NewLine +
"]" + Environment.NewLine +
"}, {" + Environment.NewLine +
"label: 'My Second dataset'," + Environment.NewLine +
"backgroundColor: 'rgba(54, 162, 235, 0.2)'," + Environment.NewLine +
"borderColor: 'rgba(54, 162, 235, 1)'," + Environment.NewLine +
"pointBackgroundColor: 'rgba(153, 102, 255, 0.2)'," + Environment.NewLine +
"data: [" + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()" + Environment.NewLine +
"]" + Environment.NewLine +
"}]" + Environment.NewLine +
"}," + Environment.NewLine;

string myhtml_post =
"options: {" + Environment.NewLine +
"legend: {" + Environment.NewLine +
"position: 'top'," + Environment.NewLine +
"}," + Environment.NewLine +
"title: {" + Environment.NewLine +
"display: true," + Environment.NewLine +
"text: 'cmdr synergy'" + Environment.NewLine +
"}," + Environment.NewLine +
"scale: {" + Environment.NewLine +
"ticks: {" + Environment.NewLine +
"beginAtZero: true" + Environment.NewLine +
"}" + Environment.NewLine +
"}" + Environment.NewLine +
"}" + Environment.NewLine +
"};	" + Environment.NewLine +
"var myChart = new Chart(ctx, config);" + Environment.NewLine +
"</script>" + Environment.NewLine +
"</body>" + Environment.NewLine +
"</html>" + Environment.NewLine;

            HT_PRE = myhtml_pre;
            HT_DATA = myhtml_data;
            HT_POST = myhtml_post;
        }

        public dsradar(MainWindow mw) : this()
        {
            MW = mw;
        }

        public string GenerateHTML(Dictionary<string, List<KeyValuePair<string, double>>> mylist)
        {
            string myhtml = HT_PRE;

            string mylabels = "labels: [";
            string label_temp = "";
            string mydataset = "";
            int i = 0;
            foreach (var synlist in mylist)
            {
                i++;
                mydataset +=
    "{" + Environment.NewLine +
    "label: '" + synlist.Key + "'," + Environment.NewLine +
    //"backgroundColor: 'rgba(255, 99, 132, 0.2)'," + Environment.NewLine +
    //"borderColor: 'rgba(255,99,132,1)'," + Environment.NewLine +
    //"pointBackgroundColor: 'rgba(255, 159, 64, 0.2)'," + Environment.NewLine +
    GetColor(i) +
    "data: [" + Environment.NewLine;

                label_temp = "";
                foreach (var bab in synlist.Value)
                {
                    label_temp += "'" + bab.Key.ToString() + "',";
                    mydataset += bab.Value.ToString(new CultureInfo("en-US")) + "," + Environment.NewLine;
                }
                label_temp = label_temp.Remove(label_temp.Length - 1);
                label_temp += "],";

                mydataset += "]" + Environment.NewLine +
                "}" + Environment.NewLine;
                mydataset += ",";
            }
            mylabels += label_temp;
            //if (mydataset.Length > 1) mydataset = mydataset.Remove(mydataset.Length - 1);

            string myhtml_data =
"data:" + Environment.NewLine +
"{" + Environment.NewLine +
mylabels + Environment.NewLine +
"datasets: [" + Environment.NewLine +
mydataset +
"]" + Environment.NewLine +
"}," + Environment.NewLine;

            myhtml += myhtml_data;
            myhtml += HT_POST;

            return myhtml;
        }

        public string GetColor(int myi)
        {
            string temp_col;
            if (myi == 1)
            {
                temp_col = "26, 94, 203";
            } else if (myi == 2) {
                temp_col = "203, 26, 59";
            }
            else if (myi == 2)
            {
                temp_col = "203, 26, 59";
            }
            else if (myi == 3)
            {
                temp_col = "47, 203, 26";
            }
            else if (myi == 4)
            {
                temp_col = "26, 203, 191";
            }
            else if (myi == 5)
            {
                temp_col = "203, 26, 177";
            }
            else if (myi == 6)
            {
                temp_col = "203, 194, 26";
            } else
            {
                temp_col = "72, 69, 9";
            }

            string mycolor =
"backgroundColor: 'rgba("+ temp_col + ", 0.2)'," + Environment.NewLine +
"borderColor: 'rgba(" + temp_col + ",1)'," + Environment.NewLine +
"pointBackgroundColor: 'rgba(" + temp_col + ", 0.2)'," + Environment.NewLine;
            return mycolor;
        }

        public string GetHTML(List<string> cmdrs, List<dsreplay> replays)
        {
            return GenerateHTML(GetSynList(cmdrs, replays));
        }

        public Dictionary<string, List<KeyValuePair<string, double>>> GetSynList (List<string> cmdrs, List<dsreplay> replays)
        {
            var mylist = new Dictionary<string, List<KeyValuePair<string, double>>>();

            dssynergy syn = new dssynergy();
            dsstats_race synrace = new dsstats_race();
            foreach (dsreplay replay in replays)
            {
                if (replay.PLAYERCOUNT != 6) continue;
                foreach (dsplayer player in replay.PLAYERS)
                {
                    syn.RACES[player.RACE].GAMES++;
                    List<dsplayer> teammates = new List<dsplayer>(GetTeammates(player, replay));
                    foreach (dsplayer teammate in teammates)
                    {
                        synrace = syn.RACES[player.RACE].objRace(teammate.RACE);
                        synrace.RGAMES++;
                        if (player.TEAM == replay.WINNER)
                        {
                            synrace.RWINS++;
                        }
                    }
                }
            }

            foreach (string cmdr in cmdrs)
            {
                List<KeyValuePair<string, double>> synrow = new List<KeyValuePair<string, double>>();
                foreach (dsstats_race intsyn in syn.RACES[cmdr].LRACE)
                {
                    double wr = intsyn.GetWR();
                    string syncmdr = intsyn.RACE;
                    KeyValuePair<string, double> mystat = new KeyValuePair<string, double>(syncmdr, wr);
                    if (MW.cb_std.IsChecked == false)
                    {
                        if (intsyn.RACE == "Protoss" || intsyn.RACE == "Terran" || intsyn.RACE == "Zerg")
                        {
                            continue;
                        }
                    }
                    synrow.Add(mystat);
                }
                mylist.Add(key: cmdr, value: synrow);
            }

            return mylist;
        }

        public List<dsplayer> GetTeammates(dsplayer pl, dsreplay rep)
        {
            List<dsplayer> teammates = new List<dsplayer>();
            if (pl.POS == 1)
            {
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 2));
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 3));
            } else if (pl.POS ==2)
            {
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 1));
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 3));
            }
            else if (pl.POS == 3)
            {
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 1));
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 2));
            }
            else if (pl.POS == 4)
            {
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 5));
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 6));
            }
            else if (pl.POS == 5)
            {
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 4));
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 6));
            }
            else if (pl.POS == 6)
            {
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 4));
                teammates.Add(rep.PLAYERS.Find(x => x.POS == 5));
            }
            return teammates;
        }

    }

    class dssynergy
    {
        public Dictionary<string, dsstats> RACES { get; set; }
        public Dictionary<string, dsstats> SYNRACES { get; set; }

        public dssynergy()
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

            RACES = new Dictionary<string, dsstats>();
            SYNRACES = new Dictionary<string, dsstats>();


            foreach (string r in s_races)
            {
                dsstats mystat = new dsstats();
                mystat.Init();
                mystat.GAMES = 0;
                mystat.WINS = 0;
                RACES.Add(key: r, value: mystat);
                SYNRACES.Add(key: r, value: mystat);
            }

        }

    }

}
