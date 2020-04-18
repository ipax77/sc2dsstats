using System.IO;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using sc2dsstats.lib.Data;
using System.Collections.Generic;

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
