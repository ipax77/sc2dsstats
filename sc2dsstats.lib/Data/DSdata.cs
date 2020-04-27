using Newtonsoft.Json;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace sc2dsstats.lib.Data
{
    public static class DSdata
    {
        public static UserConfig Config = new UserConfig();
        public static ServerConfig ServerConfig = new ServerConfig();
        public static List<DatasetInfo> Datasets = new List<DatasetInfo>();
        public static ReplaysLoadedEventArgs Status = new ReplaysLoadedEventArgs();

        public static Version DesktopVersion = new Version("2.0.8");
        public static DesktopStatus DesktopStatus = new DesktopStatus();
        public static bool DesktopUpdateAvailable = false;

        public static int OptID = 0;
        public static ConcurrentDictionary<int, List<string>> Telemetrie = new ConcurrentDictionary<int, List<string>>();

        public static bool IsMySQL = false;
        public static Regex rx_ds = new Regex(@"(Direct Strike.*)\.SC2Replay$|(DST.*)\.SC2Replay$", RegexOptions.Singleline);
        public static PlayerStats PlayerStats;

        public static void Init()
        {
            var rootDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            DSdata.Units = System.Text.Json.JsonSerializer.Deserialize<List<UnitModelBase>>(File.ReadAllText(rootDir + "/json/dataunits.json"));
            DSdata.Upgrades = System.Text.Json.JsonSerializer.Deserialize<List<UnitModelBase>>(File.ReadAllText(rootDir + "/json/upgrademap.json"));
            DSdata.Objectives = JsonConvert.DeserializeObject<List<Objective>>(File.ReadAllText(rootDir + "/json/objectives.json"));

            foreach (string cmdr in s_races)
            {
                CmdrBtnStyle += new StringBuilder("" + Environment.NewLine +
                    cmdr + "Button:before {" + Environment.NewLine +
                    " background-image : url(" + GetIcon(cmdr) + ");" + Environment.NewLine +
                    "}" + Environment.NewLine
                    );
            }
        }


        public static string[] s_races { get; } = new string[]
        {
                "Abathur",
                 "Alarak",
                 "Artanis",
                 "Dehaka",
                 "Fenix",
                 "Horner",
                 "Karax",
                 "Kerrigan",
                 "Mengsk",
                 "Nova",
                 "Raynor",
                 "Stetmann",
                 "Stukov",
                 "Swann",
                 "Tychus",
                 "Vorazun",
                 "Zagara",
                 "Protoss",
                 "Terran",
                 "Zerg"
        };

        public static string[] s_races_cmdr { get; } = new string[]
        {
                "Abathur",
                 "Alarak",
                 "Artanis",
                 "Dehaka",
                 "Fenix",
                 "Horner",
                 "Karax",
                 "Kerrigan",
                 "Mengsk",
                 "Nova",
                 "Raynor",
                 "Stetmann",
                 "Stukov",
                 "Swann",
                 "Tychus",
                 "Vorazun",
                 "Zagara"
        };

        public static string[] s_chartmodes { get; } = new string[]
        {
            "Winrate",
            "MVP",
            "DPS",
            "Synergy",
            "AntiSynergy",
            "Timeline"
        };

        public static string[] s_timespans { get; } = new string[]
        {
            "This Month",
            "Last Month",
            "This Year",
            "Last Year",
            "ALL"
        };

        public static string[] s_gamemodes { get; } = new string[]
        {
            "GameModeBrawlCommanders",
            "GameModeBrawlStandard",
            "GameModeCommanders",
            "GameModeCommandersHeroic",
            "GameModeGear",
            "GameModeSabotage",
            "GameModeStandard",
            "GameModeSwitch"
        };

        public static string[] s_breakpoints { get; } = new string[]
        {
                 "MIN5",
                 "MIN10",
                 "MIN15",
                 "ALL",
        };

        public static string[] s_builds { get; } = new string[]
        {
            "PAX",
            "Feralan",
            "Panzerfaust"
        };

        public static string[] s_players { get; } = new string[]
        {
            "player",
            "player1",
            "player2",
            "player3",
            "player4",
            "player5",
            "player6"
        };

        public static Dictionary<string, string> INFO { get; } = new Dictionary<string, string>() {
            { "Winrate", "Winrate: Shows the winrate for each commander. When selecting a commander on the left it shows the winrate of the selected commander when matched vs the other commanders." },
            { "MVP", "MVP: Shows the % for the most ingame damage for each commander based on mineral value killed. When selecting a commander on the left it shows the mvp of the selected commander when matched vs the other commanders." },
            { "DPS", "DPS: Shows the damage delt for each commander based on mineral value killed / game duration (or army value, or minerals collected). When selecting a commander on the left it shows the damage of the selected commander when matched vs the other commanders." },
            { "Synergy", "Synergy: Shows the winrate for the selected commander when played together with the other commanders"},
            { "AntiSynergy", "Antisynergy: Shows the winrate for the selected commander when played vs the other commanders (at any position)"},
            { "Build", "Builds: Shows the average unit count for the selected commander at the selected game duration. When selecting a vs commander it shows the average unit count of the selected commander when matched vs the other commanders."},
            { "Timeline", "Timeline: Shows the winrate development for the selected commander over the given time period."},
        };

        public static string color_max1 = "Crimson";
        public static string color_max2 = "OrangeRed";
        public static string color_max3 = "Chocolate";
        public static string color_def = "#FFCC00";
        public static string color_info = "#46a2c9";
        public static string color_diff1 = "Crimson";
        public static string color_diff2 = color_info;
        public static string color_null = color_diff2;
        public static string color_bg = "#0e0e24";
        public static string color_plbg_def = "#D8D8D8";
        public static string color_plbg_player = "#2E9AFE";
        public static string color_plbg_mvp = "#FFBF00";


        public static Dictionary<string, string> CMDRcolor { get; } = new Dictionary<string, string>()
        {
            {     "global", "#0000ff"  },
            {     "Abathur", "#266a1b" },
            {     "Alarak", "#ab0f0f" },
            {     "Artanis", "#edae0c" },
            {     "Dehaka", "#d52a38" },
            {     "Fenix", "#fcf32c" },
            {     "Horner", "#ba0d97" },
            {     "Karax", "#1565c7" },
            {     "Kerrigan", "#b021a1" },
            {     "Mengsk", "#a46532" },
            {     "Nova", "#f6f673" },
            {     "Raynor", "#dd7336" },
            {     "Stetmann", "#ebeae8" },
            {     "Stukov", "#663b35" },
            {     "Swann", "#ab4f21" },
            {     "Tychus", "#150d9f" },
            {     "Vorazun", "#07c543" },
            {     "Zagara", "#b01c48" },
            {     "Protoss", "#fcc828"   },
            {     "Terran", "#242331"   },
            {     "Zerg", "#440e5f"   }
        };

        public static string CmdrBtnStyle = "";

        public static Dictionary<string, string> s_builds_hash = new Dictionary<string, string>()
        {
            { "b33aef3fcc740b0d67eda3faa12c0f94cef5213fe70921d72fc2bfa8125a5889", "PAX" },
            { "e2dfd75fcad1c454cfb2526fae4f3feb5e901039f7d366f69094c0d16a12e338", "Feralan" },
            { "bd78339bb80c299a6c82812d9d4547d09cf15b0e8bb99b38090dc3bc4a5af8b5", "Panzerfaust"}
        };

        public static string Enddate { get; set; } = DateTime.Today.AddDays(1).ToString("yyyyMMdd");

        public static string GetIcon(string race)
        {
            string r = race.ToLower();
            //r = "images/btn-unit-hero-" + r + ".png";
            r = "_content/sc2dsstats.shared/images/btn-unit-hero-" + r + ".png";

            return r;
        }

        //public static double MIN5 = 6720;
        //public static double MIN10 = 13440;
        //public static double MIN15 = 20160;

        public static double MIN5 = 6240;
        public static double MIN10 = 13440;
        public static double MIN15 = 20640;

        public static Dictionary<string, double> BreakpointMid = new Dictionary<string, double>()
{
        { "MIN5", MIN5 },
        { "MIN10", MIN10 },
        { "MIN15", MIN15 },
        { "ALL", 0 }
    };

        public static string[] s_units { get; set; }

        public static List<UnitModelBase> Units { get; set; } = new List<UnitModelBase>();
        public static List<UnitModelBase> Upgrades { get; set; } = new List<UnitModelBase>();
        public static List<Objective> Objectives { get; set; } = new List<Objective>();
    }
}
