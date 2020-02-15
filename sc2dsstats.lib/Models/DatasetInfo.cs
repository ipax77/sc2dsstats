using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sc2dsstats.lib.Models
{
    public class DatasetInfo
    {
        public string Dataset { get; set; }
        public int Count { get; set; } = 0;
        public int Teamgames { get; set; }
    }
}
