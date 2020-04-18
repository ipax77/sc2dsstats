using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sc2dsstats.decode.Models
{
    public class DecReplay
    {
        public string Replay { get; set; }
        public DateTime Gametime { get; set; }
        public sbyte Winner { get; set; } = -1;
        public int Duration { get; set; } = 0;
        public byte PlayerCount { get; set; }
        public string Gamemode { get; set; } = "unknown";
        public string Version { get; set; } = "3.0";
        public string Hash { get; set; }
        public string ReplayPath { get; set; }
        public List<DecPlayer> Players { get; set; } = new List<DecPlayer>();
        public List<KeyValuePair<int, int>> Middle { get; set; } = new List<KeyValuePair<int, int>>();
    }

    public class DecPlayer
    {
        public int Pos { get; set; }
        public int WorkingSlot { get; set; }
        public int RealPos { get; set; } = 0;
        public int Team { get; set; }
        public string Name { get; set; }
        public string Race { get; set; }
        public int Result { get; set; }
        public int LastSpawn { get; set; }
        public int Duration { get; set; }
        public int Spawned { get; set; } = 0;
        public int Army { get; set; } = 0;
        public List<DecUnit> decUnits { get; set; } = new List<DecUnit>();
        public List<DecStats> DecStats { get; set; } = new List<DecStats>();
        public List<DecRefinery> Refineries { get; set; } = new List<DecRefinery>();
        public List<KeyValuePair<int, int>> Tiers { get; set; } = new List<KeyValuePair<int, int>>();
        public List<KeyValuePair<int, string>> Upgrades { get; set; } = new List<KeyValuePair<int, string>>();
        public SortedDictionary<int, List<DecUnit>> Spawns { get; set; } = new SortedDictionary<int, List<DecUnit>>();
        public List<DecBreakpoint> Breakpoints { get; set; } = new List<DecBreakpoint>();
    }

    public class DecBreakpoint
    {
        public string Breakpoint { get; set; }
        public int Gas { get; set; } = 0;
        public int Income { get; set; } = 0;
        public int Army { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Upgrades { get; set; } = 0;
        public int Tier { get; set; } = 1;
        public Dictionary<string, int> Units { get; set; } = new Dictionary<string, int>();


        public DecBreakpoint()
        {
        }

        public DecBreakpoint(DecPlayer pl, int bp)
        {
            Gas = pl.Refineries.Where(x => x.Gameloop > 0 && x.Gameloop <= bp).Count();
            Income = (int)pl.DecStats.Where(x => x.Gameloop <= bp).Sum(s => s.MineralsCollectionRate / 9.15);
            DecStats statlast = null;
            foreach (DecStats stat in pl.DecStats)
            {
                if (stat.Gameloop > bp)
                    break;
                statlast = stat;
            }
            if (statlast != null)
            {
                Army = statlast.Army;
                Kills = statlast.MineralsKilledArmy;
                Upgrades = statlast.MineralsUsedCurrentTechnology;
            }
            Tier = pl.Tiers.Where(x => x.Key <= bp).Select(s => s.Value).LastOrDefault();

            List<DecUnit> decUnits = null;
            foreach (var ent in pl.Spawns)
            {
                if (ent.Key > bp)
                    break;
                decUnits = ent.Value;
            }
            if (decUnits != null)
            {
                foreach (DecUnit unit in decUnits)
                {
                    if (!Units.ContainsKey(unit.Name))
                        Units[unit.Name] = 1;
                    else
                        Units[unit.Name]++;
                }
            }
        }
    }


    public class DecUnit
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int BornGameloop { get; set; }
        public int BornX { get; set; }
        public int BornY { get; set; }
        public int DiedGameloop { get; set; }
        public int DiedX { get; set; }
        public int DiedY { get; set; }
        public int Index { get; set; }
        public int RecycleTag { get; set; }
        public int KillerPlayerRealPos { get; set; }
        public int KillerID { get; set; }
    }

    public class StagingAreaNextSpawn
    {
        public int Pos { get; set; }
        public int Gameloop { get; set; }
        public int Count { get; set; }

        public StagingAreaNextSpawn(int pos, int gameloop, int count)
        {
            Pos = pos;
            Gameloop = gameloop;
            Count = count;
        }
    }

    public class DecRefinery
    {
        public int Index { get; set; }
        public int RecycleTag { get; set; }
        public int PlayerId { get; set; }
        public int Gameloop { get; set; }

        public DecRefinery(int index, int recycletag, int playerid, int gameloop)
        {
            Index = index;
            RecycleTag = recycletag;
            PlayerId = playerid;
            Gameloop = gameloop;
        }
    }

    public class DecStats
    {
        public int Gameloop { get; set; }
        public int FoodUsed { get; set; } = 0;
        public int MineralsCollectionRate { get; set; } = 0;
        public int MineralsCurrent { get; set; } = 0;
        public int MineralsFriendlyFireArmy { get; set; } = 0;
        public int MineralsFriendlyFireTechnology { get; set; } = 0;
        public int MineralsKilledArmy { get; set; } = 0;
        public int MineralsKilledTechnology { get; set; } = 0;
        public int MineralsLostArmy { get; set; } = 0;
        public int MineralsUsedActiveForces { get; set; } = 0;
        public int MineralsUsedCurrentArmy { get; set; } = 0;
        public int MineralsUsedCurrentTechnology { get; set; } = 0;
        public int Army { get; set; } = 0;
        
    }
}

