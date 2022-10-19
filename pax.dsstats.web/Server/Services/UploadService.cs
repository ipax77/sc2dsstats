using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.IO.Compression;
using System.Text;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<UploadService> logger;

    public UploadService(IServiceProvider serviceProvider, IMapper mapper, ILogger<UploadService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task ImportReplays(string gzipbase64String, Guid appGuid)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);
        if (uploader == null)
        {
            return;
        }

        uploader.LatestUpload = DateTime.UtcNow;
        await context.SaveChangesAsync();
        _ = Produce(gzipbase64String, appGuid);
    }

    public async Task<DateTime> CreateOrUpdateUploader(UploaderDto uploader)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dbUplaoder = await context.Uploaders
            .Include(i => i.Players)
            .FirstOrDefaultAsync(f => f.BattleNetId == uploader.BattleNetId);

        if (dbUplaoder == null)
        {
            dbUplaoder = mapper.Map<Uploader>(uploader);
            await CreateUploaderPlayers(context, dbUplaoder);

            context.Uploaders.Add(dbUplaoder);
            await context.SaveChangesAsync();
        }
        else
        {
            await UpdateUploaderPlayers(context, dbUplaoder, uploader);
            if (dbUplaoder.AppGuid != uploader.AppGuid)
            {
                dbUplaoder.AppGuid = uploader.AppGuid;
            }
            await context.SaveChangesAsync();
        }
        return dbUplaoder.LatestReplay;
    }

    private static async Task CreateUploaderPlayers(ReplayContext context, Uploader dbUplaoder)
    {
        for (int i = 0; i < dbUplaoder.Players.Count; i++)
        {
            var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == dbUplaoder.Players.ElementAt(i).ToonId);
            if (dbPlayer != null)
            {
                dbUplaoder.Players.Remove(dbUplaoder.Players.ElementAt(i));
                dbUplaoder.Players.Add(dbPlayer);
            }
        }
    }

    private async Task UpdateUploaderPlayers(ReplayContext context, Uploader dbUplaoder, UploaderDto uploader)
    {
        for (int i = 0; i < dbUplaoder.Players.Count; i++)
        {
            var uploaderPlayer = uploader.Players.FirstOrDefault(f => f.Toonid == dbUplaoder.Players.ElementAt(i).ToonId);
            if (uploaderPlayer == null)
            {
                dbUplaoder.Players.Remove(dbUplaoder.Players.ElementAt(i));
            }
            else if (uploaderPlayer.Name != dbUplaoder.Players.ElementAt(i).Name)
            {
                dbUplaoder.Players.ElementAt(i).Name = uploaderPlayer.Name;
            }
        }

        for (int i = 0; i < uploader.Players.Count; i++)
        {
            var dbuploaderPlayer = dbUplaoder.Players.FirstOrDefault(f => f.ToonId == uploader.Players.ElementAt(i).Toonid);
            if (dbuploaderPlayer == null)
            {
                dbUplaoder.Players.Add(mapper.Map<Player>(uploader.Players.ElementAt(i)));
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task<string> UnzipAsync(string base64string)
    {
        var bytes = Convert.FromBase64String(base64string);
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                await gs.CopyToAsync(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }
}
