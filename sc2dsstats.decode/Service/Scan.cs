using sc2dsstats.lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace sc2dsstats.decode.Service
{
    public static class Scan
    {
        public static IEnumerable<string> ScanReplayFolders(IEnumerable<string> DbReplays)
        {
            List<string> NewReplays = new List<string>();
            foreach (var directory in DSdata.Config.Replays)
                foreach (var file in Directory.GetFiles(directory, "*.SC2Replay", SearchOption.AllDirectories).Where(path => !DbReplays.Contains(path) && DSdata.rx_ds.IsMatch(path)))
                    NewReplays.Add(file);
            return NewReplays;
        }
    }
}
