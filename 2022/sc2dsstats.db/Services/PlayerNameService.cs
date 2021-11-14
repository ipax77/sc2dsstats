using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats.db.Services;

public class PlayerNameService
{
    public static async Task GetStats(sc2dsstatsContext context)
    {

        var request = from r in context.Dsreplays
                      from p in r.Dsplayers
                      group p by p.Name into g
                      select new
                      {
                          Name = g.Key,
                          Count = g.Count(),
                          Wins = g.Count(c => c.Win)
                      };
        var players = await request.AsNoTracking().ToListAsync();
        foreach (var player in players.OrderBy(o => o.Count))
        {
            Console.WriteLine($"{player.Name} => {player.Count}|{player.Wins}");
        }
    }
}
