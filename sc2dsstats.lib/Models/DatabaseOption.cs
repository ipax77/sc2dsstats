using System.Collections.Generic;

namespace sc2dsstats.lib.Models
{
    public class ReplayOptions
    {
        public bool ID { get; set; } = true;
        public bool REPLAY { get; set; } = false;
        public bool GAMETIME { get; set; } = true;
        public bool WINNER { get; set; } = false;
        public bool DURATION { get; set; } = false;
        public bool MAXLEAVER { get; set; } = false;
        public bool MINKILLSUM { get; set; } = false;
        public bool MININCOME { get; set; } = false;
        public bool MINARMY { get; set; } = false;
        public bool PLAYERCOUNT { get; set; } = false;
        public bool GAMEMODE { get; set; } = false;

        public Dictionary<string, bool> Opt { get; set; } = new Dictionary<string, bool>();

        public ReplayOptions()
        {
            Opt.Add("ID", ID);
            Opt.Add("REPLAY", REPLAY);
            Opt.Add("GAMETIME", GAMETIME);
            Opt.Add("WINNER", WINNER);
            Opt.Add("DURATION", DURATION);
            Opt.Add("MAXLEAVER", MAXLEAVER);
            Opt.Add("MINKILLSUM", MINKILLSUM);
            Opt.Add("MININCOME", MININCOME);
            Opt.Add("MINARMY", MINARMY);
            Opt.Add("PLAYERCOUNT", PLAYERCOUNT);
            Opt.Add("GAMEMODE", GAMEMODE);
        }
    }

    public class DBFind
    {
        public int ID { get; set; } = 0;
        public string RACE { get; set; } = "";
        public string RACEVS { get; set; } = "";
        public bool PLAYER { get; set; } = false;
        public string UNIT { get; set; } = "";
        public int UNITCOUNT { get; set; } = 1;
        public string UNITMOD { get; set; } = ">";
    }
}
