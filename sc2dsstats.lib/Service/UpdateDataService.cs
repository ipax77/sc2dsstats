using Newtonsoft.Json;
using sc2dsstats.lib.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace sc2dsstats.lib.Service
{
    public class UpdateDataService
    {
        public static void Reset()
        {
            var tele = JsonConvert.SerializeObject(DSdata.Telemetrie, Formatting.Indented);
            File.AppendAllText("/data/tele.json", tele);
            DSdata.Telemetrie = new ConcurrentDictionary<int, List<string>>();
        }
    }
}
