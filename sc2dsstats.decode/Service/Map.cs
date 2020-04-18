using sc2dsstats.decode.Models;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sc2dsstats.decode.Service
{
    public static class Map_deprecated
    {
        public static DSReplay Rep(DecReplay rep)
        {
            DSReplay dbrep = new DSReplay();
            dbrep.REPLAYPATH = rep.ReplayPath;
            using (var md5 = MD5.Create())
            {
                string dirHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetDirectoryName(rep.ReplayPath)))).Replace("-", "").ToLowerInvariant();
                string fileHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(rep.ReplayPath)))).Replace("-", "").ToLowerInvariant();
                dbrep.REPLAY = dirHash + fileHash;
            }
            dbrep.GAMETIME = rep.Gametime;
            dbrep.WINNER = rep.Winner;
            dbrep.DURATION = rep.Duration;
            dbrep.PLAYERCOUNT = (byte)rep.PlayerCount;
            dbrep.GAMEMODE = rep.Gamemode;
            dbrep.VERSION = rep.Version;

            List<DSPlayer> pls = new List<DSPlayer>();
            foreach (DecPlayer pl in rep.Players)
            {
                DSPlayer dbpl = new DSPlayer();
                dbpl.POS = (byte)pl.Pos;
                dbpl.REALPOS = (byte)pl.RealPos;
                dbpl.NAME = pl.Name;
                dbpl.RACE = pl.Race;
                if (pl.Team == rep.Winner)
                    dbpl.WIN = true;
                dbpl.TEAM = (byte)pl.Team;
                dbpl.PDURATION = pl.Duration;
                dbpl.ARMY = pl.Army;
                
                dbpl.OPPRACE = pl.Race;
                dbpl.DSReplay = dbrep;
                pls.Add(dbpl);



            }
            dbrep.DSPlayer = pls as ICollection<DSPlayer>;
            return dbrep;
        }



        public static dsreplay Rep(DSReplay rep)
        {
            dsreplay dbrep = new dsreplay();
            dbrep.REPLAY = rep.REPLAY;
            dbrep.GAMETIME = double.Parse(rep.GAMETIME.ToString("yyyyMMddHHmmss"));
            dbrep.WINNER = (sbyte)rep.WINNER;
            dbrep.DURATION = (int)(rep.DURATION * 22.4);
            dbrep.MINKILLSUM = rep.MINKILLSUM;
            dbrep.MAXKILLSUM = rep.MAXKILLSUM;
            dbrep.MINARMY = rep.MINARMY;
            dbrep.MININCOME = (int)rep.MININCOME;
            dbrep.MAXLEAVER = rep.MAXLEAVER;
            dbrep.PLAYERCOUNT = (byte)rep.PLAYERCOUNT;
            dbrep.REPORTED = (byte)rep.REPORTED;
            dbrep.ISBRAWL = rep.ISBRAWL;
            dbrep.GAMEMODE = rep.GAMEMODE;
            dbrep.VERSION = rep.VERSION;
            dbrep.HASH = rep.HASH;

            foreach (DSPlayer pl in rep.DSPlayer)
            {
                dsplayer dbpl = new dsplayer();
                dbpl.POS = (byte)pl.POS;
                dbpl.REALPOS = (byte)pl.REALPOS;
                dbpl.NAME = pl.NAME;
                dbpl.RACE = pl.RACE;
                dbpl.TEAM = (byte)pl.TEAM;
                dbpl.KILLSUM = pl.KILLSUM;
                dbpl.INCOME = (int)pl.INCOME;
                dbpl.PDURATION = (int)(pl.PDURATION * 22.4);
                dbpl.ARMY = pl.ARMY;
                dbpl.GAS = (byte)pl.GAS;

                foreach (var dsunit in pl.DSUnit)
                {
                    if (!dbpl.UNITS.ContainsKey(dsunit.BP))
                        dbpl.UNITS[dsunit.BP] = new Dictionary<string, int>();
                    dbpl.UNITS[dsunit.BP][dsunit.Name] = dsunit.Count;
                }
                dbrep.PLAYERS.Add(dbpl);
            }
            return dbrep;
        }
        public static DSReplay Rep(dsreplay rep)
        {
            DSReplay dbrep = new DSReplay();
            dbrep.REPLAYPATH = rep.REPLAY;
            using (var md5 = MD5.Create())
            {
                string dirHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetDirectoryName(rep.REPLAY)))).Replace("-", "").ToLowerInvariant();
                string fileHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(rep.REPLAY)))).Replace("-", "").ToLowerInvariant();
                dbrep.REPLAY = dirHash + fileHash;
            }
            string gametime = rep.GAMETIME.ToString();
            int year = int.Parse(gametime.Substring(0, 4));
            int month = int.Parse(gametime.Substring(4, 2));
            int day = int.Parse(gametime.Substring(6, 2));
            int hour = int.Parse(gametime.Substring(8, 2));
            int min = int.Parse(gametime.Substring(10, 2));
            int sec = int.Parse(gametime.Substring(12, 2));
            DateTime gtime = new DateTime(year, month, day, hour, min, sec);
            dbrep.GAMETIME = gtime;
            dbrep.WINNER = (sbyte)rep.WINNER;
            TimeSpan d = TimeSpan.FromSeconds(rep.DURATION / 22.4);
            dbrep.DURATION = (int)d.TotalSeconds;
            dbrep.MINKILLSUM = rep.MINKILLSUM;
            dbrep.MAXKILLSUM = rep.MAXKILLSUM;
            dbrep.MINARMY = rep.MINARMY;
            dbrep.MININCOME = (int)rep.MININCOME;
            dbrep.MAXLEAVER = rep.MAXLEAVER;
            dbrep.PLAYERCOUNT = (byte)rep.PLAYERCOUNT;
            dbrep.REPORTED = (byte)rep.REPORTED;
            dbrep.ISBRAWL = rep.ISBRAWL;
            dbrep.GAMEMODE = rep.GAMEMODE;
            dbrep.VERSION = rep.VERSION;
            dbrep.HASH = rep.HASH;

            List<DSPlayer> pls = new List<DSPlayer>();
            foreach (dsplayer pl in rep.PLAYERS)
            {
                DSPlayer dbpl = new DSPlayer();
                dbpl.POS = (byte)pl.POS;
                dbpl.REALPOS = (byte)pl.REALPOS;
                dbpl.NAME = pl.NAME;
                dbpl.RACE = pl.RACE;
                if (pl.TEAM == rep.WINNER)
                    dbpl.WIN = true;
                dbpl.TEAM = (byte)pl.TEAM;
                dbpl.KILLSUM = pl.KILLSUM;
                dbpl.INCOME = (int)pl.INCOME;
                dbpl.PDURATION = (int)TimeSpan.FromSeconds(pl.PDURATION / 22.4).TotalSeconds;
                dbpl.ARMY = pl.ARMY;
                dbpl.GAS = (byte)pl.GAS;
                dsplayer opp = rep.GetOpp(pl.REALPOS);
                if (opp != null)
                    dbpl.OPPRACE = opp.RACE;
                dbpl.DSReplay = dbrep;
                pls.Add(dbpl);

                List<DSUnit> units = new List<DSUnit>();
                foreach (var bp in pl.UNITS.Keys)
                {
                    foreach (var name in pl.UNITS[bp].Keys)
                    {
                        DSUnit dbunit = new DSUnit();
                        dbunit.BP = bp;
                        dbunit.Name = name;
                        dbunit.Count = pl.UNITS[bp][name];
                        dbunit.DSPlayer = dbpl;
                        units.Add(dbunit);
                    }
                }
                dbpl.DSUnit = units as ICollection<DSUnit>;
            }
            dbrep.DSPlayer = pls as ICollection<DSPlayer>;
            return dbrep;
        }
    }
}
