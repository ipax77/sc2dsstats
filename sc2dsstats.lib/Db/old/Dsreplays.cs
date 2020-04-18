using System;
using System.Collections.Generic;

namespace sc2dsstats.lib.Db.Models
{
    public partial class Dsreplays
    {
        public Dsreplays()
        {
            Dsplayers = new HashSet<Dsplayers>();
        }

        public int Id { get; set; }
        public string Replay { get; set; }
        public DateTime Gametime { get; set; }
        public sbyte Winner { get; set; }
        public int Minkillsum { get; set; }
        public int Maxkillsum { get; set; }
        public int Minarmy { get; set; }
        public int Minincome { get; set; }
        public int Maxleaver { get; set; }
        public byte Playercount { get; set; }
        public byte Reported { get; set; }
        public bool Isbrawl { get; set; }
        public string Gamemode { get; set; }
        public string Version { get; set; }
        public string Hash { get; set; }
        public string Replaypath { get; set; }
        public int? Duration { get; set; }

        public virtual ICollection<Dsplayers> Dsplayers { get; set; }
    }
}
