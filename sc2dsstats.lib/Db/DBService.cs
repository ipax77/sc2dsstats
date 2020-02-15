using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using sc2dsstats.decode.Models;

namespace sc2dsstats.lib.Db
{
    public static class DBService
    {
        public static DSReplay InsertdsReplay(DSReplayContext context, dsreplay rep)
        {
            Dictionary<int, PLDuplicate> dPos = new Dictionary<int, PLDuplicate>();

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
            dbrep.WINNER = (sbyte)rep.WINNER;
            dbrep.DURATION = TimeSpan.FromSeconds(rep.DURATION / 22.4);
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
            context.DSReplays.Add(dbrep);

            foreach (var dup in rep.PLDupPos)
            {
                PLDuplicate dbdup = new PLDuplicate();
                dbdup.Hash = dup.Key;
                dbdup.Pos = (byte)dup.Value;
                dbdup.DSReplay = dbrep;
                context.PLDuplicates.Add(dbdup);
                dPos[dup.Value] = dbdup;
            }

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
                dbpl.PDURATION = TimeSpan.FromSeconds(pl.PDURATION / 22.4);
                dbpl.ARMY = pl.ARMY;
                dbpl.GAS = (byte)pl.GAS;
                dsplayer opp = rep.GetOpp(pl.REALPOS);
                if (opp != null)
                    dbpl.OPPRACE = opp.RACE;
                if (dPos.ContainsKey(pl.REALPOS))
                {
                    dbpl.PLDuplicate = dPos[pl.REALPOS];
                    dbpl.NAME = dPos[pl.REALPOS].Hash;
                }
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
            context.SaveChanges();
            return dbrep;
        }

        public static int DeleteRep(DSReplayContext context, int id)
        {
            var replay = context.DSReplays
                .Include(p => p.DSPlayer)
                    .ThenInclude(p => p.DSUnit)
                .Include(p => p.PLDuplicate)
                .Single(s => s.ID == id);


            foreach (DSPlayer pl in replay.DSPlayer)
            {
                if (pl.DSUnit != null)
                    foreach (DSUnit unit in pl.DSUnit)
                        context.DSUnits.Remove(unit);
                context.DSPlayers.Remove(pl);
            }

            if (replay.PLDuplicate != null)
                foreach (PLDuplicate dup in replay.PLDuplicate)
                    context.PLDuplicates.Remove(dup);

            context.DSReplays.Remove(replay);
            context.SaveChanges();

            return id;
        }

        public static DSReplay GetReplay(int id)
        {
            using (var context = new DSReplayContext()) {
                return context.DSReplays
                    .Include(p => p.DSPlayer)
                    .ThenInclude(q => q.DSUnit)
                    .SingleOrDefault(x => x.ID == id);

            }
        }
    }
}
