using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Data;
using System;
using System.Linq;

namespace sc2dsstats.test
{
    public class DBReplayFilter
    {
        public static DSReplay[] Filter(DSoptions options, DSReplayContext context, bool isFiltered = true)
        {
            var replays = context.DSReplays
                .Include(p => p.DSPlayer);

            if (isFiltered == false)
            {
                if (String.IsNullOrEmpty(options.Interest))
                    return replays
                            .Where(x => x.GAMETIME > options.Startdate)
                            .Where(x => x.GAMETIME < options.Enddate)
                            .Where(x => x.DURATION > TimeSpan.FromMinutes(5))
                            .Where(x => x.MAXLEAVER < options.Leaver)
                            .Where(x => x.MINARMY > options.Army)
                            .Where(x => x.MININCOME > options.Income)
                            .Where(x => x.MINKILLSUM > options.Kills)
                            .Where(x => x.PLAYERCOUNT == 6).ToArray();
                else
                    return replays
                            .Where(x => x.GAMETIME > options.Startdate)
                            .Where(x => x.GAMETIME < options.Enddate)
                            .Where(x => x.DURATION > TimeSpan.FromMinutes(5))
                            .Where(x => x.MAXLEAVER < options.Leaver)
                            .Where(x => x.MINARMY > options.Army)
                            .Where(x => x.MININCOME > options.Income)
                            .Where(x => x.MINKILLSUM > options.Kills)
                            .Where(x => x.PLAYERCOUNT == 6)
                            .Where(x => x.DSPlayer.FirstOrDefault(s => s.RACE == options.Interest) != null).ToArray();
            }
            else
            {
                /*
                if (String.IsNullOrEmpty(options.Interest))
                    return replays
                        .Where(x => x.GAMETIME > options.Startdate)
                        .Where(x => x.GAMETIME < options.Enddate).ToArray();
                else
                    return replays
                        .Where(x => x.GAMETIME > options.Startdate)
                        .Where(x => x.GAMETIME < options.Enddate).ToArray();
                */
                if (String.IsNullOrEmpty(options.Interest))
                    return (from dbrep in replays
                            where dbrep.GAMETIME > options.Startdate && dbrep.GAMETIME < options.Enddate
                            select dbrep).ToArray();
                else
                    return (from dbrep in replays
                            where dbrep.GAMETIME > options.Startdate
                            where dbrep.GAMETIME < options.Enddate
                            where dbrep.DSPlayer.FirstOrDefault(s => s.RACE == options.Interest) != null
                            select dbrep).ToArray();

            }
        }
    }
}
