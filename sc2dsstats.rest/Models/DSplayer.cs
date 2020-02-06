using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Models
{
    [Serializable]
    public class DSplayer
    {
        public string Name { get; set; }
        public string LastRep { get; set; }
        public string _LastRep { get; set; }
        public string LastAutoRep { get; set; }
        public string _LastAutoRep { get; set; }
        public int Data { get; set; } = 0;
        public List<DateTime> Uploads { get; set; } = new List<DateTime>();
        public DSinfo Info { get; set; } = new DSinfo();
        public bool SendAllV1_5 { get; set; } = false;
    }
}
