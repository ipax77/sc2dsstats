using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.db.Extensions;
using static sc2dsstats._2022.Shared.DSData;

namespace sc2dsstats.db.Services
{
    public static class ReplayService
    {
        public static async Task<List<DsReplayResponse>> GetReplays(sc2dsstatsContext context, DsReplayRequest request, CancellationToken cancellationToken)
        {
            var replays = GetQueriableReplays(context, request);

            if (request.sortOrders != null && request.sortOrders.Any())
            {
                foreach (var sortOrder in request.sortOrders)
                {
                    if (sortOrder.Order)
                    {
                        replays = replays.AppendOrderBy(sortOrder.Sort);
                    }
                    else
                    {
                        replays = replays.AppendOrderByDescending(sortOrder.Sort);
                    }
                }
            }
            else
                replays = replays.OrderByDescending(o => o.Gametime);

            replays = replays.Skip(request.Skip).Take(request.Take);

            List<DsReplayResponse> replayResponses = new List<DsReplayResponse>();
            if (!cancellationToken.IsCancellationRequested)
                try
                {
                    replayResponses = await replays.Select(s => new DsReplayResponse()
                    {
                        Id = s.Id,
                        Hash = s.Hash,
                        Races = s.Dsplayers.OrderBy(o => o.Realpos).Select(r => ((Commander)r.Race).ToString()).ToList(),
                        Players = s.Dsplayers.OrderBy(o => o.Realpos).Select(r => r.Name).ToList(),
                        Gametime = s.Gametime,
                        Duration = s.Duration,
                        PlayerCount = s.Playercount,
                        GameMode = ((Gamemode)s.Gamemode).ToString(),
                        MaxLeaver = s.Maxleaver,
                        MaxKillsum = s.Maxkillsum,
                        Winner = s.Winner,
                        DefaultFilter = s.DefaultFilter
                    }).ToListAsync(cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    return replayResponses;
                }
                catch (OperationCanceledException)
                {
                    return replayResponses;
                }
            return replayResponses;

        }

        private static IQueryable<Dsreplay> GetQueriableReplays(sc2dsstatsContext context, DsReplayRequest request)
        {
            //var replays = context.Dsreplays
            //    .Include(i => i.Dsplayers)
            //    .AsNoTracking();

            //if (request.DefaultFilter)
            //    replays = replays.Where(x => x.DefaultFilter == true);

            var replays = ReplayFilter.Filter(context, request);

            if (request.Races != null && request.Races.Any())
            {
                if (request.Opponents != null && request.Opponents.Any())
                {
                    int matchups = Math.Min(request.Races.Count, request.Opponents.Count);
                    for (int i = 0; i < matchups; i++)
                    {
                        var interest = (byte)DSData.GetCommander(request.Races[i]);
                        var opponent = (byte)DSData.GetCommander(request.Opponents[i]);

                        replays = interest == opponent ?
                                  from r in replays
                                  from p in r.Dsplayers
                                  where p.Realpos <= 3 &&
                                    (p.Race == interest && p.Opprace == opponent)
                                  select r
                                  :
                                  from r in replays
                                  from p in r.Dsplayers
                                  where (p.Race == interest && p.Opprace == opponent)
                                  select r
                                  ;
                    }

                    List<byte> teammates = new List<byte>();
                    if (request.Races.Count > request.Opponents.Count)
                    {
                        teammates = request.Races.Skip(request.Opponents.Count).Take(request.Races.Count - request.Opponents.Count).Select(s => (byte)DSData.GetCommander(s)).ToList();
                        replays = from r in replays
                                  from p in r.Dsplayers
                                  where teammates.Contains(p.Race)
                                  select r;
                    }
                }
                else
                    replays = from r in replays
                              from p in r.Dsplayers
                              where request.Races.Select(s => (byte)DSData.GetCommander(s)).Contains(p.Race)
                              select r;
            }

            if (!String.IsNullOrEmpty(request.Playername))
            {
                replays = String.IsNullOrEmpty(request.PlayerRace) ?
                          from r in replays
                          from p in r.Dsplayers
                          where p.Name.Contains(request.Playername)
                          select r
                          :
                          from r in replays
                          from p in r.Dsplayers
                          where p.Race == (byte)DSData.GetCommander(request.PlayerRace) && p.Name.Contains(request.Playername)
                          select r;
            }

            //if (request.Players != null && request.Players.Any())
            //{
            //    replays = from r in replays
            //              from p in r.Dsplayers
            //              where request.Players.Contains(p.Name)
            //              select r;
            //}

            return replays;
        }

        public static async Task<int> GetCount(sc2dsstatsContext context, DsReplayRequest request)
        {
            var replays = GetQueriableReplays(context, request);
            return await replays.CountAsync();
        }

        public static async Task<DsGameResponse> GetReplay(sc2dsstatsContext context, string hash)
        {
            var replay = await context.Dsreplays
                .Include(i => i.Middles)
                .Include(i => i.Dsplayers)
                .ThenInclude(t => t.Breakpoints)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.Hash == hash);

            if (replay == null)
                return null;

            Version replayVersion = Version.Parse(replay.Version);

            DsGameResponse response = new DsGameResponse()
            {
                Id = replay.Id,
                GameTime = replay.Gametime,
                Duration = replay.Duration,
                GameMode = ((Gamemode)replay.Gamemode).ToString(),
                Objective = replay.Objective,
                Winner = replay.Winner,
                MaxKills = replay.Maxkillsum,
                ReplayPath = replay.Replaypath,
                Middle = replay.Middles == null ? null : replay.Middles.Select(s => new MiddleResponse()
                {
                    Gameloop = s.Gameloop,
                    Team = s.Team
                }).ToList(),
                Players = replay.Dsplayers.Select(s => new DsPlayerResponse()
                {
                    Name = s.Name,
                    Cmdr = ((Commander)s.Race).ToString(),
                    Army = s.Army,
                    Kills = s.Killsum,
                    Cash = s.Income,
                    Duration = s.Pduration,
                    Pos = s.Realpos,
                    Uploader = s.isPlayer,
                    Leaver = replay.Duration - s.Pduration > 89 || s.Army < 1500 || s.Killsum < 1500,
                    Breakpoints = s.Breakpoints.Select(t => new DsPlayerBreakpointResponse()
                    {
                        Breakpoint = t.Breakpoint1,
                        GasCount = t.Gas,
                        UpgradesSpending = t.Upgrades,
                        Upgrades = replayVersion >= new Version(4, 0) ? GetUpgrades(t.DbUpgradesString) : GetUpgrades_v3(t.DbUpgradesString),
                        Units = replayVersion >= new Version(4, 0) ? GetUnits(t.DsUnitsString, t.DbUnitsString) : GetUnits_v3(t.DsUnitsString, t.DbUpgradesString)
                    }).ToList()
                }).ToList(),
                Bunker = replay.Bunker,
                Cannon = replay.Cannon,
                Mid1 = replay.Mid1 == null ? 0 : (decimal)replay.Mid1,
                Mid2 = replay.Mid2 == null ? 0 : (decimal)replay.Mid2
            };

            return response;
        }

        public static List<string> GetUpgrades(string upgradeString)
        {
            if (String.IsNullOrEmpty(upgradeString))
                return new List<string>();

            List<string> upgrades = new List<string>();
            var ents = upgradeString.Split("|");
            for (int i = 0; i < ents.Length; i++)
            {
                int id;
                if (int.TryParse(ents[i], out id))
                {
                    var upgrade = NameService.GetUpgradeName(id);
                    if (!String.IsNullOrEmpty(upgrade))
                    {
                        upgrades.Add(upgrade);
                    }
                }
            }
            return upgrades;
        }

        public static List<DsPlayerBreakpointUnitResponse> GetUnits(string unitCountString, string unitPosString)
        {
            if (unitCountString == null)
                return new List<DsPlayerBreakpointUnitResponse>();

            var ents = unitCountString.Split("|");
            List<DsPlayerBreakpointUnitResponse> units = new List<DsPlayerBreakpointUnitResponse>();
            for (int i = 0; i < ents.Length; i++)
            {
                var entents = ents[i].Split(",");
                if (entents.Length == 2)
                {
                    int id;
                    int count;
                    string name = entents[0];
                    if (int.TryParse(entents[0], out id))
                    {
                        name = NameService.GetUnitName(id);
                    }
                    int.TryParse(entents[1], out count);
                    units.Add(new DsPlayerBreakpointUnitResponse()
                    {
                        Name = name,
                        Count = count,
                        Positions = new List<DsPlayerBreakpointUnitPosResponse>()
                    });
                }
            }

            if (!String.IsNullOrEmpty(unitPosString))
            {
                var pents = unitPosString.Split("|");
                for (int i = 0; i < pents.Length; i++)
                {
                    var pentents = pents[i].Split(",");
                    if (pentents.Length == 3)
                    {
                        int id = 0;
                        int x = 0;
                        int y = 0;
                        string name = pentents[0];
                        if (int.TryParse(pentents[0], out id))
                        {
                            name = NameService.GetUnitName(id);
                        }
                        var unit = units.FirstOrDefault(f => f.Name == name);
                        if (unit != null)
                        {
                            int.TryParse(pentents[1], out x);
                            int.TryParse(pentents[2], out y);
                            unit.Positions.Add(new DsPlayerBreakpointUnitPosResponse() { X = x, Y = y });
                        }
                    }
                }
            }

            return units;

        }

        private static List<string> GetUpgrades_v3(string upgradeString)
        {
            if (String.IsNullOrEmpty(upgradeString))
                return new List<string>();

            List<string> upgrades = new List<string>();
            var ents = upgradeString.Split("|");
            foreach (var ent in ents)
            {
                int id = 0;
                string upgrade = ent;
                if (int.TryParse(ent, out id))
                {
                    var jsonUpgrade = DSData.Upgrades.FirstOrDefault(f => f.ID == id);
                    if (jsonUpgrade != null)
                        upgrade = jsonUpgrade.Name;
                }
                upgrades.Add(upgrade);
            }
            return upgrades;
        }

        private static List<DsPlayerBreakpointUnitResponse> GetUnits_v3(string unitCountString, string unitPosString)
        {
            if (unitCountString == null)
                return new List<DsPlayerBreakpointUnitResponse>();

            var ents = unitCountString.Split("|");
            List<DsPlayerBreakpointUnitResponse> units = new List<DsPlayerBreakpointUnitResponse>();
            foreach (var ent in ents)
            {
                var entents = ent.Split(",");
                if (entents.Length == 2)
                {
                    int id = 0;
                    int count = 0;
                    string name = entents[0];
                    if (int.TryParse(entents[0], out id))
                    {
                        var jsonUnit = DSData.Units.FirstOrDefault(f => f.ID == id);
                        if (jsonUnit != null)
                        {
                            name = jsonUnit.Name;
                        }
                    }
                    int.TryParse(entents[1], out count);
                    units.Add(new DsPlayerBreakpointUnitResponse()
                    {
                        Name = name,
                        Count = count,
                        Positions = new List<DsPlayerBreakpointUnitPosResponse>()
                    });
                }
            }

            if (!String.IsNullOrEmpty(unitPosString))
            {
                var pents = unitPosString.Split("|");
                foreach (var pent in pents)
                {
                    var pentents = pent.Split(",");
                    if (pentents.Length == 3)
                    {
                        int id = 0;
                        int x = 0;
                        int y = 0;
                        string name = pentents[0];
                        if (int.TryParse(pentents[0], out id))
                        {
                            var jsonUnit = DSData.Units.FirstOrDefault(f => f.ID == id);
                            if (jsonUnit != null)
                            {
                                name = jsonUnit.Name;
                            }
                        }
                        var unit = units.FirstOrDefault(f => f.Name == name);
                        if (unit != null)
                        {
                            int.TryParse(pentents[1], out x);
                            int.TryParse(pentents[2], out y);
                            unit.Positions.Add(new DsPlayerBreakpointUnitPosResponse() { X = x, Y = y });
                        }
                    }
                }
            }

            return units;
        }
    }
}
