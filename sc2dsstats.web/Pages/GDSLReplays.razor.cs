using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace sc2dsstats.web.Pages
{
    public partial class GDSLReplays : ComponentBase
    {
        [Inject]
        protected NavigationManager _nav { get; set; }

        string Interest = String.Empty;

        public static List<string> Teams = new List<string>()
            {
                "GDSL",
                "Team Squirters",
                "Normalna Nazwa Klanu",
                "The Lab Rats",
                "Kapanyanyi bogyok",
                "Typische Mongos",
                "GrumpyKittens",
                "Khas",
                "MY OD JEMIOŁA",
                "strikE",
                "The Usurper's Downfall",
                "Ti’punch",
                "DSGODS",
                "No Scan",
                "Gdzie tu się strzela",
                "No Sympathy",
                "Dreamcatchers",
                "4GasRushGaming",
                "SnaZorNuz"
            };

        List<GDSLReplay> Replays = new List<GDSLReplay>();

        protected override void OnInitialized()
        {
            var files = Directory.GetFiles("wwwroot/gdslreplays");
            foreach (var file in files.Where(x => x.EndsWith(".json"))) {

                var names = Path.GetFileNameWithoutExtension(file).Split("_vs_");
                var replay = JsonSerializer.Deserialize<DSReplay>(File.ReadAllText(file));
                var repstring = new HtmlString(_nav.BaseUri + file.Replace(".json", ".SC2Replay"));
                replay.REPLAY = Path.GetFileNameWithoutExtension(file);
                Replays.Add(new GDSLReplay()
                {
                    Team1 = names[0],
                    Team2 = names[1].Substring(0, names[1].Length - 2),
                    ReplayString = repstring,
                    Replay = replay,
                    DateTime = replay.GAMETIME
                });
            }
            base.OnInitialized();
        }

        void OnInterestSelected(string selection)
        {
            Interest = selection;
        }
    }

    public class GDSLReplay
    {
        public string Team1 { get; set; }
        public string Team2 { get; set; }
        public HtmlString ReplayString { get; set; }
        public DSReplay Replay { get; set; }
        public DateTime DateTime { get; set; }
    }
}
