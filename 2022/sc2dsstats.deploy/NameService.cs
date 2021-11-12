using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using System.Text.Json;

public static class NameService
{
    public static bool CreateJson(sc2dsstatsContext context, localContext? localContext)
    {
        try
        {
            var names = context.UnitNames.Select(s => new NameResponse()
            {
                sId = s.sId,
                Name = s.Name
            }).ToList();
            var upgrades = context.UpgradeNames.Select(s => new NameResponse()
            {
                sId = s.sId,
                Name = s.Name
            });

            var unitfile = "../sc2dsstats.app/json/unitnames.json";
            var upgradefile = "../sc2dsstats.app/json/upgradenames.json";

            File.WriteAllText(unitfile, JsonSerializer.Serialize(names));
            File.WriteAllText(upgradefile, JsonSerializer.Serialize(upgrades));

            if (localContext != null)
            {
                localContext.Database.ExecuteSqlRaw("TRUNCATE TABLE unitnames");
                localContext.UnitNames.AddRange(names.Select(s => new UnitName()
                {
                    sId = s.sId,
                    Name = s.Name
                }));
                localContext.SaveChanges();

                localContext.Database.ExecuteSqlRaw("TRUNCATE TABLE upgradenames");
                localContext.UpgradeNames.AddRange(upgrades.Select(s => new UpgradeName()
                {
                    sId = s.sId,
                    Name = s.Name
                }));
                localContext.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Program.logger?.LogError($"failed creating name jsons: {ex.Message}");
            return false;
        }

        var info = context.DsInfo.First(f => f.Id == 1);
        DateTime now = DateTime.UtcNow;
        info.UnitNamesUpdate = new DateTime(now.Year, now.Month, now.Day);
        info.UpgradeNamesUpdate = new DateTime(now.Year, now.Month, now.Day);
        context.SaveChanges();
        Program.logger?.LogInformation("Successfully created name jsons with DsInfo update");
        return true;
    }
}
