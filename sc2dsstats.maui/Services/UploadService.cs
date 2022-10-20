using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sc2dsstats.maui.Services;

public class UploadService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly SemaphoreSlim ss = new(1, 1);

    public UploadService(IServiceProvider serviceProvider, IMapper mapper)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
    }

    public async Task UploadReplays()
    {
        await ss.WaitAsync();

        try
        {
            var latestReplayDate = await GetLastReplay();

            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var replays = await context.Replays
                .Include(i => i.Players)
                    .ThenInclude(t => t.Spawns)
                        .ThenInclude(t => t.Units)
                            .ThenInclude(t => t.Unit)
                .Include(i => i.Players)
                    .ThenInclude(t => t.Player)
                    .AsNoTracking()
                    .AsSplitQuery()
                .OrderBy(o => o.GameTime)
                .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
                .Where(x => x.GameTime > latestReplayDate)
                .ToListAsync();

            var base64string = GetBase64String(replays.Take(1).ToList());

            File.WriteAllText("/data/ds/uploadtest3.json", JsonSerializer.Serialize(replays.Take(1).ToList()));
        } catch (Exception ex)
        {

        } finally
        {
            ss.Release();
        }
    }

    private string GetBase64String(List<ReplayDto> replays)
    {
        var json = JsonSerializer.Serialize(replays);
        return Zip(json);
    }

    private async Task<DateTime> GetLastReplay()
    {
        return await Task.FromResult(new DateTime(2021, 10, 1));
    }

    private static string Zip(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return Convert.ToBase64String(mso.ToArray());
        }
    }
}
