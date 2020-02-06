using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Models
{
    public class DSinfo
    {
        public string Name { get; set; }
        public string Json { get; set; }
        public int Total { get; set; }
        public DateTime LastUpload { get; set; }
        public string LastRep { get; set; }
        public string Version { get; set; }
        public bool SendAllV1_5 { get; set; } = false;
    }
}
