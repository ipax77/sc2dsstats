using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using sc2dsstats.decode.Models;

namespace sc2dsstats.decode
{
    static class DSparseNG
    {

        static Regex rx_race2 = new Regex(@"Worker(.*)", RegexOptions.Singleline);
        static Regex rx_unit = new Regex(@"([^']+)Place([^']+)?", RegexOptions.Singleline);
        static Regex rx_subname = new Regex(@"<sp\/>(.*)$", RegexOptions.Singleline);

        public static int MIN5 = 6720;
        public static int MIN10 = 13440;
        public static int MIN15 = 20160;
        public static List<KeyValuePair<string, int>> BREAKPOINTS { get; } = new List<KeyValuePair<string, int>>()
        {
            new KeyValuePair<string, int>("MIN5", MIN5),
            new KeyValuePair<string, int>("MIN10", MIN10),
            new KeyValuePair<string, int>("MIN15", MIN15),
            new KeyValuePair<string, int>("ALL", 0)
        };

        private static REParea AREA { get; set; } = new REParea();

        public static HashSet<string> AbilityUpgrades = new HashSet<string>()
        {
            "zerglingmovementspeed",
            "zerglingattackspeed",
            "TunnelingClaws",
            "Stimpack",
            "Tier2",
            "Tier3",
            "Stimpack",
            "ShieldWall",
            "RavenCorvidReactor",
            "PunisherGrenades",
            "PsiStormTech",
            "PhoenixRangeUpgrade",
            "PersonalCloaking",
            "ObserverGraviticBooster",
            "NeuralParasite",
            "MedivacIncreaseSpeedBoost",
            "MedivacCaduceusReactor",
            "LiberatorAGRangeUpgrade",
            "InfestorEnergyUpgrade",
            "HiSecAutoTracking",
            "HighCapacityMode",
            "HighCapacityBarrels",
            "GlialReconstitution",
            "ExtendedThermalLance",
            "EvolveMuscularAugments",
            "EvolveGroovedSpines",
            "DrillClaws",
            "DiggingClaws",
            "DarkTemplarBlinkUpgrade",
            "CycloneLockOnDamageUpgrade",
            "ChitinousPlating",
            "Charge",
            "CentrificalHooks",
            "BlinkTech",
            "BansheeSpeed",
            "BansheeCloak",
            "AnabolicSynthesis",
            "AdeptPiercingAttack"
        };


        public static dsreplay GetDetails(string replay_file, dynamic details_dec)
        {
            dsreplay replay = new dsreplay();
            replay.REPLAY = replay_file;
            int failsafe_pos = 0;
            foreach (var player in details_dec["m_playerList"])
            {
                if (player["m_observe"] > 0) continue;

                failsafe_pos++;
                string name = "";
                Bytes bab = null;
                try
                {
                    bab = player["m_name"];
                }
                catch { }

                if (bab != null) name = Encoding.UTF8.GetString(bab.ToByteArray());
                else name = player["m_name"].ToString();

                Match m2 = rx_subname.Match(name);
                if (m2.Success) name = m2.Groups[1].Value;
                dsplayer pl = new dsplayer();

                pl.NAME = name;
                pl.RACE = player["m_race"].ToString();
                pl.RESULT = int.Parse(player["m_result"].ToString());
                pl.TEAM = int.Parse(player["m_teamId"].ToString());
                pl.POS = failsafe_pos;
                if (player["m_workingSetSlotId"] != null)
                    pl.WORKINGSLOT = int.Parse(player["m_workingSetSlotId"].ToString());

                replay.PLAYERS.Add(pl);
            }

            replay.PLAYERCOUNT = replay.PLAYERS.Count();

            //long offset = (long)details_dec["m_timeLocalOffset"];
            long timeutc = (long)details_dec["m_timeUTC"];
            long georgian = timeutc;
            DateTime gametime = DateTime.FromFileTime(georgian);
            replay.GAMETIME = double.Parse(gametime.ToString("yyyyMMddHHmmss"));

            return replay;
        }

        public static dsreplay GetTrackerevents(dynamic trackerevents_dec, dsreplay replay, bool GetDetail = false)
        {
            bool isBrawl_set = false;
            HashSet<string> Mutation = new HashSet<string>();
            replay.MIDDLE.Add(new KeyValuePair<int, int>(0, 0));

            bool noStagingAreaNextSpawn = false;
            if (replay.GAMETIME < 20190324214615)
                noStagingAreaNextSpawn = true;

            foreach (PythonDictionary pydic in trackerevents_dec)
            {
                // Units
                if (pydic.ContainsKey("m_unitTypeName"))
                {
                    if (pydic.ContainsKey("m_controlPlayerId"))
                    {
                        int playerid = (int)pydic["m_controlPlayerId"];
                        int gameloop = (int)pydic["_gameloop"];

                        // Game end
                        if (pydic["m_unitTypeName"].ToString().StartsWith("DeathBurst"))
                        {
                            replay.DURATION = gameloop;

                            if (playerid == 13)
                                replay.WINNER = 1;
                            else if (playerid == 14)
                                replay.WINNER = 0;

                            break;
                        }

                        // Player
                        if (playerid == 0 || playerid > 12) continue;
                        dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                        if (pl == null)
                        {
                            pl = replay.PLAYERS.Where(x => x.WORKINGSLOT == playerid - 1).FirstOrDefault();
                            if (pl == null)
                                continue;
                            else
                                pl.POS = playerid;
                        };

                        // Race
                        Match m = rx_race2.Match(pydic["m_unitTypeName"].ToString());
                        if (m.Success && m.Groups[1].Value.Length > 0)
                        {
                            pl.RACE = m.Groups[1].Value;
                        }
                        else if (pydic.ContainsKey("m_creatorAbilityName"))
                        {
                            if (pydic["m_creatorAbilityName"] == null || pydic["m_creatorAbilityName"].ToString() == "")
                            {
                                // Refineries init
                                if (gameloop == 0 && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitBornEvent" && pydic["m_unitTypeName"].ToString().StartsWith("MineralField"))
                                {
                                    int index = (int)pydic["m_unitTagIndex"];
                                    int recycle = (int)pydic["m_unitTagRecycle"];

                                    Refinery refinery = new Refinery();
                                    refinery.Index = index;
                                    refinery.RecycleTag = recycle;
                                    refinery.PlayerId = playerid;

                                    replay.Refineries.Add(refinery);
                                }


                                if (gameloop < 480) continue;

                                if (noStagingAreaNextSpawn == false)
                                    if (gameloop - replay.LastSpawn >= 9) continue;

                                if (pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitBornEvent")
                                {
                                    string born_unit = pydic["m_unitTypeName"].ToString();

                                    if (born_unit == "TrophyRiftPremium") continue;
                                    if (born_unit == "MineralIncome") continue;
                                    if (born_unit == "ParasiticBombRelayDummy") continue;
                                    if (born_unit == "Biomass") continue;
                                    if (born_unit == "PurifierAdeptShade") continue;
                                    if (born_unit == "PurifierTalisShade") continue;
                                    if (born_unit == "HornerReaperLD9ClusterCharges") continue;
                                    if (born_unit == "Broodling") continue;
                                    if (born_unit == "Raptorling") continue;
                                    if (born_unit == "InfestedLiberatorViralSwarm") continue;
                                    if (born_unit == "SplitterlingSpawn") continue;
                                    if (born_unit == "GuardianShell") continue;
                                    if (born_unit == "BroodlingStetmann") continue;

                                    if (noStagingAreaNextSpawn == true)
                                    {
                                        if (gameloop - replay.LastSpawn >= 460)
                                            replay.LastSpawn = gameloop;
                                    }

                                    if (!replay.Spawns.ContainsKey(playerid))
                                        replay.Spawns.Add(playerid, new List<int>());

                                    if (!replay.Spawns[playerid].Contains(replay.LastSpawn))
                                    {
                                        replay.Spawns[playerid].Add(replay.LastSpawn);
                                        pl.Spawned = 1;
                                        pl.LastSpawn = replay.LastSpawn;
                                    }

                                    if (GetDetail == true)
                                    {
                                        UnitEvent _unit = new UnitEvent();
                                        _unit.Gameloop = gameloop;
                                        _unit.Name = born_unit;
                                        _unit.Index = (int)pydic["m_unitTagIndex"];
                                        _unit.RecycleTag = (int)pydic["m_unitTagRecycle"];
                                        _unit.PlayerId = playerid;
                                        _unit.x = (int)pydic["m_x"];
                                        _unit.y = (int)pydic["m_y"];
                                        replay.UnitBorn.Add(_unit);

                                        if (!replay.UnitLife.ContainsKey(_unit.Index))
                                            replay.UnitLife.Add(_unit.Index, new Dictionary<int, UnitLife>());
                                        if (!replay.UnitLife[_unit.Index].ContainsKey(_unit.RecycleTag))
                                            replay.UnitLife[_unit.Index].Add(_unit.RecycleTag, new UnitLife());

                                        replay.UnitLife[_unit.Index][_unit.RecycleTag].Born = _unit;
                                    }

                                    if (!pl.SPAWNS.ContainsKey(pl.LastSpawn)) pl.SPAWNS.Add(pl.LastSpawn, new Dictionary<string, int>());
                                    if (!pl.SPAWNS[pl.LastSpawn].ContainsKey(born_unit)) pl.SPAWNS[pl.LastSpawn].Add(born_unit, 1);
                                    else pl.SPAWNS[pl.LastSpawn][born_unit]++;

                                    // Ingame position
                                    if (pl.REALPOS == 0)
                                    {
                                        int pos = 0;

                                        if (replay.PLAYERCOUNT == 2)
                                            pos = 1;
                                        else if ((gameloop - 480) % 1440 == 0)
                                            pos = 1;
                                        else if ((gameloop - 481) % 1440 == 0)
                                            pos = 1;
                                        else if ((gameloop - 960) % 1440 == 0)
                                            pos = 2;
                                        else if ((gameloop - 961) % 1440 == 0)
                                            pos = 2;
                                        else if ((gameloop - 1440) % 1440 == 0)
                                            pos = 3;
                                        else if ((gameloop - 1441) % 1440 == 0)
                                            pos = 3;

                                        if (replay.PLAYERCOUNT == 4 && pos == 3) pos = 1;

                                        if (pos > 0)
                                        {
                                            int team = REParea.GetTeam((int)pydic["m_x"], (int)pydic["m_y"]);
                                            if (team == 1) pl.REALPOS = pos;
                                            else if (team == 2) pl.REALPOS = pos + 3;
                                            pl.TEAM = team - 1;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Tier upgrade
                                if (pl.STATS.Count() > 0)
                                {
                                    if (pydic["m_creatorAbilityName"].ToString() == "NeutralUpgradesAutocast")
                                        if (pydic["m_unitTypeName"].ToString() == "Tier2Dummy")
                                            pl.STATS.Last().Value.Tier = 2;
                                        else if (pydic["m_unitTypeName"].ToString() == "Tier3Dummy")
                                            pl.STATS.Last().Value.Tier = 3;
                                }
                            }
                        }

                    }
                    else if (pydic.ContainsKey("_event") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitTypeChangeEvent")
                    {
                        // Refinery taken
                        if (pydic["m_unitTypeName"].ToString().StartsWith("RefineryMinerals") || pydic["m_unitTypeName"].ToString().StartsWith("AssimilatorMinerals") || pydic["m_unitTypeName"].ToString().StartsWith("ExtractorMinerals"))
                        {
                            var refinery = replay.Refineries.Where(x => x.Index == (int)pydic["m_unitTagIndex"] && x.RecycleTag == (int)pydic["m_unitTagRecycle"]).FirstOrDefault();
                            if (refinery != null)
                            {
                                refinery.Taken = true;
                                refinery.Gameloop = (int)pydic["_gameloop"];
                            }
                        }
                    }
                }
                else if (pydic.ContainsKey("m_unitTagIndex") && (int)pydic["m_unitTagIndex"] == 20 && pydic.ContainsKey("_event") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitOwnerChangeEvent")
                {
                    // Middle
                    int gameloop = (int)pydic["_gameloop"];
                    int upkeepid = (int)pydic["m_upkeepPlayerId"];

                    KeyValuePair<int, int> Mid = new KeyValuePair<int, int>(0, 0);
                    if (upkeepid == 13)
                        Mid = new KeyValuePair<int, int>(gameloop, 1);
                    else if (upkeepid == 14)
                        Mid = new KeyValuePair<int, int>(gameloop, 2);

                    if (Mid.Key > 0)
                        replay.MIDDLE.Add(Mid);
                }
                else if (GetDetail == true && pydic.ContainsKey("m_killerPlayerId") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitDiedEvent")
                {
                    // Unit died
                    if ((int)pydic["_gameloop"] > 480)
                    {
                        UnitEvent _unit = new UnitEvent();
                        _unit.Gameloop = (int)pydic["_gameloop"];
                        if (pydic["m_killerPlayerId"] != null)
                            _unit.KilledId = (int)pydic["m_killerPlayerId"];
                        if (pydic["m_killerUnitTagIndex"] != null)
                            _unit.KilledBy = (int)pydic["m_killerUnitTagIndex"];
                        _unit.Index = (int)pydic["m_unitTagIndex"];
                        if (pydic["m_unitTagRecycle"] != null)
                            _unit.RecycleTag = (int)pydic["m_unitTagRecycle"];
                        if (pydic["m_killerUnitTagRecycle"] != null)
                            _unit.KillerRecycleTag = (int)pydic["m_killerUnitTagRecycle"];
                        _unit.x = (int)pydic["m_x"];
                        _unit.y = (int)pydic["m_y"];

                        if (!replay.UnitLife.ContainsKey(_unit.Index))
                            replay.UnitLife.Add(_unit.Index, new Dictionary<int, UnitLife>());
                        if (!replay.UnitLife[_unit.Index].ContainsKey(_unit.RecycleTag))
                            replay.UnitLife[_unit.Index].Add(_unit.RecycleTag, new UnitLife());

                        replay.UnitLife[_unit.Index][_unit.RecycleTag].Died = _unit;

                        UnitEvent bornunit = replay.UnitBorn.SingleOrDefault(x => x.Index == _unit.Index && x.RecycleTag == _unit.RecycleTag);
                        if (bornunit != null)
                        {
                            bornunit.KilledBy = _unit.KilledBy;
                            bornunit.KilledId = _unit.KilledId;
                            bornunit.KillerRecycleTag = _unit.KillerRecycleTag;
                            bornunit.GameloopDied = _unit.Gameloop;
                            bornunit.x_died = _unit.x;
                            bornunit.y_died = _unit.y;
                        }

                    }
                }
                else if (pydic.ContainsKey("m_stats"))
                {
                    // Stats
                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    if (playerid == 0 || playerid > 6) continue;
                    dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                    if (pl == null) continue;

                    if (isBrawl_set == false)
                        isBrawl_set = true;

                    PythonDictionary pystats = pydic["m_stats"] as PythonDictionary;
                    M_stats m_stats = new M_stats();

                    m_stats.FoodUsed = (int)pystats["m_scoreValueFoodUsed"];
                    m_stats.MineralsCollectionRate = (int)pystats["m_scoreValueMineralsCollectionRate"];
                    m_stats.MineralsCurrent = (int)pystats["m_scoreValueMineralsCurrent"];
                    m_stats.MineralsFriendlyFireArmy = (int)pystats["m_scoreValueMineralsFriendlyFireArmy"];
                    m_stats.MineralsFriendlyFireTechnology = (int)pystats["m_scoreValueMineralsFriendlyFireTechnology"];
                    m_stats.MineralsKilledArmy = (int)pystats["m_scoreValueMineralsKilledArmy"];
                    m_stats.MineralsKilledTechnology = (int)pystats["m_scoreValueMineralsKilledTechnology"];
                    m_stats.MineralsLostArmy = (int)pystats["m_scoreValueMineralsLostArmy"];
                    m_stats.MineralsUsedActiveForces = (int)pystats["m_scoreValueMineralsUsedActiveForces"];
                    m_stats.MineralsUsedCurrentArmy = (int)pystats["m_scoreValueMineralsUsedCurrentArmy"];
                    m_stats.MineralsUsedCurrentTechnology = (int)pystats["m_scoreValueMineralsUsedCurrentTechnology"];

                    pl.STATS[gameloop] = m_stats;

                    replay.DURATION = gameloop;
                    pl.PDURATION = gameloop;

                    if (pl.Spawned == 2)
                    {
                        pl.ARMY = pl.ARMY + (pl.STATS.Last().Value.MineralsUsedActiveForces / 2);
                        pl.Spawned = 0;

                        SpawnInfo(replay, pl);
                    }

                    if (pl.Spawned == 1)
                        pl.Spawned = 2;

                    pl.STATS[gameloop].Army = pl.ARMY;

                    int income = pl.STATS[gameloop].MineralsCollectionRate;
                    pl.INCOME += (double)income / 9.15;

                    pl.GAS = replay.Refineries.Where(x => x.PlayerId == pl.POS && x.Taken == true).Count();
                    pl.STATS[gameloop].Gas = pl.GAS * 1000;

                    if (!pl.SPAWNS.ContainsKey(pl.LastSpawn))
                        pl.SPAWNS.Add(pl.LastSpawn, new Dictionary<string, int>());

                    pl.SPAWNS[pl.LastSpawn]["Gas"] = pl.GAS;
                    pl.SPAWNS[pl.LastSpawn]["Upgrades"] = pl.STATS[gameloop].MineralsUsedCurrentTechnology;

                }
                else if (isBrawl_set == false && pydic.ContainsKey("_gameloop") && (int)pydic["_gameloop"] == 0 && pydic.ContainsKey("m_upgradeTypeName"))
                {
                    // Gamemodes
                    if (pydic["m_upgradeTypeName"].ToString().StartsWith("Mutation"))
                        Mutation.Add(pydic["m_upgradeTypeName"].ToString());
                }
                else if (pydic.ContainsKey("m_upgradeTypeName") && pydic["m_upgradeTypeName"].ToString() == "StagingAreaNextSpawn")
                {
                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    if (playerid == 0 || playerid > 6) continue;
                    dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                    if (pl == null) continue;

                    int m_count = (int)pydic["m_count"];
                    replay.LastSpawn = gameloop;

                    // Spawn
                    if (m_count == -1 || replay.PLAYERCOUNT == 2)
                    {
                        if (!replay.Spawns.ContainsKey(playerid))
                            replay.Spawns.Add(playerid, new List<int>());

                        replay.Spawns[playerid].Add(gameloop);
                        pl.LastSpawn = gameloop;
                        pl.Spawned = 1;
                    }
                } else if (GetDetail == true && pydic.ContainsKey("_event") && pydic.ContainsKey("m_upgradeTypeName") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUpgradeEvent")
                {
                    string upgrade = pydic["m_upgradeTypeName"].ToString();

                    if (upgrade.StartsWith("Mineral"))
                        continue;
                    if (upgrade.StartsWith("AFK"))
                        continue;
                    if (upgrade.StartsWith("Decoration"))
                        continue;
                    if (upgrade.StartsWith("Mastery"))
                        continue;
                    if (upgrade.EndsWith("Disable"))
                        continue;
                    if (upgrade.StartsWith("Emote"))
                        continue;
                    if (upgrade == "HighCapacityMode")
                        continue;
                    if (upgrade == "PlayerIsAFK")
                        continue;

                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    if (playerid == 0 || playerid > 6) continue;
                    dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                    if (pl == null || gameloop == 0) continue;

                    if (upgrade.StartsWith(pl.RACE))
                    {
                        //if (upgrade.EndsWith("Multi"))
                        if (upgrade.EndsWith("Starlight"))
                        {
                            if (!pl.Upgrades.ContainsKey(gameloop))
                                pl.Upgrades[gameloop] = new List<string>();
                            pl.Upgrades[gameloop].Add(upgrade);
                        }
                    } else if (AbilityUpgrades.Contains(upgrade))
                    {
                        if (!pl.AbilityUpgrades.ContainsKey(gameloop))
                            pl.AbilityUpgrades[gameloop] = new List<string>();
                        pl.AbilityUpgrades[gameloop].Add(upgrade);
                    }

                }
            }

            // Gamemode
            if (Mutation.Contains("MutationCovenant"))
                replay.GAMEMODE = "GameModeSwitch";
            else if (Mutation.Contains("MutationEquipment"))
                replay.GAMEMODE = "GameModeGear";
            else if (Mutation.Contains("MutationExile")
                    && Mutation.Contains("MutationRescue")
                    && Mutation.Contains("MutationShroud")
                    && Mutation.Contains("MutationSuperscan"))
                replay.GAMEMODE = "GameModeSabotage";
            else if (Mutation.Contains("MutationCommanders"))
            {
                replay.GAMEMODE = "GameModeCommanders"; // fail safe
                if (Mutation.Count() == 3 && Mutation.Contains("MutationExpansion") && Mutation.Contains("MutationOvertime")) replay.GAMEMODE = "GameModeCommandersHeroic";
                else if (Mutation.Count() == 2 && Mutation.Contains("MutationOvertime")) replay.GAMEMODE = "GameModeCommanders";
                else if (Mutation.Count() >= 3) replay.GAMEMODE = "GameModeBrawlCommanders";
            }
            else
            {
                if (replay.GAMEMODE == "unknown" && Mutation.Count() == 0) replay.GAMEMODE = "GameModeStandard";
                else if (replay.GAMEMODE == "unknown" && Mutation.Count() > 0) replay.GAMEMODE = "GameModeBrawlStandard";
            }

            replay.ISBRAWL = true;
            if (replay.GAMEMODE == "GameModeCommanders" || replay.GAMEMODE == "GameModeCommandersHeroic" || replay.GAMEMODE == "GameModeStandard")
                replay.ISBRAWL = false;

            // fail safe
            FixPos(replay);
            FixWinner(replay);
            //replay.MIDDLE.Add(new KeyValuePair<int, int>(replay.DURATION, replay.WINNER + 1));

            // Breakpoints 5min, 10min, 15min, all
            Dictionary<string, int> Bps = new Dictionary<string, int>()
            {
                { "MIN15", 20640 },
                { "MIN10", 13440 },
                { "MIN5", 6240 }
            };
            foreach (dsplayer pl in replay.PLAYERS)
            {

                if (pl.SPAWNS.Count() == 0) continue;

                pl.UNITS["ALL"] = pl.SPAWNS.Last().Value;
                pl.UNITS["ALL"]["Mid"] = GetMiddle(replay.DURATION, pl.TEAM, replay.MIDDLE);
                pl.UNITS["ALL"]["Gas"] = pl.GAS;
                pl.UNITS["ALL"]["Upgrades"] = pl.STATS.Last().Value.MineralsUsedCurrentTechnology;

                if (pl.SPAWNS.Count() > 2)
                    if (pl.SPAWNS.Last().Value.Count() < pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 2).Value.Count())
                    {
                        pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 2).Value["Mid"] = pl.UNITS["ALL"]["Mid"];
                        pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 2).Value["Gas"] = pl.UNITS["ALL"]["Gas"];
                        pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 2).Value["Upgrades"] = pl.UNITS["ALL"]["Upgrades"];
                        pl.UNITS["ALL"] = pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 2).Value;
                    }
                
                foreach (string bp in Bps.Keys)
                {
                    Dictionary<string, int> units = new Dictionary<string, int>();
                    int gameloop = 0;
                    var ent = pl.SPAWNS.Where(x => x.Key >= Bps[bp]).FirstOrDefault();
                    if (ent.Value != null)
                    {
                        units = ent.Value;
                        gameloop = ent.Key;
                        units["Mid"] = GetMiddle(gameloop, pl.TEAM, replay.MIDDLE);
                    }
                    else
                    {
                        units = pl.UNITS["ALL"];
                        gameloop = replay.DURATION;
                        units["Mid"] = pl.UNITS["ALL"]["Mid"];
                    }
                    pl.UNITS[bp] = units;
                }


                if (pl.STATS.Count() > 0)
                    pl.KILLSUM = pl.STATS.Last().Value.MineralsKilledArmy;

                pl.INCOME = Math.Round(pl.INCOME, 2);
            }


            return replay;
        }

        public static int GetMiddle(int gameloop, int team, List<KeyValuePair<int, int>> middle)
        {
            KeyValuePair<int, int> lastent = new KeyValuePair<int, int>(0, 0);
            int mid = 0;
            bool hasInfo = false;
            foreach (var ent in middle)
            {
                if (ent.Key > gameloop)
                {
                    hasInfo = true;
                    if (lastent.Value == team + 1)
                        mid += gameloop - lastent.Key;
                    break;
                }

                if (lastent.Key > 0 && lastent.Value == team + 1)
                    mid += ent.Key - lastent.Key;

                lastent = ent;
            }
            if (middle.Count() > 0)
                if (hasInfo == false && middle.Last().Value == team + 1)
                    mid += gameloop - middle.Last().Key;

            if (mid < 0) mid = 0;
            return mid;
        }

        public static void FixPos(dsreplay replay)
        {
            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.REALPOS == 0)
                {
                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;

                        List<dsplayer> temp = new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == j).ToList());
                        if (temp.Count == 0)
                        {
                            pl.REALPOS = j;
                        }
                    }
                    if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == pl.POS).ToList()).Count == 0) pl.REALPOS = pl.POS;
                }

                if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == pl.REALPOS).ToList()).Count > 1)
                {
                    Console.WriteLine("Found double playerid for " + pl.POS + "|" + pl.REALPOS);

                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;
                        if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == j).ToList()).Count == 0)
                        {
                            pl.REALPOS = j;
                            break;
                        }
                    }

                }

            }
        }

        public static void FixWinner(dsreplay replay)
        {
            if (replay.WINNER < 0)
            {
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    if (pl.RESULT == 1)
                    {
                        replay.WINNER = pl.TEAM;
                        break;
                    }
                }
            }

            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.TEAM == replay.WINNER) pl.RESULT = 1;
                else pl.RESULT = 2;
            }
        }

        public static void SpawnInfo(dsreplay rep, dsplayer pl)
        {

            if (pl.STATS.Count() > 1)
            {
                dsplayer opp = rep.GetOpp(pl.REALPOS);
                if (opp != null && opp.STATS.Count() > 1 && opp.STATS.ContainsKey(pl.STATS.Last().Key))
                {

                    int k1 = pl.STATS.Last().Value.MineralsKilledArmy;
                    int k2 = opp.STATS.Last().Value.MineralsKilledArmy;

                    pl.LastSpawnKills = k1 - pl.LastSpawnKills;
                    opp.LastSpawnKills = k2 - opp.LastSpawnKills;

                    int m1 = pl.STATS.Last().Value.MineralsUsedActiveForces / 2;
                    int m2 = opp.STATS.Last().Value.MineralsUsedActiveForces / 2;

                    pl.LastSpawnArmy = m1 - pl.LastSpawnArmy;
                    opp.LastSpawnArmy = m2 - opp.LastSpawnArmy;

                    pl.STATS.Last().Value.ArmyDiff = pl.LastSpawnArmy - opp.LastSpawnArmy;
                    opp.STATS.Last().Value.ArmyDiff = opp.LastSpawnArmy - pl.LastSpawnArmy;
                    pl.STATS.Last().Value.KillsDiff = pl.LastSpawnKills - opp.LastSpawnKills;
                    opp.STATS.Last().Value.KillsDiff = opp.LastSpawnKills - pl.LastSpawnKills;
                }
            }
        }

        public class REPvec
        {
            public int x { get; set; }
            public int y { get; set; }

            public REPvec(int X, int Y)
            {
                x = X;
                y = Y;
            }
        }

        private class REParea
        {
            public static Dictionary<int, Dictionary<string, REPvec>> POS { get; set; } = new Dictionary<int, Dictionary<string, REPvec>>()
            {
                // spawn area pl 1,2,3
                { 1, new Dictionary<string, REPvec>() {
                    { "A", new REPvec(107, 162) },
                    { "B", new REPvec(160, 106) },
                    { "C", new REPvec(218, 160) },
                    { "D", new REPvec(162, 216) }
                }
                },
                // spawn area pl 4,5,6
                { 2, new Dictionary<string, REPvec>()
                {
                    { "A", new REPvec(35, 88) },
                    { "B", new REPvec(92, 30) },
                    { "C", new REPvec(142, 99) },
                    { "D", new REPvec(100, 144) }
                }
                }
            };

            public static int GetTeam(int x, int y)
            {
                int team = 0;
                bool indahouse = false;
                foreach (int plpos in POS.Keys)
                {
                    indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["B"].x, POS[plpos]["B"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);
                    if (indahouse == false) indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["D"].x, POS[plpos]["D"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);

                    if (indahouse == true)
                    {
                        team = plpos;
                        break;
                    }
                }
                return team;
            }


            private static bool PointInTriangle(int Px, int Py, int Ax, int Ay, int Bx, int By, int Cx, int Cy)
            {
                bool indahouse = false;
                int b1 = 0;
                int b2 = 0;
                int b3 = 0;

                if (sign(Px, Py, Ax, Ay, Bx, By) < 0) b1 = 1;
                if (sign(Px, Py, Bx, By, Cx, Cy) < 0) b2 = 1;
                if (sign(Px, Py, Cx, Cy, Ax, Ay) < 0) b3 = 1;

                if ((b1 == b2) && (b2 == b3)) indahouse = true;
                return indahouse;
            }

            private static int sign(int Ax, int Ay, int Bx, int By, int Cx, int Cy)
            {
                int sig = (Ax - Cx) * (By - Cy) - (Bx - Cx) * (Ay - Cy);
                return sig;
            }

        }
    }
}

