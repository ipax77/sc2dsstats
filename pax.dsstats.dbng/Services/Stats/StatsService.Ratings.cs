using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<int> GetRatingsCount(CancellationToken token = default)
    {
        return await context.Players.CountAsync(token);
    }

    public async Task<List<PlayerRatingDto>> GetRatings(int skip, int take, List<TableOrder> orders, CancellationToken token = default)
    {
        var players = context.Players
            .OrderBy(o => o.PlayerId)
            .AsNoTracking();

        players = SetOrder(players, orders);

        return await players
        .Skip(skip)
            .Take(take)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public IQueryable<Player> SetOrder(IQueryable<Player> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Ascending)
            {
                players = players.AppendOrderBy(order.Property);
            }
            else
            {
                players = players.AppendOrderByDescending(order.Property);
            }
        }
        return players;
    }

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        return await context.Players
            .Where(x => x.ToonId == toonId)
            .Select(s => $"{s.MmrOverTime}X{s.MmrStdOverTime}")
            .FirstOrDefaultAsync();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        var devs = await context.Players
            .GroupBy(g => Math.Round(g.Mmr, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Mmr, 0))
            }).ToListAsync();

        return devs;
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        var devs = await context.Players
            .GroupBy(g => Math.Round(g.MmrStd, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.MmrStd, 0))
            }).ToListAsync();

        return devs;
    }
}
