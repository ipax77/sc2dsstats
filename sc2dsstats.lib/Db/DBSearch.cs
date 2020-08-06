using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace sc2dsstats.lib.Db
{
    public class DBSearch
    {
        private readonly DSReplayContext _context;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public DBSearch(DSReplayContext context, IMemoryCache cache, ILogger<DBSearch> logger)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IEnumerable<DSReplay>> Init(DBSearchOptions dbOpt)
        {
            dbOpt.Replays = _context.DSReplays;
            return await GetReplaysPart(dbOpt);
        }

        public async Task<DSReplay> GetReplay(int id)
        {
            _logger.LogInformation("Getting Replay " + id);
            return await _context.DSReplays
                .Include(p => p.Middle)
                .Include(p => p.DSPlayer)
                .ThenInclude(q => q.Breakpoints)
                .FirstOrDefaultAsync(x => x.ID == id);
        }

        public async Task<IEnumerable<DSReplay>> Search(DBSearchOptions dbOpt)
        {
            IQueryable<DSReplay> repids = _context.DSReplays;

            if (dbOpt.DefaultFilter)
                repids = DBReplayFilter.Filter(new DSoptions(), _context, false);

            List<string> GameModes = dbOpt.GameModes.Where(x => x.Value == true).Select(s => s.Key).ToList();

            if (GameModes != null && GameModes.Any())
            {
                repids = repids.Where(x => GameModes.Contains(x.GAMEMODE));
            }

            if (dbOpt.PlayerRace != null && dbOpt.PlayerRace != "Any")
            {
                if (dbOpt.OpponentRace == null || dbOpt.OpponentRace == "Any")
                {
                    repids = from r in repids
                             from p in r.DSPlayer
                             where p.NAME.Length == 64 && p.RACE == dbOpt.PlayerRace
                             select r;
                } else
                {
                    repids = from r in repids
                             from p in r.DSPlayer
                             where p.NAME.Length == 64 && p.RACE == dbOpt.PlayerRace && p.OPPRACE == dbOpt.OpponentRace
                             select r;
                    dbOpt.InterestVs = dbOpt.OpponentRace;
                }
                dbOpt.Interest = dbOpt.PlayerRace;
            }

            if (dbOpt.MatchupPlayerRace != null && dbOpt.MatchupPlayerRace != "Any")
            {
                if (dbOpt.MatchupOpponentRace == null || dbOpt.MatchupOpponentRace == "Any")
                {
                    repids = from r in repids
                             from p in r.DSPlayer
                             where p.RACE == dbOpt.MatchupPlayerRace
                             select r;
                } else
                {
                    repids = from r in repids
                             from p in r.DSPlayer
                             where p.RACE == dbOpt.MatchupPlayerRace && p.OPPRACE == dbOpt.MatchupOpponentRace
                             select r;
                    dbOpt.InterestVs = dbOpt.MatchupOpponentRace;
                }
                dbOpt.Interest = dbOpt.MatchupPlayerRace;
            }

            if (dbOpt.PlayerUnits != null && dbOpt.PlayerUnits.Count > 0)
            {
                var reps = dbOpt.OpponentUnits switch {
                    null => from r in repids
                                from p in r.DSPlayer
                                where p.RACE == dbOpt.PlayerUnits.Race
                                from b in p.Breakpoints
                                where b.Breakpoint == "ALL" && b.dsUnitsString.Contains(dbOpt.PlayerUnits.ID + ",")
                                select new { r, b, p },
                    _ => from r in repids
                             from p in r.DSPlayer
                             where p.RACE == dbOpt.PlayerUnits.Race && p.OPPRACE == dbOpt.OpponentUnits.Race
                             from b in p.Breakpoints
                             where b.Breakpoint == "ALL" && b.dsUnitsString.Contains(dbOpt.PlayerUnits.ID + ",")
                             select new { r, b, p }
                };
                var breps = await reps.ToListAsync();

                HashSet<int> uReplays = new HashSet<int>();
                foreach (var ent in breps)
                {
                    var units = ent.b.GetUnits();
                    var intunit = units.FirstOrDefault(f => f.Name == dbOpt.PlayerUnits.Name && f.Count >= dbOpt.PlayerUnits.Count);
                    if (intunit != null && dbOpt.OpponentUnits != null && dbOpt.OpponentUnits.Count > 0)
                    {
                        int okey = DBFunctions.GetOpp(ent.p.REALPOS);
                        var opps = from r in repids
                                       where r.ID == ent.r.ID
                                       from p in r.DSPlayer
                                       where p.REALPOS == okey
                                       from b in p.Breakpoints
                                       where b.Breakpoint == "ALL" && b.dsUnitsString.Contains(dbOpt.OpponentUnits.ID + ",")
                                       select new { r, b};
                        var obreps = await opps.ToListAsync();
                        if (!obreps.Any())
                            intunit = null;
                        foreach (var oent in obreps)
                        {
                            var ounits = oent.b.GetUnits();
                            var ointunit = ounits.FirstOrDefault(f => f.Name == dbOpt.OpponentUnits.Name && f.Count >= dbOpt.OpponentUnits.Count);
                            if (ointunit == null)
                                intunit = null;
                        }
                        dbOpt.InterestVs = dbOpt.OpponentUnits.Race;
                    }
                    if (intunit != null)
                        uReplays.Add(ent.r.ID);
                }
                dbOpt.Interest = dbOpt.PlayerUnits.Race;
                repids = _context.DSReplays.Where(x => uReplays.Contains(x.ID));
            }

            dbOpt.Replays = repids;
            await GetWinrate(dbOpt);
            return await GetReplaysPart(dbOpt);
        }

        public async Task GetWinrate(DBSearchOptions dbOpt)
        {
            if (!String.IsNullOrEmpty(dbOpt.Interest))
            {

                var res = String.IsNullOrEmpty(dbOpt.InterestVs) switch {

                    true => from r in dbOpt.Replays
                        from t1 in r.DSPlayer
                        where t1.RACE == dbOpt.Interest
                          select new WinRateHelper()
                          {
                              WIN = t1.WIN,
                              RACE = t1.RACE,
                              OPPRACE = t1.OPPRACE,
                              DURATION = r.DURATION,
                              ID = r.ID
                          },
                    _ => from r in dbOpt.Replays
                         from t1 in r.DSPlayer
                         where t1.RACE == dbOpt.Interest && t1.OPPRACE == dbOpt.InterestVs
                         select new WinRateHelper()
                         {
                             WIN = t1.WIN,
                             RACE = t1.RACE,
                             OPPRACE = t1.OPPRACE,
                             DURATION = r.DURATION,
                             ID = r.ID
                         }
                };
                
                var result = res.Where(x => x.RACE == dbOpt.Interest);
                float games = 1;
                float wins = 0;
                
                games = await result.CountAsync();
                wins = await result.Where(x => x.WIN == true).CountAsync();
                dbOpt.Winrate = MathF.Round(wins * 100 / games, 2);
            }
        }

        public async Task<IEnumerable<DSReplay>> GetReplaysPart(DBSearchOptions dbOpt)
        {
            IEnumerable<DSReplay> Replays = new List<DSReplay>();
            dbOpt.Count = await dbOpt.Replays.CountAsync();
            if (dbOpt.Order)
            {
                Replays = dbOpt.Sort switch
                {
                    "ID" => await dbOpt.Replays.OrderByDescending(o => o.ID).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "REPLAY" => await dbOpt.Replays.OrderByDescending(o => o.REPLAY).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "GAMETIME" => await dbOpt.Replays.OrderByDescending(o => o.GAMETIME).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "WINNER" => await dbOpt.Replays.OrderByDescending(o => o.WINNER).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "DURATION" => await dbOpt.Replays.OrderByDescending(o => o.DURATION).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MAXLEAVER" => await dbOpt.Replays.OrderByDescending(o => o.MAXLEAVER).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MINKILLSUM" => await dbOpt.Replays.OrderByDescending(o => o.MINKILLSUM).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MININCOME" => await dbOpt.Replays.OrderByDescending(o => o.MININCOME).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MINARMY" => await dbOpt.Replays.OrderByDescending(o => o.MINARMY).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "PLAYERCOUNT" => await dbOpt.Replays.OrderByDescending(o => o.PLAYERCOUNT).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "GAMEMODE" => await dbOpt.Replays.OrderByDescending(o => o.GAMEMODE).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    _ => await dbOpt.Replays.OrderByDescending(o => o.GAMETIME).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync()
                };
            }
            else
            {
                Replays = dbOpt.Sort switch
                {
                    "ID" => await dbOpt.Replays.OrderBy(o => o.ID).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "REPLAY" => await dbOpt.Replays.OrderBy(o => o.REPLAY).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "GAMETIME" => await dbOpt.Replays.OrderBy(o => o.GAMETIME).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "WINNER" => await dbOpt.Replays.OrderBy(o => o.WINNER).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "DURATION" => await dbOpt.Replays.OrderBy(o => o.DURATION).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MAXLEAVER" => await dbOpt.Replays.OrderBy(o => o.MAXLEAVER).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MINKILLSUM" => await dbOpt.Replays.OrderBy(o => o.MINKILLSUM).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MININCOME" => await dbOpt.Replays.OrderBy(o => o.MININCOME).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "MINARMY" => await dbOpt.Replays.OrderBy(o => o.MINARMY).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "PLAYERCOUNT" => await dbOpt.Replays.OrderBy(o => o.PLAYERCOUNT).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    "GAMEMODE" => await dbOpt.Replays.OrderBy(o => o.GAMEMODE).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync(),
                    _ => await dbOpt.Replays.OrderBy(o => o.GAMETIME).Skip(dbOpt.Skip).Take(dbOpt.Take).ToListAsync()
                };

            }
            
            return Replays;
        }

    }

    public class DBSearchOptions
    {
        public Dictionary<string, bool> GameModes { get; set; }
        public string PlayerRace { get; set; } = "Any";
        public string OpponentRace { get; set; } = "Any";
        public string MatchupPlayerRace { get; set; } = "Any";
        public string MatchupOpponentRace { get; set; } = "Any";
        public DBSearchUnits PlayerUnits { get; set; }
        public DBSearchUnits OpponentUnits { get; set; }
        public IQueryable<DSReplay> Replays { get; set; }
        public string Sort { get; set; } = "GAMETIME";
        public bool Order { get; set; } = true;
        public int Take { get; set; } = 20;
        public int Skip { get; set; } = 0;
        public int Count { get; set; } = 0;
        public string Interest { get; set; } = String.Empty;
        public bool DefaultFilter { get; set; } = false;
        public string InterestVs { get; set; } = String.Empty;
        public string Player { get; set; }
        public float Winrate { get; set; } = 0;

        public DBSearchOptions()
        {
            GameModes = new Dictionary<string, bool>();
            foreach (string mode in DSdata.s_gamemodes)
            {
                GameModes[mode] = false;
            }
        }
    }

    public class DBSearchUnits
    {
        public string Race { get; set; }
        public string Name { get; set; }
        public int ID { get; set; }
        public int Count { get; set; } = 0;
    }

    public class WinRateHelper
    {
        public bool WIN { get; set; }
        public int DURATION { get; set; }
        public int ID { get; set; }
        public string RACE { get; set; }
        public string OPPRACE { get; set; }
        public string SYNRACE { get; set; }
        public int KILLSUM { get; set; }
        public int MAXKILLSUM { get; set; }
        public int ARMY { get; set; }
    }
}
