using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sc2dsstats.lib.Db
{
    public class DBReplayFilter
    {
        public static IQueryable<DSReplay> Filter(DSoptions options, DSReplayContext context, bool isFiltered = false, IQueryable<DSReplay> ireps = null)
        {
            var replays = ireps;
            if (replays == null)
                replays = context.DSReplays
                    .Include(p => p.DSPlayer);




            HashSet<string> Gamemodes = options.Gamemodes.Where(x => x.Value == true).Select(y => y.Key).ToHashSet();

            IQueryable<DSReplay> filReplays;
            if (isFiltered == false)
            {
                if (String.IsNullOrEmpty(options.Interest))
                    if (options.Leaver > 0)
                        filReplays = replays
                                .Where(x => x.DURATION > options.Duration)
                                .Where(x => x.MAXLEAVER < options.Leaver)
                                .Where(x => x.MINARMY > options.Army)
                                .Where(x => x.MININCOME > options.Income)
                                .Where(x => x.MINKILLSUM > options.Kills)
                                .Where(x => x.PLAYERCOUNT >= options.PlayerCount)
                                .Where(x => Gamemodes.Contains(x.GAMEMODE))
                                //.ToArray()
                                ;
                    else
                        filReplays = replays
                                .Where(x => x.DURATION > options.Duration)
                                .Where(x => x.MINARMY > options.Army)
                                .Where(x => x.MININCOME > options.Income)
                                .Where(x => x.MINKILLSUM > options.Kills)
                                .Where(x => x.PLAYERCOUNT >= options.PlayerCount)
                                .Where(x => Gamemodes.Contains(x.GAMEMODE))
                                //.ToArray()
                                ;
                else
                    if (options.Leaver > 0)
                    filReplays = replays
                            .Where(x => x.DURATION > options.Duration)
                            .Where(x => x.MAXLEAVER < options.Leaver)
                            .Where(x => x.MINARMY > options.Army)
                            .Where(x => x.MININCOME > options.Income)
                            .Where(x => x.MINKILLSUM > options.Kills)
                            .Where(x => x.PLAYERCOUNT >= options.PlayerCount)
                            .Where(x => Gamemodes.Contains(x.GAMEMODE))
                            .Where(x => x.DSPlayer.FirstOrDefault(s => s.RACE == options.Interest) != null)
                            //.ToArray()
                            ;
                else
                    filReplays = replays
                            .Where(x => x.DURATION > options.Duration)
                            .Where(x => x.MINARMY > options.Army)
                            .Where(x => x.MININCOME > options.Income)
                            .Where(x => x.MINKILLSUM > options.Kills)
                            .Where(x => x.PLAYERCOUNT >= options.PlayerCount)
                            .Where(x => Gamemodes.Contains(x.GAMEMODE))
                            .Where(x => x.DSPlayer.FirstOrDefault(s => s.RACE == options.Interest) != null)
                            //.ToArray()
                            ;
            }
            else
                filReplays = replays;

            if (options.Startdate != default(DateTime))
                filReplays = filReplays.Where(x => x.GAMETIME >= options.Startdate);
            if (options.Enddate != default(DateTime))
                filReplays = filReplays.Where(x => x.GAMETIME <= options.Enddate.AddDays(1));

            if (options.Dataset.Any() && DSdata.IsMySQL)
            {
                filReplays = filReplays
                    .Where(x => x.DSPlayer.Select(s => s.NAME).Contains(options.Dataset.First()));
            }
            return filReplays;
        }
    }
}
