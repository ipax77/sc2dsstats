using System;
using System.Collections.Generic;

namespace sc2dsstats.lib.Db.Models
{
    public partial class Dsunits
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Bp { get; set; }
        public int Count { get; set; }
        public int? DsplayerId { get; set; }

        public virtual Dsplayers Dsplayer { get; set; }
    }
}
