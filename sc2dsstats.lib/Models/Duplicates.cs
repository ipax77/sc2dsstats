using System.Collections.Generic;

namespace sc2dsstats.lib.Models
{
    public class Duplicates
    {
        public string REPHASH { get; set; }
        public Dictionary<string, string> PLHASH { get; set; } = new Dictionary<string, string>();
        public HashSet<string> REPHASHS { get; set; } = new HashSet<string>();
    }
}
