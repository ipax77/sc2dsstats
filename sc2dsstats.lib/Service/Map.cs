using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sc2dsstats.lib.Service
{
    public class Map
    {
        // WebDatabase Replay
        // LocalDatabase Replay
        // DecodedReplay
        // OldReplay
        public static DSReplay Rep(dsreplay rep, string id = "player")
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
            dbrep.PLAYERCOUNT = (byte)rep.PLAYERCOUNT;
            dbrep.REPORTED = (byte)rep.REPORTED;
            dbrep.ISBRAWL = rep.ISBRAWL;
            dbrep.GAMEMODE = rep.GAMEMODE;
            dbrep.VERSION = rep.VERSION;
            dbrep.MAXLEAVER = 0;

            List<DSPlayer> pls = new List<DSPlayer>();
            foreach (dsplayer pl in rep.PLAYERS)
            {
                DSPlayer dbpl = new DSPlayer();
                dbpl.POS = (byte)pl.POS;
                dbpl.REALPOS = (byte)pl.REALPOS;
                if (pl.NAME == "player")
                    dbpl.NAME = id;
                else
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
                int diff = dbrep.DURATION - dbpl.PDURATION;
                if (diff > dbrep.MAXLEAVER)
                    dbrep.MAXLEAVER = diff;

                dbpl.DSReplay = dbrep;
                pls.Add(dbpl);

                List<DbBreakpoint> bps = new List<DbBreakpoint>();
                foreach (var bp in pl.UNITS.Keys)
                {
                    DbBreakpoint dbbp = new DbBreakpoint();
                    dbbp.Breakpoint = bp;
                    dbbp.dsUnitsString = "";

                    foreach (var name in pl.UNITS[bp].Keys)
                    {
                        if (name == "Gas")
                            dbbp.Gas = pl.UNITS[bp][name];
                        else if (name == "Upgrades")
                            dbbp.Upgrades = pl.UNITS[bp][name];
                        else if (name == "Mid")
                            dbbp.Mid = pl.UNITS[bp][name];
                        else
                        {
                            UnitModelBase unit = DSdata.Units.FirstOrDefault(f => f.Race == pl.RACE && f.Name == Fix.UnitName(name));
                            if (unit != null)
                                dbbp.dsUnitsString += unit.ID + "," + pl.UNITS[bp][name] + "|";
                            else
                                dbbp.dsUnitsString += name + "," + pl.UNITS[bp][name] + "|";
                        }
                    }
                    if (!String.IsNullOrEmpty(dbbp.dsUnitsString))
                        dbbp.dsUnitsString = dbbp.dsUnitsString.Remove(dbbp.dsUnitsString.Length - 1);
                    bps.Add(dbbp);
                }
                dbpl.Breakpoints = bps;
            }
            dbrep.DSPlayer = pls as ICollection<DSPlayer>;
            dbrep.HASH = dbrep.GenHash();
            dbrep.Upload = DateTime.UtcNow;
            return dbrep;
        }


    }

}

