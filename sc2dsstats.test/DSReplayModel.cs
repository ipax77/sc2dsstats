using System;
using System.Collections.Generic;

namespace sc2dsstats.test
{
    public class DSReplay
    {
        public int ID { get; set; }
        public string REPLAY { get; set; }
        public DateTime GAMETIME { get; set; }
        public int WINNER { get; set; } = -1;
        public TimeSpan DURATION { get; set; } = TimeSpan.Zero;
        public int MINKILLSUM { get; set; }
        public int MAXKILLSUM { get; set; }
        public int MINARMY { get; set; }
        public double MININCOME { get; set; }
        public int MAXLEAVER { get; set; }
        public int PLAYERCOUNT { get; set; }
        public int REPORTED { get; set; } = 0;
        public string ISBRAWL { get; set; }
        public string GAMEMODE { get; set; } = "unknown";
        public string VERSION { get; set; } = "3.0";
        public string HASH { get; set; }
        public virtual ICollection<DSPlayer> DSPlayer { get; set; }
        public virtual ICollection<PLDuplicate> PLDuplicate { get; set; }
    }

    public class DSPlayer
    {
        public int ID { get; set; }
        public int POS { get; set; }
        public int REALPOS { get; set; } = 0;
        public string NAME { get; set; }
        public string RACE { get; set; }
        public int RESULT { get; set; }
        public int TEAM { get; set; }
        public int KILLSUM { get; set; } = 0;
        public double INCOME { get; set; } = 0;
        public TimeSpan PDURATION { get; set; } = TimeSpan.Zero;
        public int ARMY { get; set; } = 0;
        public int GAS { get; set; } = 0;
        public virtual ICollection<DSUnit> DSUnit { get; set; }
        public virtual DSReplay DSReplay { get; set; }
    }



    public class DSUnit
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string BP { get; set; }
        public int Count { get; set; }
        public virtual DSPlayer DSPlayer { get; set; }
    }

    public class PLDuplicate
    {
        public int ID { get; set; }
        public string Hash { get; set; }
        public int Pos { get; set; }
        public virtual DSReplay DSReplay { get; set; }
    }

}
