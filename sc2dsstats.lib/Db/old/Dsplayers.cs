using System;
using System.Collections.Generic;

namespace sc2dsstats.lib.Db.Models
{
    public partial class Dsplayers
    {
        public Dsplayers()
        {
            Dsunits = new HashSet<Dsunits>();
        }

        public int Id { get; set; }
        public byte Pos { get; set; }
        public byte Realpos { get; set; }
        public string Name { get; set; }
        public string Race { get; set; }
        public string Opprace { get; set; }
        public bool Win { get; set; }
        public byte Team { get; set; }
        public int Killsum { get; set; }
        public int Income { get; set; }
        public int Army { get; set; }
        public byte Gas { get; set; }
        public int? DsreplayId { get; set; }
        public int? Pduration { get; set; }

        public virtual Dsreplays Dsreplay { get; set; }
        public virtual ICollection<Dsunits> Dsunits { get; set; }
    }
}
