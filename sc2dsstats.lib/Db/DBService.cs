using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Models;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using sc2dsstats.lib.Data;

namespace sc2dsstats.lib.Db
{
    public static class DBService
    {
        public static object lockobject = new object();

        public static async Task DeleteRep_bak(DSReplayContext context, int id)
        {
            var replay = await context.DSReplays
                .Include(p => p.DSPlayer)
                    .ThenInclude(p => p.Breakpoints)
                .SingleAsync(s => s.ID == id);


            foreach (DSPlayer pl in replay.DSPlayer)
            {
                if (pl.Breakpoints != null)
                    foreach (DbBreakpoint bp in pl.Breakpoints)
                        context.Breakpoints.Remove(bp);
                context.DSPlayers.Remove(pl);
            }

            context.DSReplays.Remove(replay);
            await context.SaveChangesAsync();
        }

        public static void DeleteRep(DSReplayContext context, int id, bool bulk = false)
        {
            var replay = context.DSReplays
                .Include(o => o.Middle)
                .Include(p => p.DSPlayer)
                .ThenInclude(q => q.Breakpoints)
                .FirstOrDefault(s => s.ID == id);

            if (replay.DSPlayer != null)
            {
                foreach (DSPlayer pl in replay.DSPlayer)
                {
                    if (pl.Breakpoints != null)
                        foreach (DbBreakpoint bp in pl.Breakpoints)
                            context.Breakpoints.Remove(bp);
                    context.DSPlayers.Remove(pl);
                }
            }

            context.DSReplays.Remove(replay);

            if (replay.Middle != null)
                foreach (DbMiddle mid in replay.Middle)
                    context.Middle.Remove(mid);
            if (bulk == false)
                context.SaveChanges();
        }


        public static DSReplay GetReplay(DSoptions _options, int id)
        {
            if (_options.db != null)
            {
                lock (_options.db)
                {
                    return _options.db.DSReplays
                        .Include(p => p.Middle)
                        .Include(p => p.DSPlayer)
                        .ThenInclude(q => q.Breakpoints)
                        .FirstOrDefault(x => x.ID == id);
                }
            }
            else
                return null;
        }

        public static void SaveReplay(DSReplayContext context, DSReplay rep, bool bulk = false)
        {
            context.DSReplays.Add(rep);
            if (bulk == false)
                context.SaveChanges();
        }
    }
}
