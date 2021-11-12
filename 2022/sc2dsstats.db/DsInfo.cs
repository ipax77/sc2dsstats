using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats.db
{
    public class DsInfo
    {
        public int Id { get; set; }
        public DateTime UnitNamesUpdate { get; set; }
        public DateTime UpgradeNamesUpdate { get; set; }
    }
}
