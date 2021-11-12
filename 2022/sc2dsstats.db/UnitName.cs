﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats.db
{
    public class UnitName
    {
        public int Id { get; set; }
        public int sId { get; set; }
        [MaxLength(64)]
        public string Name { get; set; }
        // public virtual CommanderName CommanderName {  get; set; }
    }

    public class CommanderName
    {
        public int Id { get; set; }
        public int sId { get; set; }
        [MaxLength(64)]
        public string Name { get; set; }
    }

    public class UpgradeName
    {
        public int Id { get; set; }
        public int sId { get; set; }
        [MaxLength(64)]
        public string Name { get; set; }
        // public virtual CommanderName CommanderName { get; set; }
    }
}
