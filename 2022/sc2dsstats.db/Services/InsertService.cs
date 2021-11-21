using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Shared;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;

namespace sc2dsstats.db.Services
{
    public class InsertService
    {

        private ConcurrentBag<Dsreplay> Replays = new ConcurrentBag<Dsreplay>();
        private bool jobRunning = false;
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<InsertService> logger;
        public int NewReplaysCount { get; set; } = 0;

        public InsertService(IServiceScopeFactory scopeFactory, ILogger<InsertService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        public async Task<List<DsReplayDto>> Decompress(MemoryStream stream)
        {
            List<DsReplayDto> replays = new List<DsReplayDto>();
            try
            {
                stream.Position = 0;
                using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(decompressionStream))
                    {
                        string line;
                        while ((line = await sr.ReadLineAsync()) != null)
                        {
                            var replay = JsonSerializer.Deserialize<DsReplayDto>(line, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                            if (replay != null)
                                replays.Add(replay);
                        }
                    }

                }

            }
            catch (Exception e)
            {
                logger.LogError($"Failed decompressing: {e.Message}");
            }
            return replays.OrderByDescending(o => o.GAMETIME).ToList();
        }

        public async void InsertReplays(List<DsReplayDto> replays, DsPlayerName player, EventWaitHandle ewh = null)
        {
            var dsreplays = replays.Select(s => new Dsreplay(s)).ToList();
            dsreplays.SelectMany(s => s.Dsplayers).Where(x => x.Name == "player").ToList().ForEach(f => { f.Name = player.DbId.ToString(); f.isPlayer = true; f.PlayerName = player; });
            ReplayFilter.SetDefaultFilter(dsreplays);
            DbService.SetMid(dsreplays);

            if (!player.NamesMapped && !String.IsNullOrEmpty(player.Hash))
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    var mapReps = await context.Dsplayers.Where(x => x.Name == player.Hash).ToListAsync();
                    context.Entry(player).Reload();
                    foreach (var pl in mapReps)
                    {
                        pl.Name = player.DbId.ToString();
                        pl.PlayerName = player;
                    }
                    player.NamesMapped = true;
                    await context.SaveChangesAsync();
                }
            }

            foreach (var rep in dsreplays)
            {
                Replays.Add(rep);
            }
            _ = InsertJob(ewh);
        }

        // app only
        public void InsertReplays(List<DsReplayDto> replays, List<string> ids, EventWaitHandle ewh = null)
        {
            if (replays.Any())
                replays.SelectMany(s => s.DSPlayer).Where(x => ids.Contains(x.NAME)).ToList().ForEach(f => f.isPlayer = true);
            InsertReplays(replays, "", ewh);
        }

        public void InsertReplays(List<DsReplayDto> replays, string id, EventWaitHandle ewh = null, bool checkDuplicates = true)
        {
            var dsreplays = replays.Select(s => new Dsreplay(s)).ToList();
            if (!String.IsNullOrEmpty(id))
            {
                dsreplays.SelectMany(s => s.Dsplayers).Where(x => x.Name == "player").ToList().ForEach(f => { f.Name = id; f.isPlayer = true; });
            }
            ReplayFilter.SetDefaultFilter(dsreplays);
            DbService.SetMid(dsreplays);

            foreach (var rep in dsreplays)
                Replays.Add(rep);
            _ = InsertJob(ewh, checkDuplicates);
        }

        public async Task InsertJob(EventWaitHandle ewh, bool checkDuplicates = true)
        {
            await semaphoreSlim.WaitAsync();

            int replayCount = 0;
            int dupCount = 0;
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    logger.LogInformation($"Context scope: {context.ContextId}");
                    int i = 0;
                    Dsreplay replay;
                    while (Replays.TryTake(out replay))
                    {
                        replayCount++;

                        foreach (var pl in replay.Dsplayers.Where(x => x.PlayerName != null))
                        {
                            // await context.Entry(pl.PlayerName).ReloadAsync();
                            var player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.DbId == pl.PlayerName.DbId);
                            pl.PlayerName = player;
                        }

                        if (!CheckDuplicate(context, replay, checkDuplicates))
                        {
                            replay.Upload = DateTime.UtcNow;
                            Version replayVersion = Version.Parse(replay.Version);
                            if (replayVersion < new Version(4, 0))
                            {
                                NameService.ConvertNameStrings(context, replay);
                                foreach (var pl in replay.Dsplayers.Where(x => x.isPlayer))
                                {
                                    var player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Hash == pl.Name);
                                    if (player != null)
                                    {
                                        pl.Name = player.DbId.ToString();
                                        pl.PlayerName = player;
                                    }
                                }
                                replay.Version = "4.0";
                            }
                            context.Dsreplays.Add(replay);
                        }
                        else
                            dupCount++;
                        i++;
                        if (i % 1000 == 0)
                            await context.SaveChangesAsync();
                    }
                    await context.SaveChangesAsync();
                    NewReplaysCount += replayCount - dupCount;
                    if (ewh != null)
                        ewh.Set();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed inserting relays: {ex.Message}");
            }
            finally
            {
                semaphoreSlim.Release();
            }
            logger.LogInformation($"Replays inserted: {replayCount}, duplicates: {dupCount}");
        }

        private bool CheckDuplicate(sc2dsstatsContext context, Dsreplay replay, bool checkDuplicates = true)
        {
            if (!checkDuplicates)
            {
                return false;
            }
            var lDupReplays = Replays.Where(f => f.Hash == replay.Hash).ToList();
            var dbDupReplay = context.Dsreplays.FirstOrDefault(x => x.Hash == replay.Hash);

            if (!lDupReplays.Any() && dbDupReplay == null)
                return false;
            else
            {
                var pls = replay.Dsplayers.Where(f => f.isPlayer);
                if (pls.Any())
                {
                    if (lDupReplays.Any())
                    {
                        foreach (var lDupReplay in lDupReplays)
                        {
                            foreach (var pl in pls)
                            {
                                var lpl = lDupReplay.Dsplayers.FirstOrDefault(f => f.Realpos == pl.Realpos);
                                if (lpl != null)
                                {
                                    lpl.Name = pl.Name;
                                    lpl.isPlayer = true;
                                    lpl.PlayerName = pl.PlayerName;
                                }
                            }
                            if (dbDupReplay != null
                                && new Version(dbDupReplay.Version) <= new Version(4, 0)
                                && new Version(lDupReplay.Version) > new Version(4, 0))
                            {
                                dbDupReplay.Bunker = lDupReplay.Bunker;
                                dbDupReplay.Cannon = lDupReplay.Cannon;
                                dbDupReplay.Mid1 = lDupReplay.Mid1;
                                dbDupReplay.Mid2 = lDupReplay.Mid2;
                            }
                        }
                    }
                    if (dbDupReplay != null)
                    {
                        var dbpls = context.Dsplayers.Include(i => i.PlayerName).Where(x => x.DsreplayId == dbDupReplay.Id);
                        foreach (var pl in pls)
                        {
                            var dbpl = dbpls.FirstOrDefault(f => f.Realpos == pl.Realpos);
                            if (dbpl != null)
                            {
                                dbpl.Name = pl.Name;
                                dbpl.isPlayer = true;
                                dbpl.PlayerName = pl.PlayerName;
                            }
                        }
                        if (new Version(dbDupReplay.Version) <= new Version(4, 0)
                            && new Version(replay.Version) > new Version(4, 0))
                        {
                            dbDupReplay.Bunker = replay.Bunker;
                            dbDupReplay.Cannon = replay.Cannon;
                            dbDupReplay.Mid1 = replay.Mid1;
                            dbDupReplay.Mid2 = replay.Mid2;
                        }
                    }
                }
                return true;
            }
        }
    }
}
