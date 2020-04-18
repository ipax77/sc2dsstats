using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace sc2dsstats.lib.Models
{
    public class DSRestModel
    {
    }

    public class DSRestPlayer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Json { get; set; }
        public DateTime LastRep { get; set; }
        public DateTime LastUpload { get; set; }
        public int Data { get; set; } = 0;
        public int Total { get; set; } = 0;
        public bool SendAllV1_5 { get; set; } = false;
        public string Version { get; set; }
        public virtual ICollection<DSRestUpload> Uploads { get; set; }
    }


    public class DSRestUpload
    {
        public int ID { get; set; }
        public DateTime Upload { get; set; }
        public virtual DSRestPlayer DSRestPlayer { get; set; }
    }
}
