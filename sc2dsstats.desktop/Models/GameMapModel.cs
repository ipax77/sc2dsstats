using paxgamelib.Models;
using System.Collections.Generic;

namespace sc2dsstats.desktop.Models
{
    public class GameMapModel
    {
        public int ReplayID { get; set; } = 0;
        public Dictionary<int, List<Unit>> Spawns = new Dictionary<int, List<Unit>>();
        public Dictionary<int, HashSet<int>> plSpawns = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, Dictionary<int, List<UnitUpgrade>>> Upgrades = new Dictionary<int, Dictionary<int, List<UnitUpgrade>>>();
        public Dictionary<int, Dictionary<int, List<UnitAbility>>> AbilityUpgrades = new Dictionary<int, Dictionary<int, List<UnitAbility>>>();
    }
}
