using paxgamelib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace paxgamelib.Models
{
    [Serializable]
    public class Player
    {
        public ulong ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public string AuthName { get; set; }
        public int Pos { get; set; } = 1;
        public UnitRace Race { get; set; } = UnitRace.Terran;
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public List<Unit> Units { get; set; } = new List<Unit>();
        public int Tier { get; set; } = 1;
        public int MineralsCurrent { get; set; }
        public List<UnitUpgrade> Upgrades { get; set; } = new List<UnitUpgrade>();
        public List<UnitAbility> AbilityUpgrades { get; set; } = new List<UnitAbility>();
        public bool inGame { get; set; } = false;
        public BBuild LastSpawn { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public HashSet<UnitAbilities> AbilityUpgradesAvailable { get; set; } = new HashSet<UnitAbilities>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public HashSet<UnitUpgrades> UpgradesAvailable { get; set; } = new HashSet<UnitUpgrades>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Dictionary<UnitAbilities, bool> AbilitiesGlobalDeactivated { get; set; } = new Dictionary<UnitAbilities, bool>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Dictionary<int, Dictionary<UnitAbilities, bool>> AbilitiesSingleDeactivated { get; set; } = new Dictionary<int, Dictionary<UnitAbilities, bool>>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public GameHistory Game { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public GameMode Mode { get; set; } = new GameMode();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Dictionary<int, M_stats> Stats { get; set; } = new Dictionary<int, M_stats>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public int Gameloop { get; set; } = 0;
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        BBuild Build { get; set; } = new BBuild();

        public Player()
        {
        }

        public void Reset()
        {
            Race = UnitRace.Terran;
            Units = UnitPool.Units.Where(x => x.Race == UnitRace.Terran && x.Cost > 0).ToList();
            Tier = 1;
            MineralsCurrent = 0;
            Upgrades.Clear();
            AbilityUpgrades.Clear();
            inGame = false;
            LastSpawn = null;
            AbilityUpgradesAvailable.Clear();
            UpgradesAvailable.Clear();
            AbilitiesGlobalDeactivated.Clear();
            AbilitiesSingleDeactivated.Clear();
            Game = null;
            Stats.Clear();
            Gameloop = 0;
        }

        public void SoftReset(int minerals = 0)
        {
            Units = UnitPool.Units.Where(x => x.Race == this.Race && x.Cost > 0).ToList();
            Tier = 1;
            MineralsCurrent = minerals;
            Upgrades.Clear();
            AbilityUpgrades.Clear();
            LastSpawn = null;
            AbilityUpgradesAvailable.Clear();
            UpgradesAvailable.Clear();
            AbilitiesGlobalDeactivated.Clear();
            AbilitiesSingleDeactivated.Clear();
            Stats.Clear();
            Gameloop = 0;
        }

        public Player Deepcopy()
        {
            Player pl = new Player();
            pl.ID = ID;
            pl.Name = Name;
            pl.AuthName = AuthName;
            pl.Pos = Pos;
            pl.Race = Race;
            pl.Units = new List<Unit>(Units);
            pl.Tier = Tier;
            pl.MineralsCurrent = MineralsCurrent;
            pl.Upgrades = new List<UnitUpgrade>(Upgrades);
            pl.AbilityUpgrades = new List<UnitAbility>(AbilityUpgrades);
            // no ability deactivated copy ..
            pl.inGame = inGame;
            pl.LastSpawn = LastSpawn;
            pl.Game = Game; // no deepcopy
            pl.Mode = Mode; // no deepcopy
            pl.Stats = new Dictionary<int, M_stats>(Stats);
            pl.Gameloop = Gameloop;

            return pl;
        }

        public Player SoftCopy()
        {
            Player pl = new Player();
            pl.ID = paxgame.GetPlayerID();
            pl.Pos = Pos;
            pl.Race = Race;
            pl.Units = UnitPool.Units.Where(x => x.Race == pl.Race && x.Cost > 0).ToList();
            pl.inGame = true;
            return pl;
        }

        public string GetString()
        {
            return this.Build.GetString(this);
        }

        public void SetString(string build)
        {
            this.Build.SetString(build, this);
        }

        public BBuild GetBuild()
        {
            return this.Build.GetBuild(this);
        }

        public void SetBuild(BBuild build)
        {
            this.Build = build;
            this.Build.SetBuild(this);
        }

        public List<int> GetAIMoves()
        {
            return this.Build.GetAIMoves(this);
        }

        public void SetAIMoves(List<int> moves)
        {
            this.Build.SetAIMoves(moves, this);
        }

        public void AddAIMove(int move)
        {
            this.Build.AddAIMove(move, this);
        }
    }
}
