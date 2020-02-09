using sc2dsstats.decode.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using sc2dsstats.lib.Models;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Db;

namespace sc2dsstats.lib.Data
{
    
    public class LoadData
    {
        public event EventHandler DataLoaded;
        private ReplaysLoadedEventArgs args = new ReplaysLoadedEventArgs();
        public UserConfig myConfig = new UserConfig();
        private DSbuilds _build;

        public LoadData(DSbuilds build)
        {
            _build = build;
        }

        protected virtual void OnDataLoaded(ReplaysLoadedEventArgs e)
        {
            EventHandler handler = DataLoaded;
            handler?.Invoke(this, e);
        }

        public async Task Init()
        {
            lock (args)
                args.isBuildLoaded = false;

            //await LoadReplays();
            //await _build.Init(DSdata.Replays);
            //await _build.InitBuilds();

            int i = 0;
            using (var context = new DSReplayContext())
            {
                i = context.DSReplays.Count();
            }

            DSDbBuilds b = new DSDbBuilds();
            b.GetBuild();

            await Task.Delay(750);

            lock (args)
            {
                args.Count = i;
                args.isBuildLoaded = true;
                args.isReplaysLoaded = true;
                args.isDuplicatesLoaded = true;
            }

            OnDataLoaded(args);
            //GC.Collect();
        }

        public async Task LoadReplays()
        {
            if (!File.Exists(DSdata.Config.WorkDir + "/data.json"))
                throw new FileNotFoundException("Data file data.json not found");

            lock (args)
            {
                args.Count = 0;
                args.isReplaysLoaded = false;
            }
            OnDataLoaded(args);

            DSdata.Replays = new List<dsreplay>();
            await Task.Run(() => {
                lock (DSdata.Replays)
                {
                    foreach (string line in File.ReadAllLines(DSdata.Config.WorkDir + "/data.json", Encoding.UTF8))
                    {
                        dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                        if (rep != null)
                        {
                            rep.Init();
                            //rep.GenHash();
                            DSdata.Replays.Add(rep);
                            
                            foreach (string plhash in rep.PLDupPos.Keys)
                            {
                                DatasetInfo info = DSdata.Datasets.SingleOrDefault(s => s.Dataset == plhash);
                                if (info == null)
                                {
                                    info = new DatasetInfo();
                                    info.Dataset = plhash;
                                    info.Teamgames = 0;
                                    info.Count = 0;
                                    DSdata.Datasets.Add(info);
                                }
                                info.Count++;
                                if (rep.PLDupPos.Count > 1)
                                    info.Teamgames++;
                            }
                        }
                    }
                }
                HashSet<string> units = new HashSet<string>();
                foreach (var rep in DSdata.Replays)
                    foreach (var pl in rep.PLAYERS)
                        if (pl.UNITS.ContainsKey("ALL"))
                            foreach (var unit in pl.UNITS["ALL"].Keys)
                                units.Add(unit);

                DSdata.s_units = units.ToArray();
            });

            lock (args)
            {
                args.Count = DSdata.Replays.Count;
                args.isReplaysLoaded = true;
            }

            OnDataLoaded(args);
        }

        public void SaveConfig()
        {
            Dictionary<string, UserConfig> temp = new Dictionary<string, UserConfig>();
            temp.Add("Config", DSdata.Config);

            var option = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(temp, option);
            File.WriteAllText(DSdata.Config.WorkDir + "/config.json", json);
        }
    }

    public class ReplaysLoadedEventArgs : EventArgs
    {
        public int Count { get; set; } = 0;
        public bool isReplaysLoaded { get; set; } = false;
        public bool isDuplicatesLoaded { get; set; } = false;
        public bool isBuildLoaded { get; set; } = false;
    }
}
