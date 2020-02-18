using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Models;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

namespace sc2dsstats.lib.Db
{
    public static class DBService
    {
        public static async Task DeleteRep(DSReplayContext context, int id)
        {
            var replay = await context.DSReplays
                .Include(p => p.DSPlayer)
                    .ThenInclude(p => p.DSUnit)
                .SingleAsync(s => s.ID == id);


            foreach (DSPlayer pl in replay.DSPlayer)
            {
                if (pl.DSUnit != null)
                    foreach (DSUnit unit in pl.DSUnit)
                        context.DSUnits.Remove(unit);
                context.DSPlayers.Remove(pl);
            }

            context.DSReplays.Remove(replay);
            await context.SaveChangesAsync();
        }

        public static async Task<DSReplay> GetReplay(DSReplayContext context, int id)
        {
            return await context.DSReplays
                .Include(p => p.DSPlayer)
                .ThenInclude(q => q.DSUnit)
                .SingleOrDefaultAsync(x => x.ID == id);
        }

        public static void SaveReplay(DSReplayContext context, DSReplay rep)
        {
            
            foreach (DSPlayer pl in rep.DSPlayer)
            {
                foreach (DSUnit unit in pl.DSUnit)
                    context.DSUnits.Add(unit);
                context.DSPlayers.Add(pl);
            }
            context.DSReplays.Add(rep);
            context.SaveChanges();
        }
    }
}
