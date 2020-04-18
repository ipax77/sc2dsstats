using sc2dsstats.lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.shared.Models
{
    public class PBModel
    {
        public List<string> AvailableCmdrs { get; set; } = new List<string>();
        public List<string> PB { get; set; } = new List<string>();
        public List<Dropdown<string>> dropDowns { get; set; } = new List<Dropdown<string>>();
        public List<string> Selection { get; set; } = new List<string>();
        public List<string> isDisabled { get; set; } = new List<string>();
        public List<string> isLocked { get; set; } = new List<string>();
        public List<Visitor> Visitors { get; set; } = new List<Visitor>();
    }

    public static class PBData
    {
        public static int vID = 0;
        public static HashSet<int> IDs = new HashSet<int>();
        public static ConcurrentDictionary<int, PBModel> PBModels = new ConcurrentDictionary<int, PBModel>();
    }

}
