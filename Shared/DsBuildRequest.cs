using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace sc2dsstats._2022.Shared
{
    public class DsBuildRequest : DsRequest
    {
        public string Playername { get; set; }
        public List<string> Playernames { get; set; }
        [NotMapped]
        [JsonIgnore]
        public string CacheKey => $"Build{Interest}{(String.IsNullOrEmpty(Versus) ? "" : Versus)}{Timespan.Replace(" ", "")}{(String.IsNullOrEmpty(Playername) ? "" : Playername)}";
    }
}
