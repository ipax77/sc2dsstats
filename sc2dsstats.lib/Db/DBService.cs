using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Models;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using sc2dsstats.lib.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace sc2dsstats.lib.Db
{
    public class DBService
    {
        public object lockobject = new object();
        public static object stlockobject = new object();
        private DSReplayContext _context;
        private ILogger _logger;

        public DBService(DSReplayContext context, ILogger<DBService> logger)
        {
            _context = context;
            _logger = logger;
        }

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

        public void DeleteRep(int id, bool bulk = false)
        {
            _logger.LogInformation("Deleting rep " + id); 
            lock (lockobject)
            {
                var replay = _context.DSReplays
                    .Include(o => o.Middle)
                    .Include(p => p.DSPlayer)
                    .ThenInclude(q => q.Breakpoints)
                    .FirstOrDefault(s => s.ID == id);

                if (replay.DSPlayer != null)
                {
                    foreach (DSPlayer pl in replay.DSPlayer)
                    {
                        if (pl.Breakpoints != null)
                        {
                            foreach (DbBreakpoint bp in pl.Breakpoints)
                                _context.Breakpoints.Remove(bp);
                        }
                        _context.DSPlayers.Remove(pl);
                    }
                }
                if (replay.Middle != null)
                    foreach (DbMiddle mid in replay.Middle)
                        _context.Middle.Remove(mid);
                _context.DSReplays.Remove(replay);
                if (bulk == false)
                    _context.SaveChanges();
            }
        }

        public static void DeleteRep(DSReplayContext stcontext, int id, bool bulk = false)
        {
            lock (stlockobject)
            {
                var replay = stcontext.DSReplays
                    .Include(o => o.Middle)
                    .Include(p => p.DSPlayer)
                    .ThenInclude(q => q.Breakpoints)
                    .FirstOrDefault(s => s.ID == id);

                if (replay.DSPlayer != null)
                {
                    foreach (DSPlayer pl in replay.DSPlayer)
                    {
                        if (pl.Breakpoints != null)
                        {
                            foreach (DbBreakpoint bp in pl.Breakpoints)
                                stcontext.Breakpoints.Remove(bp);
                        }
                        stcontext.DSPlayers.Remove(pl);
                    }
                }
                if (replay.Middle != null)
                    foreach (DbMiddle mid in replay.Middle)
                        stcontext.Middle.Remove(mid);
                stcontext.DSReplays.Remove(replay);
                if (bulk == false)
                    stcontext.SaveChanges();
            }
        }

        public DSReplay GetReplay(int id)
        {
            _logger.LogInformation("Getting Replay " + id);
            lock (lockobject)
            {
                return _context.DSReplays
                    .Include(p => p.Middle)
                    .Include(p => p.DSPlayer)
                    .ThenInclude(q => q.Breakpoints)
                    .FirstOrDefault(x => x.ID == id);
            }
        }

        public void SaveReplay(DSReplay rep, bool bulk = false)
        {
            _logger.LogInformation("Saveing repls " + rep.REPLAYPATH);
            lock (lockobject)
            {
                _context.DSReplays.Add(rep);
                if (bulk == false)
                    _context.SaveChanges();
            }
        }

        public static void SaveReplay(DSReplayContext stcontext, DSReplay rep, bool bulk = false)
        {
            lock (stlockobject)
            {
                stcontext.DSReplays.Add(rep);
                if (bulk == false)
                    stcontext.SaveChanges();
            }
        }

        public void SaveContext()
        {
            _logger.LogInformation("Saveing context.");
            lock (lockobject)
            {
                _context.SaveChanges();
            }
        }

        public HashSet<string> GetReplayHashes()
        {
            _logger.LogInformation("Getting Replay Hashes.");
            lock (lockobject)
            {
                return _context.DSReplays.Select(s => s.REPLAY).ToHashSet();
            }
        }
        
        public int GetReplayCount()
        {
            _logger.LogInformation("Getting replay count");
            lock (lockobject)
            {
                return _context.DSReplays.Count();
            }
        }

        public DSReplay GetLatestReplay()
        {
            _logger.LogInformation("Getting latest replay");
            lock (lockobject)
            {
                return _context.DSReplays.Include(m => m.Middle).Include(p => p.DSPlayer).ThenInclude(b => b.Breakpoints).OrderByDescending(o => o.GAMETIME).FirstOrDefault();
            }
        }

        public DSReplay GetReplayFromREPLAY(string replay)
        {
            _logger.LogInformation("Gettin replay from REPLAY " + replay);
            lock (lockobject)
            {
                return _context.DSReplays.FirstOrDefault(s => s.REPLAY == "2c16ad60406ab0c2765cae46f0ab72ffdd513d5ade7a5e60876c166b7ba3b94a");
            }
        }

        public List<DSReplay> GetReplays(int skip, int take, string order)
        {
            lock (lockobject) {
                var reps = (order switch { 
                    "GAMETIME" => _context.DSReplays.OrderByDescending(o => o.GAMETIME).Skip(skip).Take(take).ToList(),

                    _ => new List<DSReplay>()
                });
                return reps;
            }
        }

        public IQueryable<DSReplay> GetQueriableReplays()
        {
            return _context.DSReplays;
        }

        public List<DSReplay> GetReplaysPart(IQueryable<DSReplay> replays, string id, int skip, int take, bool order)
        {
            List<DSReplay> Replays = new List<DSReplay>();
            lock (lockobject) {
                if (order)
                {
                    Replays = id switch
                    {
                        "ID" => replays.OrderByDescending(o => o.ID).Skip(skip).Take(take).ToList(),
                        "REPLAY" => replays.OrderByDescending(o => o.REPLAY).Skip(skip).Take(take).ToList(),
                        "GAMETIME" => replays.OrderByDescending(o => o.GAMETIME).Skip(skip).Take(take).ToList(),
                        "WINNER" => replays.OrderByDescending(o => o.WINNER).Skip(skip).Take(take).ToList(),
                        "DURATION" => replays.OrderByDescending(o => o.DURATION).Skip(skip).Take(take).ToList(),
                        "MAXLEAVER" => replays.OrderByDescending(o => o.MAXLEAVER).Skip(skip).Take(take).ToList(),
                        "MINKILLSUM" => replays.OrderByDescending(o => o.MINKILLSUM).Skip(skip).Take(take).ToList(),
                        "MININCOME" => replays.OrderByDescending(o => o.MININCOME).Skip(skip).Take(take).ToList(),
                        "MINARMY" => replays.OrderByDescending(o => o.MINARMY).Skip(skip).Take(take).ToList(),
                        "PLAYERCOUNT" => replays.OrderByDescending(o => o.PLAYERCOUNT).Skip(skip).Take(take).ToList(),
                        "GAMEMODE" => replays.OrderByDescending(o => o.GAMEMODE).Skip(skip).Take(take).ToList(),
                        _ => replays.OrderByDescending(o => o.GAMETIME).Skip(skip).Take(take).ToList()
                    };
                }
                else
                {
                    Replays = id switch
                    {
                        "ID" => replays.OrderBy(o => o.ID).Skip(skip).Take(take).ToList(),
                        "REPLAY" => replays.OrderBy(o => o.REPLAY).Skip(skip).Take(take).ToList(),
                        "GAMETIME" => replays.OrderBy(o => o.GAMETIME).Skip(skip).Take(take).ToList(),
                        "WINNER" => replays.OrderBy(o => o.WINNER).Skip(skip).Take(take).ToList(),
                        "DURATION" => replays.OrderBy(o => o.DURATION).Skip(skip).Take(take).ToList(),
                        "MAXLEAVER" => replays.OrderBy(o => o.MAXLEAVER).Skip(skip).Take(take).ToList(),
                        "MINKILLSUM" => replays.OrderBy(o => o.MINKILLSUM).Skip(skip).Take(take).ToList(),
                        "MININCOME" => replays.OrderBy(o => o.MININCOME).Skip(skip).Take(take).ToList(),
                        "MINARMY" => replays.OrderBy(o => o.MINARMY).Skip(skip).Take(take).ToList(),
                        "PLAYERCOUNT" => replays.OrderBy(o => o.PLAYERCOUNT).Skip(skip).Take(take).ToList(),
                        "GAMEMODE" => replays.OrderBy(o => o.GAMEMODE).Skip(skip).Take(take).ToList(),
                        _ => replays.OrderBy(o => o.GAMETIME).Skip(skip).Take(take).ToList()
                    };

                }
            }
            return Replays;
        }

        public IQueryable<DSReplay> CmdrSearch(IQueryable<DSReplay> replays, DatabaseSearchOptions dbSearch)
        {
            lock (lockobject)
            {
                List<string> cmdrst1 = dbSearch.Cmdrs.Where(x => x.Value.Any() && !x.Value.StartsWith("Team") && x.Key <= 3).Select(s => s.Value).ToList();
                List<string> cmdrst2 = dbSearch.Cmdrs.Where(x => x.Value.Any() && !x.Value.StartsWith("Team") && x.Key > 3).Select(s => s.Value).ToList();

                replays = replays.Include(p => p.DSPlayer);

                if (dbSearch.anySearch)
                {
                    if (cmdrst1.Any())
                    {
                        replays = from r in replays
                                  from p in r.DSPlayer
                                  where p.REALPOS <= 3 && cmdrst1.Contains(p.RACE)
                                  select r;
                    }
                    if (cmdrst2.Any())
                    {
                        replays = from r in replays
                                  from p in r.DSPlayer
                                  where p.REALPOS > 3 && cmdrst2.Contains(p.RACE)
                                  select r;
                    }
                    replays = replays.Distinct();
                }
                else
                {
                    List<int> skip = new List<int>();
                    foreach (var ent in dbSearch.Cmdrs)
                    {
                        if (skip.Contains(ent.Key) || !ent.Value.Any() || ent.Value.StartsWith("Team"))
                            continue;

                        int opppos = DBFunctions.GetOpp(ent.Key);
                        if (dbSearch.Cmdrs.ContainsKey(opppos))
                        {
                            skip.Add(opppos);
                            replays = from r in replays
                                      from p in r.DSPlayer
                                      where p.REALPOS == ent.Key && p.RACE == ent.Value && p.OPPRACE == dbSearch.Cmdrs[opppos]
                                      select r;
                        }
                        else
                        {
                            replays = from r in replays
                                      from p in r.DSPlayer
                                      where p.REALPOS == ent.Key && p.RACE == ent.Value
                                      select r;
                        }
                    }
                }
                return replays;
            }
        }
    }



    public class DatabaseSearchOptions
    {
        public Dictionary<int, string> Cmdrs { get; set; } = new Dictionary<int, string>();
        public bool anySearch { get; set; } = true;
    }
}
