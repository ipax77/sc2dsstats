using paxgamelib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Diagnostics.Contracts;

namespace paxgamelib.Models
{
    public class Stats
    {
        public int ID { get; set; } = 0;
        public float DamageDone { get; set; } = 0;
        public float MineralValueKilled { get; set; } = 0;
        public float ArmyValue { get; set; } = 0;
        public Unit MVP { get; set; }
        public int RoundsWon { get; set; } = 0;

        public object ShallowCopy()
        {
            return this.MemberwiseClone();
        }
    }

    [Serializable]
    public class FinalStat
    {
        public double PlayerID { get; set; } = 0;
        public string Name { get; set; } = "";
        public int Race { get; set; } = 0;
        public int VsRace { get; set; } = 0;
        public string Vs { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Mode { get; set; } = "";
        public bool Victory { get; set; } = false;
        public int Round { get; set; } = 0;
        public float RoundsWon { get; set; } = 0;
        public double Damage { get; set; } = 0;
    }

    [Serializable]
    public class StatsRound
    {
        public float ArmyHPT1 { get; set; } = 0;
        public float ArmyHPT2 { get; set; } = 0;
        public int winner { get; set; } = 0;
        public List<float> Damage { get; set; } = new List<float>();
        public List<float> Killed { get; set; } = new List<float>();
        [JsonIgnore]
        public List<Unit> Mvp { get; set; } = new List<Unit>();
        public List<float> Army { get; set; } = new List<float>();
        public List<float> Tech { get; set; } = new List<float>();
        [JsonIgnore]
        public Unit MVP { get; set; } = new Unit();
    }

    public class M_stats
    {
        public float ArmyHPTeam1 { get; set; } = 0;
        public float ArmyHPTeam2 { get; set; } = 0;
        public float RoundsWon { get; set; } = 0;
        public float DamageDone { get; set; } = 0;
        public float VlaueKilled { get; set; } = 0;
        public float ArmyValue { get; set; } = 0;
        public float Upgrades { get; set; } = 0;
    }

    [Serializable]
    public class RandomResult
    {
        public float MineralValueKilled { get; set; }
        public float DamageDone { get; set; }
    }

    [Serializable]
    public class RandomGame
    {
        public BBuild player1 { get; set; }
        public BBuild player2 { get; set; }
        public int Result { get; set; }
        public RandomResult result1 {get; set;}
        public RandomResult result2 { get; set; }
    }
}
