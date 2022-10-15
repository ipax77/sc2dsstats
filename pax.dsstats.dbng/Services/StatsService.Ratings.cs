using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<int> GetRatingsCount(CancellationToken token = default)
    {
        return await context.Players.CountAsync(token);
    }

    public async Task<List<PlayerRatingDto>> GetRatings(int skip, int take, Order order, CancellationToken token = default)
    {
        var players = context.Players
            .OrderBy(o => o.PlayerId)
            .AsNoTracking();

        if (order.Ascending)
        {
            players = players.AppendOrderBy(order.Property);
        }
        else
        {
            players = players.AppendOrderByDescending(order.Property);
        }
        return await players
        .Skip(skip)
            .Take(take)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        return await context.Players
            .Where(x => x.ToonId == toonId)
            .Select(s => s.DsROverTime)
            .FirstOrDefaultAsync();
    }

    public async Task GetRatingsDeviation()
    {
        var players = context.Players
            .AsNoTracking();

        var bab = from p in players
                  select new
                  {
                      All = 1,
                      Range =
                        (p.DsR > 0 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 550) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" :
                        (p.DsR >= 500 && p.DsR < 500) ? "1" : null
                  };
        var lbab = bab.ToListAsync();
    }
}
