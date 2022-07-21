using System;

namespace sc2dsstats.lib.Models
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
