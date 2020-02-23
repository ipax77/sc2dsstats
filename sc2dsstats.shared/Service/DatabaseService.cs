using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Db;

namespace sc2dsstats.shared.Service
{
    public static class DatabaseService
    {

        public static List<DSReplay> GetReplaysPart(IQueryable<DSReplay> replays, string id, int skip, int take, bool order)
        {
            List<DSReplay> Replays = new List<DSReplay>();

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
            } else
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
            return Replays;
        }

        public static IQueryable<DSReplay> CmdrSearch(IQueryable<DSReplay> replays, DatabaseSearchOptions dbSearch)
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
            } else
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
                    } else
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

    public class DatabaseSearchOptions
    {
        public Dictionary<int, string> Cmdrs { get; set; } = new Dictionary<int, string>();
        public bool anySearch { get; set; } = true;
    }
}
