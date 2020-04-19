using System;
using System.Collections.Generic;

namespace paxgamelib.Models
{
    [Serializable]
    public class UnitUpgrade
    {
        public int ID { get; set; }
        public UnitUpgrades Upgrade { get; set; }
        public int Level { get; set; }
    }

    public enum UnitUpgrades
    {
        GroundAttac,
        GroundArmor,
        GroundMeleeAttac,
        VehicelAttac,
        VehicelArmor,
        AirAttac,
        AirArmor,
        ShieldArmor
    }

    public class Upgrade
    {
        public int ID { get; set; }
        public UnitUpgrades Name { get; set; }
        public UnitRace Race { get; set; }
        public List<KeyValuePair<int, int>> Cost { get; set; } = new List<KeyValuePair<int, int>>();
        public string Icon { get; set; }
    }
}
