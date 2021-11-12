using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using System.IO;

namespace sc2dsstats.db.Services
{
    public class NameService
    {
        public static List<CommanderName> CommanderNames;
        public static List<UnitName> UnitNames;
        public static List<UpgradeName> UpgradeNames;

        public static async Task Init(sc2dsstatsContext context, string rootPath)
        {
            if (!context.CommanderNames.Any())
            {

                context.CommanderNames.Add(new CommanderName()
                {
                    sId = 0,
                    Name = "Protoss"
                });
                context.CommanderNames.Add(new CommanderName()
                {
                    sId = 1,
                    Name = "Terran"
                });
                context.CommanderNames.Add(new CommanderName()
                {
                    sId = 2,
                    Name = "Zerg"
                });
                int sId = 3;

                foreach (var cmdr in DSData.cmdrs)
                {
                    context.CommanderNames.Add(new CommanderName()
                    {
                        sId = sId,
                        Name = cmdr
                    });
                    sId++;
                }
                context.SaveChanges();
            }            
            CommanderNames = await context.CommanderNames.OrderBy(o => o.sId).ToListAsync();
            UnitNames = await context.UnitNames.OrderBy(o => o.sId).ToListAsync();
            UpgradeNames = await context.UpgradeNames.OrderBy(o => o.sId).ToListAsync();
            if (!UnitNames.Any() || !UpgradeNames.Any())
            {
                Init(rootPath);
                context.UnitNames.AddRange(UnitNames);
                context.UpgradeNames.AddRange(UpgradeNames);
                await context.SaveChangesAsync();
            }
        }

        private static void Init(string rootPath = "/data")
        {
            UnitNames = JsonSerializer.Deserialize<List<UnitName>>(File.ReadAllText(Path.Combine(rootPath, "json/unitnames.json")));
            UpgradeNames = JsonSerializer.Deserialize<List<UpgradeName>>(File.ReadAllText(Path.Combine(rootPath, "json/upgradenames.json")));
        }

        public static string GetUnitName(int id)
        {
            return UnitNames.FirstOrDefault(f => f.sId == id)?.Name;
        }

        public static int GetUnitId(sc2dsstatsContext context, string name)
        {
            var unit = UnitNames.FirstOrDefault(f => f.Name == name);
            if (unit == null)
            {
                if (context == null)
                {
                    return -1;
                }
                else
                {
                    int id = !UnitNames.Any() ? 0 : UnitNames.Last().sId + 1;
                    unit = new UnitName()
                    {
                        sId = id,
                        Name = name
                    };
                    UnitNames.Add(unit);
                    context.UnitNames.Add(unit);
                    var info = context.DsInfo.First(f => f.Id == 1);
                    var now = DateTime.UtcNow;
                    info.UnitNamesUpdate = new DateTime(now.Year, now.Month, now.Day);
                }
            }
            return unit.sId;
        }

        public static string GetUpgradeName(int id)
        {
            return UpgradeNames.FirstOrDefault(f => f.sId == id)?.Name;
        }

        public static int GetUpgradeId(sc2dsstatsContext context, string name)
        {
            var upgrade = UpgradeNames.FirstOrDefault(f => f.Name == name);
            if (upgrade == null)
            {
                if (context == null)
                {
                    return -1;
                }
                else
                {
                    int id = !UpgradeNames.Any() ? 0 : UpgradeNames.Last().sId + 1;
                    upgrade = new UpgradeName()
                    {
                        sId = id,
                        Name = name
                    };
                    UpgradeNames.Add(upgrade);
                    context.UpgradeNames.Add(upgrade);
                    var info = context.DsInfo.First(f => f.Id == 1);
                    var now = DateTime.UtcNow;
                    info.UpgradeNamesUpdate = new DateTime(now.Year, now.Month, now.Day);
                }
            }
            return upgrade.sId;
        }

        public static void ConvertNameStrings(sc2dsstatsContext context, Dsreplay replay) 
        {
            foreach (var player in replay.Dsplayers)
            {
                foreach (var bp in player.Breakpoints)
                {
                    (string ds, string db) = NameService.ConvertUnitStrings(context, bp.DsUnitsString, bp.DbUnitsString);
                    string up = NameService.ConvertUpgradeString(context, bp.DbUpgradesString);
                    bp.DsUnitsString = ds;
                    bp.DbUnitsString = db;
                    bp.DbUpgradesString = up;
                    replay.Version = "4.0";
                    // logger.LogInformation($"DsUnits: {bp.DsUnitsString} => {ds}");
                    // logger.LogInformation($"DbUnits: {bp.DbUnitsString} => {db}");
                    // logger.LogInformation($"Upgrades: {bp.DbUpgradesString} => {up}");
                }
            }
        }

        public static (string, string) ConvertUnitStrings(sc2dsstatsContext context, string dsUnitsString, string dbUnitsString)
        {
            StringBuilder newdsUnitsString = new StringBuilder();
            StringBuilder newdbUnitsString = new StringBuilder();

            if (!String.IsNullOrEmpty(dsUnitsString))
            {
                var units = dsUnitsString.Split("|");
                for (int i = 0; i < units.Length; i++)
                {
                    var unitcounts = units[i].Split(",");
                    int id;
                    if (int.TryParse(unitcounts[0], out id))
                    {
                        var jsonUnit = DSData.Units.FirstOrDefault(f => f.ID == id);
                        if (jsonUnit != null)
                        {
                            id = GetUnitId(context, jsonUnit.Name);
                        }
                    }
                    else
                    {
                        id = GetUnitId(context, unitcounts[0]);
                    }
                    newdsUnitsString.Append($"{id},{unitcounts[1]}|");
                }
                newdsUnitsString.Remove(newdsUnitsString.Length - 1, 1);
            }

            if (!String.IsNullOrEmpty(dbUnitsString))
            {
                var pos = dbUnitsString.Split("|");
                for (int i = 0; i < pos.Length; i++)
                {
                    var unitpos = pos[i].Split(",");
                    int id;
                    if (int.TryParse(unitpos[0], out id))
                    {
                        var jsonUnit = DSData.Units.FirstOrDefault(f => f.ID == id);
                        if (jsonUnit != null)
                        {
                            id = GetUnitId(context, jsonUnit.Name);
                        }
                    }
                    else
                    {
                        id = GetUnitId(context, unitpos[0]);
                    }
                    newdbUnitsString.Append($"{id},{unitpos[1]},{unitpos[2]}|");

                }
                if (newdbUnitsString.Length > 0)
                    newdbUnitsString.Remove(newdbUnitsString.Length - 1, 1);
            }
            return (newdsUnitsString.Length == 0 ? null : newdsUnitsString.ToString(), newdbUnitsString.Length == 0 ? null : newdbUnitsString.ToString());
        }

        public static string ConvertUpgradeString(sc2dsstatsContext context, string upgradeString)
        {
            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrEmpty(upgradeString))
            {
                var ups = upgradeString.Split("|");
                for (int i = 0; i < ups.Length; i++)
                {
                    int id;
                    if (int.TryParse(ups[i], out id))
                    {
                        var jsonUpgrade = DSData.Upgrades.FirstOrDefault(f => f.ID == id);
                        if (jsonUpgrade != null)
                        {
                            id = GetUpgradeId(context, jsonUpgrade.Name);
                        }
                    }
                    else
                    {
                        id = GetUpgradeId(context, ups[i]);
                    }
                    sb.Append($"{id}|");
                }
                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }

            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}