using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.decode.Models
{
    public class DECunit
    {
        public string Name { get; set; }
        public int BornGameloop { get; set; }
        public int BornX { get; set; }
        public int BornY { get; set; }
        public int DiedGameloop { get; set; }
        public int DiedX { get; set; }
        public int DiedY { get; set; }
        public int ID { get; set; }
        public int RID { get; set; }
        public DECunit KilledBy { get; set; }
    }
}
