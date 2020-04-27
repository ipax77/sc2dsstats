using sc2dsstats.lib.Data;
using sc2dsstats.lib.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace sc2dsstats.lib.Models
{
    [NotMapped]
    public class DbStats
    {
        public int ID { get; set; }
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
        public virtual DSPlayer Player { get; set; }
    }

    public class DbUnit
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int BornGameloop { get; set; }
        public int BornX { get; set; }
        public int BornY { get; set; }
        public int DiedGameloop { get; set; }
        public int DiedX { get; set; }
        public int DiedY { get; set; }
        [NotMapped]
        public int Index { get; set; }
        [NotMapped]
        public int RecycleTag { get; set; }
        public int KillerPlayerRealPos { get; set; }
        //public virtual DbUnit KillerID { get; set; }
        [NotMapped]
        public virtual DbSpawn Spawn { get; set; }
        public virtual DbBreakpoint Breakpoint { get; set; }
    }
    [NotMapped]
    public class DbSpawn
    {
        public int ID { get; set; }
        public int Gameloop { get; set; }
        public virtual DSPlayer Player { get; set; }
        public virtual ICollection<DbUnit> Units { get; set; }
    }
    [NotMapped]
    public class DbRefinery
    {
        public int ID { get; set; }
        public int Gameloop { get; set; }
        public virtual DSPlayer Player { get; set; }
        public int Index { get; set; }
        public int RecycleTag { get; set; }

        public DbRefinery()
        {

        }

        public DbRefinery(int gameloop, int index, int recycletag, DSPlayer pl) :this()
        {
            Gameloop = gameloop;
            Index = index;
            RecycleTag = recycletag;
            Player = pl;
        }

    }

    public class DbUpgrade
    {
        public int ID { get; set; }
        public int Gameloop { get; set; }
        public string Upgrade { get; set; }
        [NotMapped]
        public virtual DSPlayer Player { get; set; }
        public virtual DbBreakpoint Breakpoint { get; set; }

        public DbUpgrade()
        {

        }

        public DbUpgrade(int gameloop, string upgrade, DSPlayer pl) : this()
        {
            Gameloop = gameloop;
            Upgrade = upgrade;
            Player = pl;
        }

    }

    public class DbMiddle
    {
        [JsonIgnore]
        public int ID { get; set; }
        public int Gameloop { get; set; }
        public byte Team { get; set; }
        [JsonIgnore]
        public virtual DSReplay Replay { get; set; }

        public DbMiddle()
        {

        }

        public DbMiddle(int gameloop, int team, DSReplay replay) : this()
        {
            Gameloop = gameloop;
            Team = (byte)team;
            Replay = replay;
        }
    }

    public class DbBreakpoint
    {
        [JsonIgnore]
        public int ID { get; set; }
        public string Breakpoint { get; set; }
        public int Gas { get; set; } = 0;
        public int Income { get; set; } = 0;
        public int Army { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Upgrades { get; set; } = 0;
        public int Tier { get; set; } = 0;
        [JsonIgnore]
        public virtual DSPlayer Player { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DSUnit> Units { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbUnit> DbUnits { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbUpgrade> DbUpgrades { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbUpgrade> DbAbilities { get; set; }
        public int Mid { get; set; }
        public string dsUnitsString { get; set; }
        public string dbUnitsString { get; set; }
        public string dbUpgradesString { get; set; }

        public List<UnitModelCount> GetUnits()
        {
            List<UnitModelCount> Units = new List<UnitModelCount>();
            if (!String.IsNullOrEmpty(dsUnitsString))
            {
                foreach (string unitstring in dsUnitsString.Split("|"))
                {
                    var ent = unitstring.Split(",");
                    if (ent.Length == 2)
                    {
                        int id = 0;
                        UnitModelBase unit = null;
                        if (int.TryParse(ent[0], out id))
                        {
                            unit = DSdata.Units.FirstOrDefault(x => x.ID == id);
                            if (unit == null)
                                unit = new UnitModelBase(Fix.UnitName(ent[0]), "");
                        }
                        else
                            unit = new UnitModelBase(Fix.UnitName(ent[0]), "");

                        int c = 0;
                        if (int.TryParse(ent[1], out c))
                            Units.Add(new UnitModelCount(unit, c));
                    }
                }
            }
            return Units;
        }

    }

    public class Objective
    {
        public int ID { get; set; }
        public Vector2 ObjectivePlanetaryFortress { get; set; } = Vector2.Zero;
        public Vector2 ObjectiveNexus { get; set; } = Vector2.Zero;
        public Vector2 ObjectiveBunker { get; set; } = Vector2.Zero;
        public Vector2 ObjectivePhotonCannon { get; set; } = Vector2.Zero;
        public Vector2 Center { get; set; } = Vector2.Zero;
        
        public KeyValuePair<Vector2, Vector2> LineT1 { get; set; } = new KeyValuePair<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
        public KeyValuePair<Vector2, Vector2> LineT2 { get; set; } = new KeyValuePair<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
    }
}
