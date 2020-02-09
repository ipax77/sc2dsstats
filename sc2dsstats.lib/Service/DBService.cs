using sc2dsstats.decode.Models;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.lib.Service
{
    public static class DBService
    {
        public static void InsertdsReplay(dsreplay rep)
        {
            using (var context = new DSReplayContext())
            {
                DSReplay dbrep = new DSReplay();
                dbrep.REPLAY = rep.REPLAY;
                string gametime = rep.GAMETIME.ToString();
                int year = int.Parse(gametime.Substring(0, 4));
                int month = int.Parse(gametime.Substring(4, 2));
                int day = int.Parse(gametime.Substring(6, 2));
                int hour = int.Parse(gametime.Substring(8, 2));
                int min = int.Parse(gametime.Substring(10, 2));
                int sec = int.Parse(gametime.Substring(12, 2));
                DateTime gtime = new DateTime(year, month, day, hour, min, sec);
                dbrep.GAMETIME = gtime;
                dbrep.WINNER = rep.WINNER;
                dbrep.DURATION = TimeSpan.FromSeconds(rep.DURATION / 22.4);
                dbrep.MINKILLSUM = rep.MINKILLSUM;
                dbrep.MAXKILLSUM = rep.MAXKILLSUM;
                dbrep.MINARMY = rep.MINARMY;
                dbrep.MININCOME = rep.MININCOME;
                dbrep.MAXLEAVER = rep.MAXLEAVER;
                dbrep.PLAYERCOUNT = rep.PLAYERCOUNT;
                dbrep.REPORTED = rep.REPORTED;
                dbrep.ISBRAWL = rep.ISBRAWL.ToString();
                dbrep.GAMEMODE = rep.GAMEMODE;
                dbrep.VERSION = rep.VERSION;
                dbrep.HASH = rep.HASH;
                context.DSReplays.Add(dbrep);

                foreach (dsplayer pl in rep.PLAYERS)
                {
                    DSPlayer dbpl = new DSPlayer();
                    dbpl.POS = pl.POS;
                    dbpl.REALPOS = pl.REALPOS;
                    dbpl.NAME = pl.NAME;
                    dbpl.RACE = pl.RACE;
                    dbpl.RESULT = pl.RESULT;
                    dbpl.TEAM = pl.TEAM;
                    dbpl.KILLSUM = pl.KILLSUM;
                    dbpl.INCOME = pl.INCOME;
                    dbpl.PDURATION = TimeSpan.FromSeconds(pl.PDURATION / 22.4);
                    dbpl.ARMY = pl.ARMY;
                    dbpl.GAS = pl.GAS;
                    dbpl.DSReplay = dbrep;
                    context.DSPlayers.Add(dbpl);

                    foreach (var bp in pl.UNITS.Keys)
                        foreach (var name in pl.UNITS[bp].Keys)
                        {
                            DSUnit dbunit = new DSUnit();
                            dbunit.BP = bp;
                            dbunit.Name = name;
                            dbunit.Count = pl.UNITS[bp][name];
                            dbunit.DSPlayer = dbpl;
                            context.DSUnits.Add(dbunit);
                        }
                }

                foreach (var dup in rep.PLDupPos)
                {
                    PLDuplicate dbdup = new PLDuplicate();
                    dbdup.Hash = dup.Key;
                    dbdup.Pos = dup.Value;
                    dbdup.DSReplay = dbrep;
                    context.PLDuplicates.Add(dbdup);
                }

                context.SaveChanges();
            }
        }
    }
}
