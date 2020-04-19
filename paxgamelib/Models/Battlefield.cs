using paxgamelib.Data;
using paxgamelib.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace paxgamelib.Models
{
    [Serializable]
    public class Battlefield
    {
        public int ID { get; set; }
        public const int Xmax = 60;
        public const int Ymax = 30;
        public static Vector2 Target1 = new Vector2(0, Ymax / 2);
        public static Vector2 Target2 = new Vector2(Xmax, Ymax / 2);
        public static TimeSpan Ticks = new TimeSpan(0, 0, 0, 0, 250);
        public Unit Def1 = UnitPool.Units.SingleOrDefault(x => x.Name == "CommandCenter1").DeepCopy();
        public Unit Def2 = UnitPool.Units.SingleOrDefault(x => x.Name == "CommandCenter2").DeepCopy();
        public int Done = 0;

        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public bool Computing { get; set; } = false;
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public List<Unit> Units { get; set; } = new List<Unit>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public ConcurrentDictionary<int, ConcurrentBag<Unit>> Rounds { get; set; } = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public List<Unit> KilledUnits { get; set; } = new List<Unit>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public ConcurrentDictionary<Vector2, bool> UnitPostions { get; set; } = new ConcurrentDictionary<Vector2, bool>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public ConcurrentDictionary<int, ConcurrentBag<Unit>> Status { get; set; } = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public ConcurrentDictionary<int, ConcurrentBag<Unit>> StatusKilled { get; set; } = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public List<KeyValuePair<KeyValuePair<float, float>, KeyValuePair<float, float>>> Health { get; set; }

        public Battlefield()
        {
            Def1.RelPos = MoveService.GetRelPos(Def1.RealPos);
            Def2.RelPos = MoveService.GetRelPos(Def2.RealPos);
        }
    }
}
