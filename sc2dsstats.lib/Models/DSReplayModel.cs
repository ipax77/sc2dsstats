using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.lib.Models
{

    public class DSReplayBase
    {
        public int ID { get; set; }
        public string REPLAY { get; set; }
        public DateTime GAMETIME { get; set; }
        public sbyte WINNER { get; set; } = -1;
        public TimeSpan DURATION { get; set; } = TimeSpan.Zero;
        public int MINKILLSUM { get; set; }
        public int MAXKILLSUM { get; set; }
        public int MINARMY { get; set; }
        public int MININCOME { get; set; }
        public int MAXLEAVER { get; set; }
        public byte PLAYERCOUNT { get; set; }
        public byte REPORTED { get; set; } = 0;
        public bool ISBRAWL { get; set; }
        public string GAMEMODE { get; set; } = "unknown";
        public string VERSION { get; set; } = "3.0";
        public string HASH { get; set; }
        public virtual ICollection<DSPlayer> DSPlayer { get; set; }
        public virtual ICollection<PLDuplicate> PLDuplicate { get; set; }
    }


    public class DSReplay : DSReplayBase
    {
    }

    public class DSPlayer
    {
        public int ID { get; set; }
        public byte POS { get; set; }
        public byte REALPOS { get; set; } = 0;
        public string NAME { get; set; }
        public string RACE { get; set; }
        public string OPPRACE { get; set; }
        public bool WIN { get; set; } = false;
        public byte TEAM { get; set; }
        public int KILLSUM { get; set; } = 0;
        public int INCOME { get; set; } = 0;
        public TimeSpan PDURATION { get; set; } = TimeSpan.Zero;
        public int ARMY { get; set; } = 0;
        public byte GAS { get; set; } = 0;
        public virtual ICollection<DSUnit> DSUnit { get; set; }
        public virtual DSReplay DSReplay { get; set; }
        public virtual PLDuplicate PLDuplicate { get; set; }

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
        public byte Pos { get; set; }
        public virtual DSReplay DSReplay { get; set; }
    }

    public class DefaultFilterCmdr : DSReplayBase
    {
    }

    public class DSPlayerResult
    {
        public int ID { get; set; }
        public DateTime GAMETIME { get; set; }
        public int WINNER { get; set; }
        public int MAXKILLSUM { get; set; }
        public int REALPOS { get; set; }
        public string NAME { get; set; }
        public string RACE { get; set; }
        public int TEAM { get; set; }
        public int KILLSUM { get; set; }
        public string OPPRACE { get; set; }
    }

}
