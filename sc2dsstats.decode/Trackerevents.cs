using IronPython.Runtime;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using sc2dsstats.lib.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace sc2dsstats.decode
{
    public class Trackerevents
    {
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

        public static Regex rx_race2 = new Regex(@"Worker(.*)", RegexOptions.Singleline);
        public static Regex rx_tier = new Regex(@"^Tier(\d+)Dummy$", RegexOptions.Singleline);

        public static DSReplay Get(dynamic trackerevents_dec, DSReplay replay)
        {
            bool isBrawl_set = false;
            //bool noStagingAreaNextSpawn = true;
            bool noStagingAreaNextSpawn = false;
            if (replay.GAMETIME < new DateTime(2019, 03, 24, 21, 46, 15)) // 20190324214615
                noStagingAreaNextSpawn = true;

            HashSet<string> Mutation = new HashSet<string>();

            List<DbMiddle> Middle = new List<DbMiddle>();
            Middle.Add(new DbMiddle(0, 0, replay));

            replay.Middle = Middle;

            List<StagingAreaNextSpawn> stagingAreaNextSpawns = new List<StagingAreaNextSpawn>();

            Vector2 ObjectivePlanetaryFortress = Vector2.Zero;
            Vector2 ObjectiveNexus = Vector2.Zero;
            Vector2 ObjectiveBunker = Vector2.Zero;
            Vector2 ObjectivePhotonCannon = Vector2.Zero;
            Vector2 Center = Vector2.Zero;

            KeyValuePair<Vector2, Vector2> LineT1 = new KeyValuePair<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
            KeyValuePair<Vector2, Vector2> LineT2 = new KeyValuePair<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
            KeyValuePair<int, int> PhotonCannon = new KeyValuePair<int, int>();
            KeyValuePair<int, int> Bunker = new KeyValuePair<int, int>();

            int UnitID = 0;
            int LastSpawn = 480;
            int Winner = 0;

            foreach (PythonDictionary pydic in trackerevents_dec)
            {
                if (pydic.ContainsKey("m_unitTypeName")) //11998
                {
                    if (pydic.ContainsKey("m_controlPlayerId"))
                    {
                        int playerid = (int)pydic["m_controlPlayerId"];
                        int gameloop = (int)pydic["_gameloop"];

                        // Game end
                        if (pydic["m_unitTypeName"].ToString().StartsWith("DeathBurst"))
                        {
                            replay.DURATION = (int)(gameloop / 22.4);

                            if (playerid == 13)
                                replay.WINNER = 1;
                            else if (playerid == 14)
                                replay.WINNER = 0;

                            break;
                        }

                        // Objectives init
                        if (gameloop == 0 && pydic.ContainsKey("m_creatorAbilityName") && (pydic["m_creatorAbilityName"] == null || pydic["m_creatorAbilityName"].ToString() == ""))
                        {
                            if (pydic["m_unitTypeName"].ToString() == "ObjectivePlanetaryFortress")
                                ObjectivePlanetaryFortress = new Vector2((int)pydic["m_x"], (int)pydic["m_y"]);
                            else if (pydic["m_unitTypeName"].ToString() == "ObjectiveNexus")
                                ObjectiveNexus = new Vector2((int)pydic["m_x"], (int)pydic["m_y"]);
                            else if (pydic["m_unitTypeName"].ToString() == "ObjectiveBunker")
                            {
                                ObjectiveBunker = new Vector2((int)pydic["m_x"], (int)pydic["m_y"]);
                                Bunker = new KeyValuePair<int, int>((int)pydic["m_unitTagIndex"], (int)pydic["m_unitTagRecycle"]);
                            }
                            else if (pydic["m_unitTypeName"].ToString() == "ObjectivePhotonCannon")
                            {
                                ObjectivePhotonCannon = new Vector2((int)pydic["m_x"], (int)pydic["m_y"]);
                                PhotonCannon = new KeyValuePair<int, int>((int)pydic["m_unitTagIndex"], (int)pydic["m_unitTagRecycle"]);
                            }

                            if (ObjectiveBunker != Vector2.Zero
                                && ObjectivePhotonCannon != Vector2.Zero
                                && ObjectivePlanetaryFortress != Vector2.Zero
                                && ObjectiveNexus != Vector2.Zero)
                            {
                                float x1t1 = ObjectivePlanetaryFortress.X + MathF.Cos(135 * MathF.PI / 180) * 100;
                                float y1t1 = ObjectivePlanetaryFortress.Y + MathF.Sin(135 * MathF.PI / 180) * 100;
                                float x2t1 = ObjectivePlanetaryFortress.X + MathF.Cos(315 * MathF.PI / 180) * 100;
                                float y2t1 = ObjectivePlanetaryFortress.Y + MathF.Sin(315 * MathF.PI / 180) * 100;

                                LineT1 = new KeyValuePair<Vector2, Vector2>(new Vector2(x1t1, y1t1), new Vector2(x2t1, y2t1));

                                float x1t2 = ObjectiveNexus.X + MathF.Cos(135 * MathF.PI / 180) * 100;
                                float y1t2 = ObjectiveNexus.Y + MathF.Sin(135 * MathF.PI / 180) * 100;
                                float x2t2 = ObjectiveNexus.X + MathF.Cos(315 * MathF.PI / 180) * 100;
                                float y2t2 = ObjectiveNexus.Y + MathF.Sin(315 * MathF.PI / 180) * 100;

                                LineT2 = new KeyValuePair<Vector2, Vector2>(new Vector2(x1t2, y1t2), new Vector2(x2t2, y2t2));

                                Center = new Vector2((ObjectiveNexus.X + ObjectivePlanetaryFortress.X) / 2, (ObjectiveNexus.Y + ObjectivePlanetaryFortress.Y) / 2);

                                Objective obj = DSdata.Objectives.FirstOrDefault(x => x.Center == Center);
                                if (obj == null)
                                {
                                    obj = new Objective();
                                    obj.Center = Center;
                                    obj.LineT1 = LineT1;
                                    obj.LineT2 = LineT2;
                                    obj.ObjectiveBunker = ObjectiveBunker;
                                    obj.ObjectiveNexus = ObjectiveNexus;
                                    obj.ObjectivePhotonCannon = ObjectivePhotonCannon;
                                    obj.ObjectivePlanetaryFortress = ObjectivePlanetaryFortress;
                                    DSdata.Objectives.Add(obj);
                                    obj.ID = DSdata.Objectives.Count;
                                }
                                replay.OBJECTIVE = obj.ID;

                            }
                        }
                        if (playerid == 0 || playerid > 12) continue;
                        

                        // Player
                        DSPlayer pl = replay.DSPlayer.SingleOrDefault(s => s.POS == playerid);
                        if (pl == null)
                        {
                            pl = replay.DSPlayer.SingleOrDefault(s => s.WORKINGSETSLOT == playerid - 1);
                            if (pl == null)
                                continue;
                            else
                                pl.POS = (byte)playerid;
                        };

                        // Race
                        if (gameloop < 1440)
                        {
                            Match m = rx_race2.Match(pydic["m_unitTypeName"].ToString());
                            if (m.Success && m.Groups[1].Value.Length > 0)
                            {
                                pl.RACE = m.Groups[1].Value;
                            }
                        }

                        if (pydic.ContainsKey("m_creatorAbilityName"))
                        {
                            if (pydic["m_creatorAbilityName"] == null || pydic["m_creatorAbilityName"].ToString() == "")
                            {
                                if (gameloop == 0)
                                {
                                    if (pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitBornEvent") {
                                        // Refineries init
                                        if (pydic["m_unitTypeName"].ToString().StartsWith("MineralField"))
                                        {
                                            int index = (int)pydic["m_unitTagIndex"];
                                            int recycle = (int)pydic["m_unitTagRecycle"];
                                            pl.Refineries.Add(new DbRefinery(gameloop, index, recycle, pl));
                                            continue;
                                        }



                                    }
                                }
                                if (gameloop < 480) continue;




                                if (noStagingAreaNextSpawn == false)
                                    if (gameloop - LastSpawn >= 9) continue;

                                if (pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitBornEvent")
                                {
                                    string born_unit = pydic["m_unitTypeName"].ToString();

                                    bool isSpawnUnit = (born_unit switch
                                    {
                                        "TrophyRiftPremium" => true,
                                        "MineralIncome" => true,
                                        "ParasiticBombRelayDummy" => true,
                                        "Biomass" => true,
                                        "PurifierAdeptShade" => true,
                                        "PurifierTalisShade" => true,
                                        "HornerReaperLD9ClusterCharges" => true,
                                        "Broodling" => true,
                                        "Raptorling" => true,
                                        "InfestedLiberatorViralSwarm" => true,
                                        "SplitterlingSpawn" => true,
                                        "GuardianShell" => true,
                                        "BroodlingStetmann" => true,
                                        _ => false
                                    });
                                    if (isSpawnUnit) continue;

                                    DbUnit unit = new DbUnit();
                                    unit.BornGameloop = gameloop;
                                    unit.Name = Fix.UnitName(born_unit);
                                    unit.Index = (int)pydic["m_unitTagIndex"];
                                    unit.RecycleTag = (int)pydic["m_unitTagRecycle"];
                                    unit.BornX = (int)pydic["m_x"];
                                    unit.BornY = (int)pydic["m_y"];

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
                                            int team = REParea.GetTeam(unit.BornX, unit.BornY);
                                            if (team == 1) pl.REALPOS = (byte)pos;
                                            else if (team == 2) pl.REALPOS = (byte)(pos + 3);
                                            pl.TEAM = (byte)(team - 1);
                                        }
                                    }

                                    // filter battlefield spawned units
                                    Vector2 point = new Vector2(unit.BornX, unit.BornY);
                                    float d = 0;
                                    if (pl.TEAM == 0)
                                        d = (point.X - LineT1.Key.X) * (LineT1.Value.Y - LineT1.Key.Y) - (point.Y - LineT1.Key.Y) * (LineT1.Value.X - LineT1.Key.X);
                                    else if (pl.TEAM == 1)
                                        d = (point.X - LineT2.Key.X) * (LineT2.Value.Y - LineT2.Key.Y) - (point.Y - LineT2.Key.Y) * (LineT2.Value.X - LineT2.Key.X);

                                    if (pl.TEAM == 0 && d > 0)
                                        continue;
                                    else if (pl.TEAM == 1 && d < 0)
                                        continue;
                                    
                                    pl.decUnits.Add(unit);

                                    if (noStagingAreaNextSpawn)
                                    {
                                        if (gameloop - LastSpawn >= 460)
                                            LastSpawn = gameloop;

                                    }

                                    if (!pl.Spawns.Any()) {
                                        DbSpawn spawn = new DbSpawn();
                                        spawn.Player = pl;
                                        spawn.Gameloop = LastSpawn;
                                        spawn.Units = new List<DbUnit>();
                                        pl.Spawns.Add(spawn);
                                        pl.LastSpawn = LastSpawn;
                                    }

                                    DbSpawn SpawnLast = pl.Spawns.OrderBy(o => o.Gameloop).Last();
                                    if (SpawnLast.Gameloop != LastSpawn)
                                    {
                                        DbSpawn spawn = new DbSpawn();
                                        spawn.Player = pl;
                                        spawn.Gameloop = LastSpawn;
                                        spawn.Units = new List<DbUnit>();
                                        pl.Spawns.Add(spawn);
                                        pl.Spawned = 1;
                                        pl.LastSpawn = LastSpawn;
                                        SpawnLast = spawn;
                                    }
                                    unit.Spawn = pl.Spawns.Last();
                                    SpawnLast.Units.Add(unit);
                                }
                            }
                        }
                    }
                    else if (pydic.ContainsKey("_event") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitTypeChangeEvent")
                    {
                        // Refinery taken
                        string m_unitTypeName = pydic["m_unitTypeName"].ToString();
                        if (m_unitTypeName.StartsWith("RefineryMinerals") || m_unitTypeName.StartsWith("AssimilatorMinerals") || m_unitTypeName.StartsWith("ExtractorMinerals"))
                        {
                            int index = (int)pydic["m_unitTagIndex"];
                            int recycletag = (int)pydic["m_unitTagRecycle"];
                            var refineries = replay.DSPlayer.Select(s => s.Refineries.SingleOrDefault(s => s.Index == index && s.RecycleTag == recycletag));
                            var refinery = refineries.SingleOrDefault(s => s != null);
                            if (refinery != null)
                                refinery.Gameloop = (int)pydic["_gameloop"];
                        }
                    }
                }
                // Middle
                else if (pydic.ContainsKey("m_unitTagIndex") && (int)pydic["m_unitTagIndex"] == 20 && pydic.ContainsKey("_event") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitOwnerChangeEvent") //22516
                {
                    int gameloop = (int)pydic["_gameloop"];
                    int upkeepid = (int)pydic["m_upkeepPlayerId"];

                    int team = 0;
                    if (upkeepid == 13)
                        team = 1;
                    else if (upkeepid == 14)
                        team = 2;

                    if (team > 0)
                        replay.Middle.Add(new DbMiddle(gameloop, team, replay));
                }
                // Unit died
                else if (pydic.ContainsKey("m_killerPlayerId") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitDiedEvent")
                {
                    
                    if ((int)pydic["_gameloop"] > 480)
                    {
                        if (pydic["m_unitTagRecycle"] != null)
                        {
                            if (PhotonCannon.Key == (int)pydic["m_unitTagIndex"] && PhotonCannon.Value == (int)pydic["m_unitTagRecycle"])
                                replay.Cannon = (int)pydic["_gameloop"];
                            else if (Bunker.Key == (int)pydic["m_unitTagIndex"] && Bunker.Value == (int)pydic["m_unitTagRecycle"])
                                replay.Bunker = (int)pydic["_gameloop"];

                            var units = replay.DSPlayer.Select(s => s.decUnits.SingleOrDefault(x => x.Index == (int)pydic["m_unitTagIndex"] && x.RecycleTag == (int)pydic["m_unitTagRecycle"]));
                            if (units != null)
                            {
                                var unit = units.SingleOrDefault(s => s != null);
                                if (unit != null)
                                {
                                    unit.DiedX = (int)pydic["m_x"];
                                    unit.DiedY = (int)pydic["m_y"];
                                    unit.DiedGameloop = (int)pydic["_gameloop"];

                                    if (pydic["m_killerPlayerId"] != null && pydic["m_killerUnitTagIndex"] != null && pydic["m_killerUnitTagRecycle"] != null)
                                    {
                                        unit.KillerPlayerRealPos = (int)pydic["m_killerPlayerId"];

                                        /*
                                        var kunits = replay.DSPlayer.Select(s => s.decUnits.SingleOrDefault(x => x.Index == (int)pydic["m_killerUnitTagIndex"] && x.RecycleTag == (int)pydic["m_killerUnitTagRecycle"]));
                                        if (kunits != null)
                                        {
                                            var kunit = units.SingleOrDefault(s => s != null);
                                            if (kunit != null)
                                            {
                                                unit.KillerPlayerRealPos = (int)pydic["m_killerPlayerId"];
                                                unit.KillerID = kunit;
                                            }
                                        }
                                        */
                                    }
                                }
                            }
                        }
                    }
                }
                // Gamemodes
                else if (isBrawl_set == false && pydic.ContainsKey("_gameloop") && (int)pydic["_gameloop"] == 0 && pydic.ContainsKey("m_upgradeTypeName")) // 56863
                {
                    
                    if (pydic["m_upgradeTypeName"].ToString().StartsWith("Mutation"))
                        Mutation.Add(pydic["m_upgradeTypeName"].ToString());
                }
                // Spawn
                
                else if (pydic.ContainsKey("m_upgradeTypeName") && pydic["m_upgradeTypeName"].ToString() == "StagingAreaNextSpawn") // 32299
                {
                    int playerid = (int)pydic["m_playerId"];
                    if (playerid == 0 || playerid > 6) continue;
                    int gameloop = (int)pydic["_gameloop"];
                    if (gameloop < 480) continue;
                    DSPlayer pl = replay.DSPlayer.SingleOrDefault(s => s.POS == playerid);
                    if (pl == null) continue;
                    int m_count = (int)pydic["m_count"];
                    LastSpawn = gameloop;
                    if (m_count == -1)
                    {
                        stagingAreaNextSpawns.Add(new StagingAreaNextSpawn(playerid, gameloop, m_count));
                        pl.LastSpawn = gameloop;
                        pl.Spawned = 1;
                    }
                }
                
                // Upgrades
                else if (pydic.ContainsKey("_event") && pydic.ContainsKey("m_upgradeTypeName") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUpgradeEvent")
                {
                    int gameloop = (int)pydic["_gameloop"];
                    if (gameloop < 450) continue;
                    int playerid = (int)pydic["m_playerId"];
                    if (playerid == 0 || playerid > 6) continue;

                    string upgrade = pydic["m_upgradeTypeName"].ToString();
                    if (upgrade == "MineralIncomeBonus") // 22202
                        continue;
                    if (upgrade.StartsWith("AFK")) // 4608
                        continue;
                    if (upgrade == "HighCapacityMode") // 880
                        continue;
                    if (upgrade.StartsWith("Mastery")) // 691
                        continue;
                    if (upgrade.StartsWith("Decoration")) // 49
                        continue;
                    if (upgrade.StartsWith("Emote")) // 36
                        continue;
                    if (upgrade == "MineralIncome")
                        continue;
                    if (upgrade.EndsWith("Disable")) // 4
                        continue;
                    if (upgrade == "PlayerIsAFK") // 0
                        continue;
                    if (upgrade == "DehakaSkillPoint")
                        continue;
                    if (upgrade == "HornerMySignificantOtherBuffHan")
                        continue;
                    if (upgrade == "HornerMySignificantOtherBuffHorner")
                        continue;
                    if (upgrade == "TychusFirstOnesontheHouse")
                        continue;
                    if (upgrade == "ClolarionInterdictorsBonus")
                        continue;
                    if (upgrade == "PartyFrameHide")
                        continue;

                    if (upgrade.EndsWith("Modification"))
                        continue;
                    if (upgrade.StartsWith("Blacklist"))
                        continue;
                    if (upgrade.StartsWith("DehakaHeroLevel"))
                        continue;
                    if (upgrade.Contains("Place"))
                        continue;
                    if (upgrade.StartsWith("PowerField"))
                        continue;
                    if (upgrade.StartsWith("RefinerySkin"))
                        continue;
                    if (upgrade.StartsWith("Theme"))
                        continue;
                    if (upgrade.StartsWith("WorkerSkin"))
                        continue;
                    if (upgrade.StartsWith("AreaFlair"))
                        continue;
                    if (upgrade.StartsWith("AreaWeather"))
                        continue;
                    if (upgrade.StartsWith("Aura"))
                        continue;
                    if (upgrade.StartsWith("Worker"))
                        continue;
                    if (upgrade == "HideWorkerCommandCard")
                        continue;
                    if (upgrade.EndsWith("Starlight"))
                        continue;

                    DSPlayer pl = replay.DSPlayer.SingleOrDefault(s => s.POS == playerid);
                    if (pl == null) continue;

                    // TODO fix Alarak/Artanis ..
                    string urace = pl.RACE;
                    if (pl.RACE == "Zagara" || pl.RACE == "Abathur" || pl.RACE == "Kerrigan")
                        urace = "Zerg";
                    else if (pl.RACE == "Alarak" || pl.RACE == "Artanis" || pl.RACE == "Vorazun" || pl.RACE == "Fenix" || pl.RACE == "Karax")
                        urace = "Protoss";
                    else if (pl.RACE == "Raynor" || pl.RACE == "Swann" || pl.RACE == "Nova" || pl.RACE == "Stukov")
                        urace = "Terran";

                    if (upgrade.StartsWith("Tier") || upgrade.StartsWith(pl.RACE))
                    {
                        pl.Upgrades.Add(new DbUpgrade(gameloop, upgrade, pl));
                    }
                    else if (!upgrade.Contains("Level"))
                    {
                        pl.Upgrades.Add(new DbUpgrade(gameloop, upgrade, pl));
                    } else if (upgrade.StartsWith(urace))
                    {
                        pl.Upgrades.Add(new DbUpgrade(gameloop, upgrade, pl));
                    }
                }
                // Stats
                else if (pydic.ContainsKey("m_stats")) // 1845
                {
                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    if (playerid == 0 || playerid > 6) continue;
                    DSPlayer pl = replay.DSPlayer.SingleOrDefault(s => s.POS == playerid);
                    if (pl == null) continue;

                    if (isBrawl_set == false)
                        isBrawl_set = true;

                    PythonDictionary pystats = pydic["m_stats"] as PythonDictionary;
                    DbStats m_stats = new DbStats();
                    m_stats.Gameloop = gameloop;
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
                    m_stats.Player = pl;
                    pl.Stats.Add(m_stats);
                    pl.PDURATION = (int)(gameloop / 22.4);
                    replay.DURATION = pl.PDURATION;

                    if (pl.Spawned == 2)
                    {
                        pl.ARMY = pl.ARMY + (m_stats.MineralsUsedActiveForces / 2);
                        pl.Spawned = 0;
                    }

                    m_stats.Army = pl.ARMY;

                    if (pl.Spawned == 1)
                        pl.Spawned = 2;

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

            return replay;
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
        public static class REParea
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

