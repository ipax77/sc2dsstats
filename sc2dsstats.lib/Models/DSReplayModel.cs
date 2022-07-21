using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace sc2dsstats.lib.Models
{

    public class DSReplayBase
    {
        [JsonIgnore]
        public int ID { get; set; }
        public string REPLAY { get; set; }
        public DateTime GAMETIME { get; set; }
        public sbyte WINNER { get; set; } = -1;
        public int DURATION { get; set; } = 0;
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
        public string REPLAYPATH { get; set; }
        public int OBJECTIVE { get; set; }
        [NotMapped]
        public int Bunker { get; set; } = 0;
        [NotMapped]
        public int Cannon { get; set; } = 0;
        public DateTime Upload { get; set; }
    }

    public class DSReplay : DSReplayBase
    {
        public virtual ICollection<DSPlayer> DSPlayer { get; set; }
        public virtual ICollection<DbMiddle> Middle { get; set; }
        //public virtual ICollection<PLDuplicate> PLDuplicate { get; set; }


        public int GetMiddle(int gameloop, int team)
        {
            KeyValuePair<int, int> lastent = new KeyValuePair<int, int>(0, 0);
            int mid = 0;
            bool hasInfo = false;
            foreach (var ent in Middle.OrderBy(o => o.Gameloop))
            {
                if (ent.Gameloop > gameloop)
                {
                    hasInfo = true;
                    if (lastent.Value == team + 1)
                        mid += gameloop - lastent.Key;
                    break;
                }

                if (lastent.Key > 0 && lastent.Value == team + 1)
                    mid += ent.Gameloop - lastent.Key;

                lastent = new KeyValuePair<int, int>(ent.Gameloop, ent.Team);
            }
            if (Middle.Any())
                if (hasInfo == false && Middle.OrderBy(o => o.Gameloop).Last().Team == team + 1)
                    mid += gameloop - Middle.OrderBy(o => o.Gameloop).Last().Gameloop;

            if (mid < 0) mid = 0;
            return mid;
        }

        public string GenHash()
        {
            string md5 = "";
            string hashstring = "";
            foreach (DSPlayer pl in DSPlayer.OrderBy(o => o.POS))
            {
                hashstring += pl.POS + pl.RACE;
            }
            hashstring += MINARMY + MINKILLSUM + MININCOME + MAXKILLSUM;
            using (MD5 md5Hash = MD5.Create())
            {
                md5 = GetMd5Hash(md5Hash, hashstring);
            }
            return md5;
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }

    public class DSPlayerBase
    {
        [JsonIgnore]
        public int ID { get; set; }
        public byte POS { get; set; }
        public byte REALPOS { get; set; } = 0;
        [NotMapped]
        [JsonIgnore]
        public byte WORKINGSETSLOT { get; set; } = 0;
        public string NAME { get; set; }
        public string RACE { get; set; }
        public string OPPRACE { get; set; }
        public bool WIN { get; set; } = false;
        [NotMapped]
        [JsonIgnore]
        public byte RESULT { get; set; } = 0;
        public byte TEAM { get; set; }
        public int KILLSUM { get; set; } = 0;
        public int INCOME { get; set; } = 0;
        public int PDURATION { get; set; } = 0;
        public int ARMY { get; set; } = 0;
        public byte GAS { get; set; } = 0;
    }

    public class DSPlayer : DSPlayerBase
    {
        [JsonIgnore]
        public virtual DSReplay DSReplay { get; set; }
        [NotMapped]
        [JsonIgnore]
        public int Spawned { get; set; } = 0;
        [NotMapped]
        [JsonIgnore]
        public int LastSpawn { get; set; } = 0;
        [NotMapped]
        [JsonIgnore]
        public List<DbUnit> decUnits { get; set; } = new List<DbUnit>();
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DSUnit> DSUnit { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbRefinery> Refineries { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbUpgrade> Upgrades { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbSpawn> Spawns { get; set; }
        [NotMapped]
        [JsonIgnore]
        public virtual ICollection<DbStats> Stats { get; set; }
        public virtual ICollection<DbBreakpoint> Breakpoints { get; set; }
    }


    public class DSUnitBase
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string BP { get; set; }
        public int Count { get; set; }

    }
    [NotMapped]
    public class DSUnit : DSUnitBase
    {
        public virtual DbBreakpoint Breakpoint { get; set; }
        [JsonIgnore]
        [NotMapped]
        public virtual DSPlayer DSPlayer { get; set; }
    }
}
