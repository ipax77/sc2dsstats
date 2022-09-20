using Microsoft.EntityFrameworkCore;
using sc2dsstats.shared;

namespace sc2dsstats.db.Services
{
    public class BuildService
    {
        public static async Task<DsBuildResponse> GetBuild(sc2dsstatsContext context, DsBuildRequest request)
        {

            var replays = ReplayFilter.DefaultFilter(context);

            replays = replays.Where(x => x.Gametime >= request.StartTime);
            if (request.EndTime != DateTime.Today)
            {
                replays = replays.Where(x => x.Gametime <= request.EndTime);
            }

            IQueryable<BuildHelper> buildResults;

            if (request.Playernames == null || !request.Playernames.Any())
            {
                string Playername = request.Playername;
                if (!String.IsNullOrEmpty(Playername))
                {
                    var player = await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Name == request.Playername);
                    if (player != null)
                    {
                        Playername = player.DbId.ToString();
                    }
                    //Playername = request.Playername switch
                    //{
                    //    "PAX" => await context.DsPlayerNames.FirstOrDefaultAsync(f => f.Name ==),
                    //    "Feralan" => "e2dfd75fcad1c454cfb2526fae4f3feb5e901039f7d366f69094c0d16a12e338",
                    //    "Panzerfaust" => "bd78339bb80c299a6c82812d9d4547d09cf15b0e8bb99b38090dc3bc4a5af8b5",
                    //    _ => "b33aef3fcc740b0d67eda3faa12c0f94cef5213fe70921d72fc2bfa8125a5889"
                    //};
                }

                buildResults = GetBuildResultQuery(replays, request, new List<string>() { Playername });
            }
            else
            {
                buildResults = GetBuildResultQuery(replays, request, request.Playernames);
            }

            var builds = await buildResults.AsSplitQuery().ToListAsync();
            var uniqueBuilds = builds.GroupBy(g => g.Id).Select(s => s.First()).ToList();


            var response = new DsBuildResponse()
            {
                Interest = request.Interest,
                Versus = request.Versus,
                Count = uniqueBuilds.Count,
                Wins = uniqueBuilds.Where(s => s.Win).Count(),
                Duration = uniqueBuilds.Sum(s => s.Duration),
                Gas = uniqueBuilds.Sum(s => s.GasCount),
                Upgrades = uniqueBuilds.Sum(s => s.UpgradeSpending),
                Replays = uniqueBuilds.Select(t => new DsBuildResponseReplay()
                {
                    Hash = t.Hash,
                    Gametime = t.Gametime
                }).ToList(),
                Breakpoints = new List<DsBuildResponseBreakpoint>()
            };

            var breakpoints = buildResults.Select(s => s.Breakpoint).Distinct();
            foreach (var bp in breakpoints)
            {
                var bpReplays = builds.Where(x => x.Breakpoint == bp).ToList();

                response.Breakpoints.Add(new DsBuildResponseBreakpoint()
                {
                    Breakpoint = bp,
                    Count = bpReplays.Count,
                    Wins = bpReplays.Where(x => x.Win).Count(),
                    Duration = bpReplays.Sum(s => s.Duration),
                    Gas = bpReplays.Sum(s => s.GasCount),
                    Upgrades = bpReplays.Sum(s => s.UpgradeSpending),
                    Units = GetUnits(bpReplays.Select(s => s.UnitString).ToList())
                });
            }
            return response;
        }

        public static IQueryable<BuildHelper> GetBuildResultQuery(IQueryable<Dsreplay> replays, DsBuildRequest request, List<string> playernames)
        {
            return (String.IsNullOrEmpty(request.Versus), !playernames.Any()) switch
            {
                (true, true) => from r in replays
                                from p in r.Dsplayers
                                where p.Race == (byte)DSData.GetCommander(request.Interest)
                                from b in p.Breakpoints
                                select new BuildHelper()
                                {
                                    Id = r.Id,
                                    Hash = r.Hash,
                                    Gametime = r.Gametime,
                                    UnitString = b.DsUnitsString,
                                    Win = p.Win,
                                    UpgradeSpending = b.Upgrades,
                                    GasCount = b.Gas,
                                    Breakpoint = b.Breakpoint1,
                                    Duration = r.Duration
                                },
                (true, false) => from r in replays
                                 from p in r.Dsplayers
                                 where p.Race == (byte)DSData.GetCommander(request.Interest) && playernames.Contains(p.Name)
                                 from b in p.Breakpoints
                                 select new BuildHelper()
                                 {
                                     Id = r.Id,
                                     Hash = r.Hash,
                                     Gametime = r.Gametime,
                                     UnitString = b.DsUnitsString,
                                     Win = p.Win,
                                     UpgradeSpending = b.Upgrades,
                                     GasCount = b.Gas,
                                     Breakpoint = b.Breakpoint1,
                                     Duration = r.Duration
                                 },
                (false, true) => from r in replays
                                 from p in r.Dsplayers
                                 where p.Race == (byte)DSData.GetCommander(request.Interest) && p.Opprace == (byte)DSData.GetCommander(request.Versus)
                                 from b in p.Breakpoints
                                 select new BuildHelper()
                                 {
                                     Id = r.Id,
                                     Hash = r.Hash,
                                     Gametime = r.Gametime,
                                     UnitString = b.DsUnitsString,
                                     Win = p.Win,
                                     UpgradeSpending = b.Upgrades,
                                     GasCount = b.Gas,
                                     Breakpoint = b.Breakpoint1,
                                     Duration = r.Duration
                                 },
                (false, false) => from r in replays
                                  from p in r.Dsplayers
                                  where p.Race == (byte)DSData.GetCommander(request.Interest) && p.Opprace == (byte)DSData.GetCommander(request.Versus) && playernames.Contains(p.Name)
                                  from b in p.Breakpoints
                                  select new BuildHelper()
                                  {
                                      Id = r.Id,
                                      Hash = r.Hash,
                                      Gametime = r.Gametime,
                                      UnitString = b.DsUnitsString,
                                      Win = p.Win,
                                      UpgradeSpending = b.Upgrades,
                                      GasCount = b.Gas,
                                      Breakpoint = b.Breakpoint1,
                                      Duration = r.Duration
                                  },

            };
        }

        public static List<DsBuildResponseBreakpointUnit> GetUnits(List<string> unitStrings)
        {
            List<DsBuildResponseBreakpointUnit> unitsums = new List<DsBuildResponseBreakpointUnit>();
            List<DsPlayerBreakpointUnitResponse> units = new List<DsPlayerBreakpointUnitResponse>();
            foreach (string unitString in unitStrings)
            {
                var ents = unitString.Split("|");

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
            }
            var uniqueUnits = units.Select(s => s.Name).Distinct().ToList();
            foreach (var unit in uniqueUnits)
            {
                unitsums.Add(new DsBuildResponseBreakpointUnit()
                {
                    Name = unit,
                    Count = units.Where(x => x.Name == unit).Sum(s => s.Count)
                });
            }
            return unitsums;
        }

    }


    public class BuildHelper
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public DateTime Gametime { get; set; }
        public string UnitString { get; set; }
        public bool Win { get; set; }
        public int UpgradeSpending { get; set; }
        public int GasCount { get; set; }
        public string Breakpoint { get; set; }
        public int Duration { get; set; }
    }


}
