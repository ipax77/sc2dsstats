using System;
using System.Collections.Generic;
using System.Text;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using sc2dsstats.lib.Db;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.lib.Data
{
    public class DSDbBuilds
    {
        static Regex rx_star = new Regex(@"(.*)Starlight(.*)", RegexOptions.Singleline);
        static Regex rx_light = new Regex(@"(.*)Lightweight(.*)", RegexOptions.Singleline);
        static Regex rx_hero = new Regex(@"Hero(.*)WaveUnit", RegexOptions.Singleline);
        static Regex rx_mp = new Regex(@"(.*)MP$", RegexOptions.Singleline);

        public void GetBuild()
        {
            string cmdr = "Swann";
            string interest = "Abathur";
            string bp = "MIN10";
            
            List<KeyValuePair<string, float>> Units = new List<KeyValuePair<string, float>>();
            
            DateTime t = DateTime.Now;
            
            using (var context = new DSReplayContext())
            {
                var replays = DBReplayFilter.Filter(new DSoptions(), context);

                float games = 0;
                

                var result = from r in replays
                             from t1 in r.DSPlayer
                             where t1.RACE == cmdr
                             from u1 in t1.DSUnit
                             where u1.BP == bp
                             select new
                             {
                                 r.ID,
                                 u1.Name,
                                 u1.Count
                             };
                games = result.Select(s => s.ID).Distinct().Count();
                HashSet<string> units = result.Select(s => s.Name).ToHashSet();
                foreach (string unit in units)
                    Units.Add(new KeyValuePair<string, float>(unit, MathF.Round((float)result.Where(x => x.Name == unit).Sum(s => s.Count) / games, 2)));
            }

            var info = new StringBuilder();
            info.AppendLine($"Time: {(DateTime.Now - t).TotalSeconds}");
            foreach (var ent in Units.OrderByDescending(o => o.Value))
            {
                info.AppendLine($"{ent.Key} => {ent.Value}");
            }

            Console.WriteLine(info);

        }

        public string FixUnitName(string unit)
        {
            if (unit == "TrophyRiftPremium") return "";
            // abathur unknown
            if (unit == "ParasiticBombRelayDummy") return "";
            // raynor viking
            if (unit == "VikingFighter" || unit == "VikingAssault") return "Viking";
            if (unit == "DuskWings") return "DuskWing";
            // stukov lib
            if (unit == "InfestedLiberatorViralSwarm") return "InfestedLiberator";
            // Tychus extra mins
            if (unit == "MineralIncome") return "";
            // Zagara
            if (unit == "InfestedAbomination") return "Aberration";
            // Horner viking
            if (unit == "HornerDeimosVikingFighter" || unit == "HornerDeimosVikingAssault") return "HornerDeimosViking";
            if (unit == "HornerAssaultGalleonUpgraded") return "HornerAssaultGalleon";
            // Terrran thor
            if (unit == "ThorAP") return "Thor";

            foreach (string cmdr in DSdata.s_races_cmdr)
            {
                if (unit.EndsWith(cmdr))
                    return unit.Replace(cmdr, "");
                if (unit.StartsWith(cmdr))
                    return unit.Replace(cmdr, "");
            }

            Match m = rx_star.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString() + m.Groups[2].ToString();

            m = rx_light.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString() + m.Groups[2].ToString();

            m = rx_hero.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString();

            m = rx_mp.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString();

            return unit;
        }
    }
}
