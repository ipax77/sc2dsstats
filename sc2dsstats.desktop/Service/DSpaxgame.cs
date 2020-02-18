using sc2dsstats.decode.Models;
using paxgamelib.Data;
using paxgamelib.Models;
using paxgamelib.Service;
using sc2dsstats.desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace sc2dsstats.desktop.Service
{
        public static class DSpaxgame
        {
            public static Vector2 center = new Vector2(128, 119);


            public static Dictionary<int, Dictionary<int, List<UnitAbility>>> GetAbilityUpgrades(dsreplay replay)
            {
                Dictionary<int, Dictionary<int, List<UnitAbility>>> Upgrades = new Dictionary<int, Dictionary<int, List<UnitAbility>>>();
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    Upgrades[pl.POS] = new Dictionary<int, List<UnitAbility>>();
                    foreach (var ent in pl.AbilityUpgrades)
                    {
                        int gameloop = ent.Key;
                        foreach (var upgrades in ent.Value)
                        {
                            UnitAbility a = AbilityPool.Map(upgrades);
                            if (a != null)
                            {
                                if (!Upgrades[pl.POS].ContainsKey(gameloop))
                                    Upgrades[pl.POS][gameloop] = new List<UnitAbility>();
                                Upgrades[pl.POS][gameloop].Add(a);
                            }
                        }
                    }
                }
                return Upgrades;
            }

            public static Dictionary<int, Dictionary<int, List<UnitUpgrade>>> GetUpgrades(dsreplay replay)
            {
                Dictionary<int, Dictionary<int, List<UnitUpgrade>>> Upgrades = new Dictionary<int, Dictionary<int, List<UnitUpgrade>>>();
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    Upgrades[pl.POS] = new Dictionary<int, List<UnitUpgrade>>();
                    foreach (var ent in pl.Upgrades)
                    {
                        int gameloop = ent.Key;
                        foreach (var upgrades in ent.Value)
                        {
                            UnitUpgrade u = UpgradePool.Map(upgrades);
                            if (u != null)
                            {
                                if (!Upgrades[pl.POS].ContainsKey(gameloop))
                                    Upgrades[pl.POS][gameloop] = new List<UnitUpgrade>();
                                Upgrades[pl.POS][gameloop].Add(u);
                            }
                        }
                    }
                }
                return Upgrades;
            }

            public static (Dictionary<int, List<Unit>>, Dictionary<int, HashSet<int>>) GetUnits(dsreplay replay, GameHistory game)
            {
                //var json = File.ReadAllText("/data/unitst1p3.json");
                //var bab = JsonSerializer.Deserialize<List<UnitEvent>>(json);

                List<Unit> Units = new List<Unit>();
                List<Vector2> vecs = new List<Vector2>();
                List<UnitEvent> UnitEvents = replay.UnitBorn;

                int maxdiff = 0;
                int temploop = 0;
                Dictionary<int, List<Unit>> spawns = new Dictionary<int, List<Unit>>();
                Dictionary<int, HashSet<int>> plspawns = new Dictionary<int, HashSet<int>>();
                foreach (var unit in UnitEvents)
                {
                    int diff = unit.Gameloop - temploop;

                    if (temploop == 0)
                        spawns.Add(unit.Gameloop, new List<Unit>());
                    else if (diff > 3)
                        spawns.Add(unit.Gameloop, new List<Unit>());

                    if (unit.Gameloop - temploop > maxdiff)
                        maxdiff = unit.Gameloop - temploop;

                    temploop = unit.Gameloop;

                    int pos = unit.PlayerId;
                    int realpos = replay.PLAYERS.SingleOrDefault(x => x.POS == pos).REALPOS;
                    /*
                    if (unit.PlayerId > 3)
                        pos = unit.PlayerId - 3;
                    else if (unit.PlayerId <= 3)
                        pos = unit.PlayerId + 3;
                    */
                    spawns.Last().Value.Add(UnitEventToUnit(unit, realpos, game));

                    if (!plspawns.ContainsKey(pos))
                        plspawns[pos] = new HashSet<int>();

                    plspawns[pos].Add(spawns.Last().Key);
                }

                return (spawns, plspawns);
            }

            public static Unit UnitEventToUnit(UnitEvent unit, int pos, GameHistory game)
            {
                Vector2 vec = MoveService.RotatePoint(new Vector2(unit.x, unit.y), center, -45);
                float newx = 0;

                // postition 1-3 => 4-6 and 4-6 => 1-3 ...
                if (pos > 3)
                    newx = ((vec.X - 62.946175f) / 2);
                else if (pos <= 3)
                    newx = ((vec.X - 177.49748f) / 2);

                float newy = vec.Y - 107.686295f;

                // Team2
                // Ymax: 131,72792 => Battlefield.YMax
                // Ymin: 107,686295 => 0

                // Xmax: 78,502525 => 10
                // Xmin: 62,946175 => 0

                // Fix Names
                if (unit.Name.EndsWith("Lightweight"))
                    unit.Name = unit.Name.Replace("Lightweight", "");

                if (unit.Name.EndsWith("Starlight"))
                    unit.Name = unit.Name.Replace("Starlight", "");

                Unit punit = null;
                Unit myunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
                if (myunit == null)
                {
                    myunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA").DeepCopy();
                    myunit.Name = unit.Name;
                }

                if (myunit != null)
                {
                    punit = myunit.DeepCopy();
                    punit.ID = game.GetUnitID();

                    newx = MathF.Round((MathF.Round(newx * 2, MidpointRounding.AwayFromZero) / 2), 1);
                    newy = MathF.Round((MathF.Round(newy * 2, MidpointRounding.AwayFromZero) / 2), 1);

                    punit.PlacePos = new Vector2(newy, newx);
                    punit.Owner = pos;
                }

                return punit;
            }

            public static int GetPlayer(GameMapModel _map, dsreplay replay, Player pl, int gameloop = 0)
            {
                dsplayer dspl = replay.PLAYERS.SingleOrDefault(x => x.REALPOS == pl.Pos);
                if (dspl == null)
                    return gameloop;

                pl.SoftReset();
                if (pl.Name == "")
                {
                    pl.Name = dspl.NAME;
                    pl.Pos = dspl.REALPOS;
                    UnitRace race = UnitRace.Terran;
                    if (dspl.RACE == "Protoss")
                        race = UnitRace.Protoss;
                    else if (dspl.RACE == "Zerg")
                        race = UnitRace.Zerg;
                    pl.Race = race;
                    pl.Units = UnitPool.Units.Where(x => x.Race == race && x.Cost > 0).ToList();
                }

                if (gameloop == 0)
                    gameloop = _map.plSpawns[pl.Pos].OrderBy(o => o).First();

                foreach (var unit in _map.Spawns[gameloop].Where(x => x.Owner == pl.Pos))
                {
                    Unit myunit = unit.DeepCopy();
                    if (pl.Pos <= 3)
                    {
                        //myunit.PlacePos = UnitService.mirrorImage(myunit.PlacePos);
                        //myunit.PlacePos = new Vector2(Battlefield.Xmax - myunit.PlacePos.X, myunit.PlacePos.Y);
                        myunit.PlacePos = new Vector2(myunit.PlacePos.X, 2 * 5 - myunit.PlacePos.Y - 1);
                    }
                    UnitService.NewUnit(pl, myunit);
                    pl.Units.Add(myunit);
                    pl.MineralsCurrent -= myunit.Cost;
                }

                foreach (var dic in _map.Upgrades[pl.Pos].OrderBy(x => x.Key))
                {
                    if (dic.Key > gameloop)
                        break;
                    foreach (var upgrade in dic.Value)
                        pl.MineralsCurrent -= UnitService.UpgradeUnit(upgrade.Upgrade, pl);
                }
                foreach (var dic in _map.AbilityUpgrades[pl.Pos].OrderBy(x => x.Key))
                {
                    if (dic.Key > gameloop)
                        break;
                    foreach (var upgrade in dic.Value)
                        pl.MineralsCurrent -= UnitService.AbilityUpgradeUnit(upgrade, pl);
                }
                pl.MineralsCurrent = 10000;
                return gameloop;
            }
        }
    }

