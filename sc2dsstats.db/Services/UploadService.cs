using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2dsstats.shared;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;

namespace sc2dsstats.db.Services
{
    public class UploadService
    {
        private ConcurrentBag<Dsreplay> Replays = new ConcurrentBag<Dsreplay>();
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<UploadService> logger;

        public UploadService(IServiceScopeFactory scopeFactory, ILogger<UploadService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        public async Task<List<Dsreplay>> Decompress(MemoryStream stream)
        {
            List<Dsreplay> replays = new List<Dsreplay>();
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
                            var replay = JsonSerializer.Deserialize<Dsreplay>(line, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                            if (replay != null)
                                replays.Add(replay);
                        }
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return replays.OrderByDescending(o => o.Gametime).ToList();
        }

        public async Task InsertReplays(List<Dsreplay> dsreplays, List<string> ids, EventWaitHandle ewh = null)
        {
            dsreplays.SelectMany(s => s.Dsplayers).Where(x => ids.Contains(x.Name)).ToList().ForEach(f => f.isPlayer = true);
            await InsertReplays(dsreplays, "", ewh);
        }

        public async Task InsertReplays(List<Dsreplay> dsreplays, string id, EventWaitHandle ewh = null)
        {
            if (!String.IsNullOrEmpty(id))
            {
                dsreplays.ForEach(f => f.Id = 0);
                dsreplays.SelectMany(s => s.Middles).ToList().ForEach(f => f.Id = 0);
                dsreplays.SelectMany(s => s.Dsplayers).ToList().ForEach(f => f.Id = 0);
                dsreplays.SelectMany(s => s.Dsplayers).SelectMany(s => s.Breakpoints).ToList().ForEach(f => f.Id = 0);
                dsreplays.SelectMany(s => s.Dsplayers).SelectMany(s => s.Breakpoints).SelectMany(s => s.Dsunits).ToList().ForEach(f => f.Id = 0);
                dsreplays.SelectMany(s => s.Dsplayers).Where(x => x.Name == "player").ToList().ForEach(f => { f.Name = id; f.isPlayer = true; });
            }
            ReplayFilter.SetDefaultFilter(dsreplays);

            foreach (var rep in dsreplays)
                Replays.Add(rep);

            await InsertJob(ewh);
        }

        public async Task InsertJob(EventWaitHandle ewh)
        {
            await semaphoreSlim.WaitAsync();

            int replayCount = 0;
            int dupCount = 0;
            try
            {
                Dsreplay replay;
                Replays.TryTake(out replay);

                using (var scope = scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                    int i = 0;
                    while (replay != null)
                    {
                        replayCount++;
                        if (!CheckDuplicate(context, replay))
                        {
                            replay.Upload = DateTime.UtcNow;
                            if (replay.DefaultFilter)
                                await context.DsTimeResultValues.AddRangeAsync(GetTimeResultValues(replay));
                            context.Dsreplays.Add(replay);
                        }
                        else
                            dupCount++;
                        Replays.TryTake(out replay);
                        i++;
                        if (i % 1000 == 0)
                            await context.SaveChangesAsync();
                    }
                    await context.SaveChangesAsync();
                    if (ewh != null)
                        ewh.Set();
                    //logger.LogInformation($"{DateTime.UtcNow} Starting CollectTimeResults2");
                    //await Task.Run(() => CollectTimeResults2(context));
                    //logger.LogInformation($"{DateTime.UtcNow} CollectTimeResults2 finished.");
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

        private bool CheckDuplicate(sc2dsstatsContext context, Dsreplay replay)
        {
            var lDupReplays = Replays.Where(f => f.Hash == replay.Hash);
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
                                if (lpl != null && lpl.Name.Length != 64)
                                    lpl.Name = pl.Name;
                            }
                        }
                    }
                    if (dbDupReplay != null)
                    {
                        var dbpls = context.Dsplayers.Where(x => x.DsreplayId == dbDupReplay.Id);
                        foreach (var pl in pls)
                        {
                            var dbpl = dbpls.FirstOrDefault(f => f.Realpos == pl.Realpos);
                            if (dbpl != null && dbpl.Name.Length != 64)
                                dbpl.Name = pl.Name;
                        }
                    }
                }
                return true;
            }
        }

        public static List<DsTimeResultValue> GetTimeResultValues(Dsreplay replay)
        {
            List<DsTimeResultValue> results = new List<DsTimeResultValue>();

            foreach (var pl in replay.Dsplayers)
            {
                var result = new DsTimeResultValue()
                {
                    Player = pl.isPlayer,
                    Gametime = replay.Gametime,
                    Cmdr = ((DSData.Commander)pl.Race).ToString(),
                    Opp = ((DSData.Commander)pl.Opprace).ToString(),
                    Win = pl.Win,
                    MVP = pl.Killsum == replay.Maxkillsum,
                    Duration = replay.Duration,
                    Kills = pl.Killsum,
                    Army = pl.Army,
                };

                result.Teammates = GetTeammates(replay, pl.Realpos).Select(s => new DsParticipantsValue()
                {
                    Cmdr = ((DSData.Commander)s.Race).ToString(),
                    Win = pl.Win,
                    // TeammateResult = result
                }).ToList();

                result.Opponents = GetOpponents(replay, pl.Realpos).Select(s => new DsParticipantsValue()
                {
                    Cmdr = ((DSData.Commander)s.Race).ToString(),
                    Win = pl.Win,
                    // OpponentResult = result
                }).ToList();

                results.Add(result);
            }
            return results;
        }

        private static List<Dsplayer> GetTeammates(Dsreplay replay, int pos)
        {
            if (pos <= 3)
            {
                return replay.Dsplayers.Where(x => x.Realpos != pos && x.Realpos <= 3).ToList();
            }
            else if (pos > 3)
            {
                return replay.Dsplayers.Where(x => x.Realpos > 3).ToList();
            }
            else
            {
                return new List<Dsplayer>();
            }
        }

        private static List<Dsplayer> GetOpponents(Dsreplay replay, int pos)
        {
            if (pos > 3)
            {
                return replay.Dsplayers.Where(x => x.Realpos <= 3).ToList();
            }
            else if (pos <= 3)
            {
                return replay.Dsplayers.Where(x => x.Realpos > 3).ToList();
            }
            else
            {
                return new List<Dsplayer>();
            }
        }

        public void CollectTimeResults(sc2dsstatsContext context)
        {
            var resultvalues = context.DsTimeResultValues
                .Include(i => i.Opponents)
                .Include(i => i.Teammates)
                .ToList();

            var results = context.DsTimeResults
                .Include(i => i.Opponents)
                .Include(i => i.Teammates)
                .ToList();

            int i = 0;
            foreach (var resultvalue in resultvalues)
            {
                string monthstring = resultvalue.Gametime.ToString("yyyyMM");
                string yearstring = resultvalue.Gametime.ToString("yyyy");
                List<string> timestrings = new List<string>() { monthstring, yearstring };
                foreach (var time in timestrings)
                {
                    var result = results.SingleOrDefault(x => x.Timespan == time && x.Cmdr == resultvalue.Cmdr && x.Opp == String.Empty && x.Player == false);
                    if (result == null)
                    {
                        result = new DsTimeResult()
                        {
                            Player = false,
                            Timespan = time,
                            Cmdr = resultvalue.Cmdr,
                            Opp = String.Empty,
                            Teammates = new HashSet<DsParticipant>(),
                            Opponents = new HashSet<DsParticipant>()
                        };
                        results.Add(result);
                        context.DsTimeResults.Add(result);
                    }

                    var vsresult = results.SingleOrDefault(x => x.Timespan == time && x.Cmdr == resultvalue.Cmdr && x.Opp == resultvalue.Opp && x.Player == false);
                    if (vsresult == null)
                    {
                        vsresult = new DsTimeResult()
                        {
                            Player = false,
                            Timespan = time,
                            Cmdr = resultvalue.Cmdr,
                            Opp = resultvalue.Opp,
                            Teammates = new HashSet<DsParticipant>(),
                            Opponents = new HashSet<DsParticipant>()
                        };
                        results.Add(vsresult);
                        context.DsTimeResults.Add(vsresult);
                    }

                    List<DsTimeResult> timeresults = new List<DsTimeResult>() { result, vsresult };

                    if (resultvalue.Player)
                    {
                        var plresult = results.SingleOrDefault(x => x.Timespan == time && x.Cmdr == resultvalue.Cmdr && x.Opp == String.Empty && x.Player == true);
                        if (plresult == null)
                        {
                            plresult = new DsTimeResult()
                            {
                                Player = true,
                                Timespan = time,
                                Cmdr = resultvalue.Cmdr,
                                Opp = String.Empty,
                                Teammates = new HashSet<DsParticipant>(),
                                Opponents = new HashSet<DsParticipant>()
                            };
                            results.Add(plresult);
                            context.DsTimeResults.Add(plresult);
                        }
                        timeresults.Add(plresult);

                        var plvsresult = results.SingleOrDefault(x => x.Timespan == time && x.Cmdr == resultvalue.Cmdr && x.Opp == resultvalue.Opp && x.Player == true);
                        if (plvsresult == null)
                        {
                            plvsresult = new DsTimeResult()
                            {
                                Player = true,
                                Timespan = time,
                                Cmdr = resultvalue.Cmdr,
                                Opp = resultvalue.Opp,
                                Teammates = new HashSet<DsParticipant>(),
                                Opponents = new HashSet<DsParticipant>()
                            };
                            results.Add(plvsresult);
                            context.DsTimeResults.Add(plvsresult);
                        }
                        timeresults.Add(plvsresult);
                    }

                    foreach (var tresult in timeresults)
                    {
                        ApplyResultChanges(resultvalue, tresult);
                    }
                }
                context.DsTimeResultValues.Remove(resultvalue);

                i++;
                if (i % 1000 == 0)
                {
                    logger.LogInformation($"DsTimeResults collected: {i}/{resultvalues.Count}");
                    context.SaveChanges();
                }
            }

            context.SaveChanges();

        }

        private void ApplyResultChanges(DsTimeResultValue resultvalue, DsTimeResult result)
        {
            result.Count++;
            if (resultvalue.Win)
                result.Wins++;
            if (resultvalue.MVP)
                result.MVP++;
            result.Duration += resultvalue.Duration;
            result.Kills += resultvalue.Kills;
            result.Army += resultvalue.Army;

            foreach (var teammate in resultvalue.Teammates)
            {
                var rteammate = result.Teammates.FirstOrDefault(f => f.Cmdr == teammate.Cmdr);
                if (rteammate == null)
                {
                    rteammate = new DsParticipant()
                    {
                        Cmdr = teammate.Cmdr
                    };
                    result.Teammates.Add(rteammate);
                }
                rteammate.Count++;
                if (teammate.Win)
                    rteammate.Wins++;
            }

            foreach (var opponent in resultvalue.Opponents)
            {
                var ropponent = result.Opponents.FirstOrDefault(f => f.Cmdr == opponent.Cmdr);
                if (ropponent == null)
                {
                    ropponent = new DsParticipant()
                    {
                        Cmdr = opponent.Cmdr
                    };
                    result.Opponents.Add(ropponent);
                }
                ropponent.Count++;
                if (opponent.Win)
                    ropponent.Wins++;
            }
        }

        public static void CollectTimeResults2(sc2dsstatsContext context, ILogger logger, List<DsTimeResultValue> resultvalues = null)
        {
            DateTime lastDate = new DateTime(2018, 1, 1);

            if (resultvalues == null)
            {
                resultvalues = context.DsTimeResultValues
                    .Include(i => i.Opponents)
                    .Include(i => i.Teammates)
                    .OrderBy(o => o.Gametime)
                    .AsSplitQuery()
                    .ToList();

                if (!resultvalues.Any())
                    return;

                context.DsParticipantsValues.RemoveRange(resultvalues.SelectMany(s => s.Opponents));
                context.DsParticipantsValues.RemoveRange(resultvalues.SelectMany(s => s.Teammates));
                context.DsTimeResultValues.RemoveRange(resultvalues);
                lastDate = resultvalues.OrderBy(o => o.Gametime).First().Gametime.AddMonths(-1);
            }

            if (!resultvalues.Any())
                return;

            //var results = context.DsTimeResults
            //    .Include(i => i.Opponents)
            //    .Include(i => i.Teammates)
            //    .AsSplitQuery()
            //    .ToList();

            // first day next month
            DateTime dateTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)).AddDays(1);
            string yearString = String.Empty;


            while (dateTime > lastDate)
            {
                DateTime _dateTime = dateTime.AddMonths(-1);
                string _yearString = _dateTime.ToString("yyyy");
                string _monthString = _dateTime.ToString("yyyyMM");
                bool doYear = false;
                if (yearString != _yearString)
                {
                    doYear = true;
                    yearString = _yearString;
                }

                logger.LogInformation($"Collecting timeresults {_monthString} ({_dateTime.ToString("yyyyMMdd")}-{dateTime.ToString("yyyyMMdd")})");

                var timeValues = resultvalues.Where(x => x.Gametime >= _dateTime && x.Gametime < dateTime).ToList();

                foreach (var cmdr in DSData.cmdrs)
                {
                    var cmdrValues = timeValues.Where(x => x.Cmdr == cmdr).ToList();
                    var cmdrPlValues = timeValues.Where(x => x.Player && x.Cmdr == cmdr).ToList();

                    // var allResult = results.SingleOrDefault(s => s.Player == false && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == String.Empty);
                    var allResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates)
                        .SingleOrDefault(s => s.Player == false && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == String.Empty);

                    if (allResult == null)
                    {
                        allResult = new DsTimeResult()
                        {
                            Player = false,
                            Timespan = _monthString,
                            Cmdr = cmdr,
                            Opp = String.Empty,
                            Teammates = new HashSet<DsParticipant>(),
                            Opponents = new HashSet<DsParticipant>()
                        };
                        // results.Add(allResult);
                        context.DsTimeResults.Add(allResult);
                    }
                    ApplyChanges(allResult, cmdrValues);


                    // var allPlResult = results.SingleOrDefault(s => s.Player == true && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == String.Empty);
                    var allPlResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates)
                        .SingleOrDefault(s => s.Player == true && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == String.Empty);
                    if (allPlResult == null)
                    {
                        allPlResult = new DsTimeResult()
                        {
                            Player = true,
                            Timespan = _monthString,
                            Cmdr = cmdr,
                            Opp = String.Empty,
                            Teammates = new HashSet<DsParticipant>(),
                            Opponents = new HashSet<DsParticipant>()
                        };
                        // results.Add(allPlResult);
                        context.DsTimeResults.Add(allPlResult);
                    }
                    ApplyChanges(allPlResult, cmdrPlValues);

                    List<DsTimeResultValue> yearValues = new List<DsTimeResultValue>();
                    List<DsTimeResultValue> cmdrYearValues = new List<DsTimeResultValue>();
                    List<DsTimeResultValue> cmdrPlYearValues = new List<DsTimeResultValue>();
                    if (doYear)
                    {
                        yearValues = resultvalues.Where(x => x.Gametime >= new DateTime(_dateTime.Year, 1, 1) && x.Gametime < new DateTime(_dateTime.Year + 1, 1, 1)).ToList();
                        cmdrYearValues = yearValues.Where(x => x.Cmdr == cmdr).ToList();
                        cmdrPlYearValues = yearValues.Where(x => x.Player && x.Cmdr == cmdr).ToList();

                        // var allYearResult = results.SingleOrDefault(s => s.Player == false && s.Timespan == _yearString && s.Cmdr == cmdr && s.Opp == String.Empty);
                        var allYearResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates)
                        .SingleOrDefault(s => s.Player == false && s.Timespan == _yearString && s.Cmdr == cmdr && s.Opp == String.Empty);

                        if (allYearResult == null)
                        {
                            allYearResult = new DsTimeResult()
                            {
                                Player = false,
                                Timespan = _yearString,
                                Cmdr = cmdr,
                                Opp = String.Empty,
                                Teammates = new HashSet<DsParticipant>(),
                                Opponents = new HashSet<DsParticipant>()
                            };
                            // results.Add(allYearResult);
                            context.DsTimeResults.Add(allYearResult);
                        }
                        ApplyChanges(allYearResult, cmdrYearValues);


                        // var allPlYearResult = results.SingleOrDefault(s => s.Player == true && s.Timespan == _yearString && s.Cmdr == cmdr && s.Opp == String.Empty);
                        var allPlYearResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates)
                        .SingleOrDefault(s => s.Player == true && s.Timespan == _yearString && s.Cmdr == cmdr && s.Opp == String.Empty);

                        if (allPlYearResult == null)
                        {
                            allPlYearResult = new DsTimeResult()
                            {
                                Player = true,
                                Timespan = _yearString,
                                Cmdr = cmdr,
                                Opp = String.Empty,
                                Teammates = new HashSet<DsParticipant>(),
                                Opponents = new HashSet<DsParticipant>()
                            };
                            // results.Add(allPlYearResult);
                            context.DsTimeResults.Add(allPlYearResult);
                        }
                        ApplyChanges(allPlYearResult, cmdrPlYearValues);
                    }

                    foreach (var vs in DSData.cmdrs)
                    {
                        var cmdrVsValues = cmdrValues.Where(x => x.Opp == vs).ToList();
                        var cmdrPlVsValues = cmdrPlValues.Where(x => x.Opp == vs).ToList();

                        // var allVsResult = results.SingleOrDefault(s => s.Player == false && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == vs);
                        var allVsResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates).SingleOrDefault(s => s.Player == false && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == vs);
                        if (allVsResult == null)
                        {
                            allVsResult = new DsTimeResult()
                            {
                                Player = false,
                                Timespan = _monthString,
                                Cmdr = cmdr,
                                Opp = vs,
                                Teammates = new HashSet<DsParticipant>(),
                                Opponents = new HashSet<DsParticipant>()
                            };
                            // results.Add(allVsResult);
                            context.DsTimeResults.Add(allVsResult);
                        }
                        ApplyChanges(allVsResult, cmdrVsValues);


                        var allPlVsResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates).SingleOrDefault(s => s.Player == true && s.Timespan == _monthString && s.Cmdr == cmdr && s.Opp == vs);
                        if (allPlVsResult == null)
                        {
                            allPlVsResult = new DsTimeResult()
                            {
                                Player = true,
                                Timespan = _monthString,
                                Cmdr = cmdr,
                                Opp = vs,
                                Teammates = new HashSet<DsParticipant>(),
                                Opponents = new HashSet<DsParticipant>()
                            };
                            // results.Add(allPlVsResult);
                            context.DsTimeResults.Add(allPlVsResult);
                        }
                        ApplyChanges(allPlVsResult, cmdrPlVsValues);

                        if (doYear)
                        {
                            var cmdrYearVsValues = cmdrYearValues.Where(x => x.Opp == vs).ToList();
                            var cmdrPlYearVsValues = cmdrPlYearValues.Where(x => x.Opp == vs).ToList();

                            var allYearVsResult = context.DsTimeResults
                            .Include(i => i.Opponents)
                            .Include(i => i.Teammates).SingleOrDefault(s => s.Player == false && s.Timespan == _yearString && s.Cmdr == cmdr && s.Opp == vs);
                            if (allYearVsResult == null)
                            {
                                allYearVsResult = new DsTimeResult()
                                {
                                    Player = false,
                                    Timespan = _yearString,
                                    Cmdr = cmdr,
                                    Opp = vs,
                                    Teammates = new HashSet<DsParticipant>(),
                                    Opponents = new HashSet<DsParticipant>()
                                };
                                // results.Add(allYearVsResult);
                                context.DsTimeResults.Add(allYearVsResult);
                            }
                            ApplyChanges(allYearVsResult, cmdrYearVsValues);


                            var allPlYearVsResult = context.DsTimeResults
                        .Include(i => i.Opponents)
                        .Include(i => i.Teammates).SingleOrDefault(s => s.Player == true && s.Timespan == _yearString && s.Cmdr == cmdr && s.Opp == vs);
                            if (allPlYearVsResult == null)
                            {
                                allPlYearVsResult = new DsTimeResult()
                                {
                                    Player = true,
                                    Timespan = _yearString,
                                    Cmdr = cmdr,
                                    Opp = vs,
                                    Teammates = new HashSet<DsParticipant>(),
                                    Opponents = new HashSet<DsParticipant>()
                                };
                                // results.Add(allPlYearVsResult);
                                context.DsTimeResults.Add(allPlYearVsResult);
                            }
                            ApplyChanges(allPlYearVsResult, cmdrYearVsValues);
                        }
                    }
                }
                dateTime = _dateTime;
            }
            context.SaveChanges();
        }

        private static void ApplyChanges(DsTimeResult result, List<DsTimeResultValue> values)
        {
            if (!values.Any())
                return;
            result.Count += values.Count;
            result.Wins += values.Where(x => x.Win == true).Count();
            result.MVP += values.Where(x => x.MVP == true).Count();
            result.Duration += values.Sum(s => s.Duration);
            result.Kills += values.Sum(s => s.Kills);
            result.Army += values.Sum(s => s.Army);

            foreach (var cmdr in DSData.cmdrs)
            {
                var teammates = values.SelectMany(s => s.Teammates).Where(x => x.Cmdr == cmdr).ToList();
                var opponents = values.SelectMany(s => s.Opponents).Where(x => x.Cmdr == cmdr).ToList();

                var teammateParticipant = result.Teammates.SingleOrDefault(f => f.Cmdr == cmdr);
                if (teammateParticipant == null)
                {
                    teammateParticipant = new DsParticipant()
                    {
                        Cmdr = cmdr
                    };
                    result.Teammates.Add(teammateParticipant);
                }
                if (teammates.Any())
                {
                    teammateParticipant.Count += teammates.Count;
                    teammateParticipant.Wins += teammates.Where(x => x.Win == true).Count();
                }
                var opponentParticipant = result.Opponents.SingleOrDefault(s => s.Cmdr == cmdr);
                if (opponentParticipant == null)
                {
                    opponentParticipant = new DsParticipant()
                    {
                        Cmdr = cmdr
                    };
                    result.Opponents.Add(opponentParticipant);
                }

                if (opponents.Any())
                {
                    opponentParticipant.Count += opponents.Count;
                    opponentParticipant.Wins += opponents.Where(x => x.Win == true).Count();
                }
            }

            foreach (var teammate in values.SelectMany(s => s.Teammates))
            {
                var rteammate = result.Teammates.FirstOrDefault(f => f.Cmdr == teammate.Cmdr);
                if (rteammate == null)
                {
                    rteammate = new DsParticipant()
                    {
                        Cmdr = teammate.Cmdr
                    };
                    result.Teammates.Add(rteammate);
                }
                rteammate.Count++;
                if (teammate.Win)
                    rteammate.Wins++;
            }

            foreach (var opponent in values.SelectMany(s => s.Opponents))
            {
                var ropponent = result.Opponents.FirstOrDefault(f => f.Cmdr == opponent.Cmdr);
                if (ropponent == null)
                {
                    ropponent = new DsParticipant()
                    {
                        Cmdr = opponent.Cmdr
                    };
                    result.Opponents.Add(ropponent);
                }
                ropponent.Count++;
                if (opponent.Win)
                    ropponent.Wins++;
            }
        }

        public void ResetDsTimeResult()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<sc2dsstatsContext>();
                var replays = ReplayFilter.DefaultFilter(context);
                replays = replays.Where(x => x.Gametime >= new DateTime(2018, 1, 1));
                var results = replays
                    .Include(i => i.Dsplayers)
                    .Select(s => GetTimeResultValues(s)).ToList();

                // context.DsTimeResultValues.AddRange(results.SelectMany(s => s));


                var timeresults = context.DsTimeResults.ToList();
                timeresults.ForEach(f => { f.Count = 0; f.Wins = 0; f.MVP = 0; f.Duration = 0; f.Kills = 0; f.Army = 0; });
                context.SaveChanges();

                var participants = context.Participants.ToList();
                participants.ForEach(f => { f.Count = 0; f.Wins = 0; });
                context.SaveChanges();

                CollectTimeResults2(context, logger, results.SelectMany(s => s).ToList());
            }
        }
    }
}
