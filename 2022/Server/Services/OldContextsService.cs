using Microsoft.EntityFrameworkCore;
using sc2dsstats.db;
using sc2dsstats.db.Services;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using sc2dsstats._2022.Shared;

namespace sc2dsstats._2022.Server.Services
{
    public class OldContextsService
    {
        private static bool isRunning = false;
        private static object lockobject = new object();

        public static void CopyRestPlayerData(sc2dsstatsContext context, DSRestContext restContext)
        {
            var restPlayers = restContext.DSRestPlayers.AsNoTracking().ToList();

            foreach (var restPlayer in restPlayers)
            {
                var newPlayer = context.DSRestPlayers.FirstOrDefault(f => f.Name == restPlayer.Name);

                if (newPlayer == null)
                {
                    newPlayer = new db.DSRestPlayer()
                    {
                        Name = restPlayer.Name,
                        Json = restPlayer.Json,
                        LastRep = restPlayer.LastRep,
                        LastUpload = restPlayer.LastUpload,
                        Data = restPlayer.Data,
                        Total = restPlayer.Total,
                        Version = restPlayer.Version
                    };
                    context.DSRestPlayers.Add(newPlayer);
                } else
                {
                    if (newPlayer.LastRep < restPlayer.LastRep)
                    {
                        newPlayer.LastRep = restPlayer.LastRep;
                    }
                }
            }
            context.SaveChanges();
        }

        public static int UpdateFromOldDb(sc2dsstatsContext context, DSReplayContext oldcontext, InsertService insertService, bool fullCopy = false)
        {
            lock (lockobject)
            {
                if (isRunning)
                {
                    return 0;
                }
                isRunning = true;
            }
            var restPlayer = context.DSRestPlayers.FirstOrDefault(f => f.Name == "olddb");
            if (restPlayer == null)
            {
                restPlayer = new db.DSRestPlayer()
                {
                    Name = "olddb",
                    LastUpload = new DateTime(2021, 11, 09, 00, 00, 00)
                };
                context.DSRestPlayers.Add(restPlayer);
            }

            if (fullCopy)
            {
                restPlayer = new db.DSRestPlayer()
                {
                    LastUpload = DateTime.MinValue
                };
            }

            int skip = 0;
            int take = 1000;

            var oldReplays = oldcontext.DSReplays
                .Include(i => i.Middle)
                .Include(i => i.DSPlayer)
                    .ThenInclude(j => j.Breakpoints)
                .AsNoTracking()
                .Where(x => x.Upload > restPlayer.LastUpload)
                .OrderBy(o => o.ID)
                .AsSplitQuery()
                .Take(take)
                .ToList();

            if (!oldReplays.Any())
            {
                isRunning = false;
                return 0;
            }
            DateTime latestReplay = DateTime.MinValue;

            while (oldReplays.Any())
            {
                if (!fullCopy)
                {
                    var l = oldReplays.OrderByDescending(o => o.Upload).First().Upload;
                    if (l > latestReplay)
                        latestReplay = l;
                }
                var json = JsonSerializer.Serialize(oldReplays);
                var newReplays = JsonSerializer.Deserialize<List<DsReplayDto>>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                newReplays.SelectMany(s => s.DSPlayer).Where(x => x.NAME.Length == 64).ToList().ForEach(f => f.isPlayer = true );
                Console.WriteLine($"Inserting {newReplays.Count} replays from olddb");
                EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
                insertService.InsertReplays(newReplays, "olddb", ewh, !fullCopy);
                ewh.WaitOne();
                skip += take;
                oldReplays = oldcontext.DSReplays
                .Include(i => i.Middle)
                .Include(i => i.DSPlayer)
                    .ThenInclude(j => j.Breakpoints)
                .AsNoTracking()
                .Where(x => x.Upload > restPlayer.LastUpload)
                .OrderBy(o => o.ID)
                .AsSplitQuery()
                .Skip(skip)
                .Take(take)
                .ToList();
            }
            if (!fullCopy)
            {
                restPlayer.LastUpload = latestReplay;
                context.SaveChanges();
            }
            isRunning = false;
            return insertService.NewReplaysCount;
        }
    }
}
