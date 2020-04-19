using paxgamelib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace paxgamelib.Models
{
    [Serializable]
    public class GameHistory
    {
        public ulong ID { get; set; }
        public int Spawn { get; set; } = 0;
        public List<Player> Players { get; set; } = new List<Player>();
        public Battlefield battlefield { get; set; }
        public DateTime Gametime { get; set; } = DateTime.UtcNow;
        public Version Version { get; set; } = new Version("0.0.1");
        private int UnitID = 1000;
        public GameMode Mode { get; set; }
        public List<StatsRound> Stats { get; set; } = new List<StatsRound>();
        [JsonIgnore]
        public string Style { get; set; }
        [JsonIgnore]
        public List<Unit> Units { get; set; }
        [JsonIgnore]
        public List<KeyValuePair<float, float>> Health { get; set; }

        public GameHistory()
        {
        }

        public object ShallowCopy()
        {
            return this.MemberwiseClone();
        }

        public int GetUnitID()
        {
            return ++UnitID;
        }
    }
}
