using sc2dsstats.lib.Data;
using sc2dsstats.lib.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace sc2dsstats.lib.Models
{
    public class WebGameModel
    {
        public string Id { get; set; } = DateTime.MinValue.ToString("yyyy/MM/dd");
        public string Duration { get; set; } = "00:00 min";
        public string Player { get; set; } = "";
        public string MVP { get; set; } = "";
        public string Mode { get; set; } = "unknown";
        public List<string> Mid { get; set; } = new List<string>() { "0%", "0%" };
        public Dictionary<string, double> BreakpointMid { get; set; } = new Dictionary<string, double>();
        public Dictionary<int, Dictionary<string, HashSet<string>>> Upgrades { get; set; } = new Dictionary<int, Dictionary<string, HashSet<string>>>();
        public Dictionary<int, Dictionary<string, Dictionary<string, int>>> Units { get; set; } = new Dictionary<int, Dictionary<string, Dictionary<string, int>>>();

        public static Vector2 center = new Vector2(128, 120);

        public WebGameModel()
        {
        }

        public void Init(DSReplay replay, DSoptions _options)
        {
            DSPlayer pl = null;
            if (DSdata.Config.Players.Any())
            {
                List<string> activePlayers = _options.Players.Where(x => x.Value == true).Select(s => s.Key).ToList();
                pl = replay.DSPlayer.Where(x => activePlayers.Contains(x.NAME)).FirstOrDefault();
            }
            else
                pl = replay.DSPlayer.Where(x => x.NAME.Length == 64).FirstOrDefault();



            Id = $"ID {replay.ID} - {replay.GAMETIME.ToString("yyyy/MM/dd")}";
            Duration = "Duration " + (TimeSpan.FromSeconds(replay.DURATION).Hours > 0 ? TimeSpan.FromSeconds(replay.DURATION).ToString(@"hh\:mm\:ss") + " h" : TimeSpan.FromSeconds(replay.DURATION).ToString(@"mm\:ss") + " min");
            if (pl != null)
                Player = "Player #" + pl.REALPOS;
            MVP = "MVP #" + replay.DSPlayer.Where(x => x.KILLSUM == replay.MAXKILLSUM).First().REALPOS;
            Mode = "Mode: " + replay.GAMEMODE.Substring(8);



            BreakpointMid = new Dictionary<string, double>(DSdata.BreakpointMid);
            BreakpointMid["ALL"] = replay.DURATION * 22.4;
            SetMid(replay, _options.GameBreakpoint);
            _options.GameBreakpoint = "ALL";
            foreach (DSPlayer bpl in replay.DSPlayer)
            {
                HashSet<string> ups = new HashSet<string>();
                Upgrades[bpl.REALPOS] = new Dictionary<string, HashSet<string>>();
                Units[bpl.REALPOS] = new Dictionary<string, Dictionary<string, int>>();
                List<string> bpdelete = new List<string>();
                foreach (var ent in BreakpointMid.OrderBy(o => o.Value))
                {
                    if (ent.Value > BreakpointMid["ALL"])
                    {
                        bpdelete.Add(ent.Key);
                        continue;
                    }
                    ups.UnionWith(GetUpgrades(bpl.Breakpoints.FirstOrDefault(f => f.Breakpoint == ent.Key)).ToHashSet());
                    Upgrades[bpl.REALPOS][ent.Key] = new HashSet<string>(ups);
                    Units[bpl.REALPOS][ent.Key] = new Dictionary<string, int>(GetUnits(bpl.Breakpoints.FirstOrDefault(f => f.Breakpoint == ent.Key)));
                }
                foreach (string dkey in bpdelete)
                    BreakpointMid.Remove(dkey);
            }
        }

        public void SetMid(DSReplay replay, string Breakpoint)
        {
            double bpgameloop = BreakpointMid[Breakpoint];
            if (replay.Middle == null || !replay.Middle.Any())
            {
                DSPlayer mpl1 = replay.DSPlayer.FirstOrDefault(f => f.TEAM == 0);
                if (mpl1 != null)
                {
                    DbBreakpoint bp = mpl1.Breakpoints.FirstOrDefault(x => x.Breakpoint == Breakpoint);
                    if (bp != null)
                        Mid[0] = Math.Round(bp.Mid * 100 / bpgameloop, 2).ToString("00.00") + "%";
                }
                DSPlayer mpl2 = replay.DSPlayer.FirstOrDefault(f => f.TEAM == 1);
                if (mpl2 != null)
                {
                    DbBreakpoint bp = mpl2.Breakpoints.FirstOrDefault(x => x.Breakpoint == Breakpoint);
                    if (bp != null)
                        Mid[1] = Math.Round(bp.Mid * 100 / bpgameloop, 2).ToString("00.00") + "%";
                }
            }
            else
            {
                int midt1 = replay.GetMiddle((int)bpgameloop, 0);
                int midt2 = replay.GetMiddle((int)bpgameloop, 1);
                Mid[0] = Math.Round(midt1 * 100 / bpgameloop, 2).ToString("00.00") + "%";
                Mid[1] = Math.Round(midt2 * 100 / bpgameloop, 2).ToString("00.00") + "%";
            }
        }

        public Dictionary<string, int> GetUnits(DbBreakpoint bp)
        {

            Dictionary<string, int> units = new Dictionary<string, int>();
            if (bp == null || String.IsNullOrEmpty(bp.dsUnitsString))
                return units;
            foreach (string unitstring in bp.dsUnitsString.Split("|"))
            {
                var ent = unitstring.Split(",");
                if (!units.ContainsKey(ent[0]))
                    units[ent[0]] = int.Parse(ent[1]);
                else
                    units[ent[0]] += int.Parse(ent[1]);
            }
            foreach (var ent in units.Keys.ToArray())
            {
                int id = 0;
                if (int.TryParse(ent, out id))
                {
                    UnitModelBase bunit = DSdata.Units.SingleOrDefault(s => s.ID == id);
                    if (bunit != null)
                    {
                        units.Add(bunit.Name, units[ent]);
                        units.Remove(ent);
                    }
                }
            }
            return units.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, p => p.Value);
        }

        public List<UnitModel> GetDbUnits(DSPlayer pl, DbBreakpoint bp, int objective)
        {
            List<UnitModel> Units = new List<UnitModel>();
            if (bp == null || String.IsNullOrEmpty(bp.dbUnitsString))
                return Units;

            foreach (string unitstring in bp.dbUnitsString.Split("|"))
            {
                var ent = unitstring.Split(",");
                UnitModel unit = new UnitModel();
                unit.Name = ent[0];
                unit.Race = pl.RACE;
                unit.Pos = new Vector2(int.Parse(ent[1]), int.Parse(ent[2]));
                int id = 0;
                if (int.TryParse(ent[0], out id))
                {
                    UnitModelBase bunit = DSdata.Units.FirstOrDefault(f => f.ID == id);
                    if (bunit != null)
                        unit.Name = bunit.Name;
                }

                Vector2 vec = Fix.RotatePoint(unit.Pos, center, -45);
                float newx = 0;
                float newy = 0;

                // Rotated Nexus/CC
                // 1
                // 77.08831, 114.34315
                // 173.25484, 120

                // 2
                // 77.08831, 125.656685
                // 167.59798, 125.656685


                if (pl.REALPOS > 3)
                    newx = (vec.X - 62.23907f) / 2;
                else
                    newx = (vec.X - 176.79037f) / 2;

                newy = vec.Y - 107.97919f;

                if (objective == 2)
                {
                    newy -= 11.313535f / 2;
                    newx += 5.65686f / 2;
                }
                unit.RotatePos = new Vector2(newx, newy);

                Units.Add(unit);
            }

            return Units;
        }

        public List<string> GetUpgrades(DbBreakpoint bp)
        {
            List<string> upgrades = new List<string>();
            if (bp == null || String.IsNullOrEmpty(bp.dbUpgradesString))
                return upgrades;
            foreach (string upgradestring in bp.dbUpgradesString.Split("|"))
            {
                int id = 0;
                if (int.TryParse(upgradestring, out id))
                {
                    UnitModelBase upgrade = DSdata.Upgrades.FirstOrDefault(s => s.ID == id);
                    if (upgrade != null)
                        upgrades.Add(upgrade.Name);
                }
                else
                    upgrades.Add(upgradestring);
            }
            return upgrades;
        }
    }
}
