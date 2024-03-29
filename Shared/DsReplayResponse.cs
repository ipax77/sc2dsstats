﻿using System;
using System.Collections.Generic;

namespace sc2dsstats._2022.Shared
{
    public class DsReplayResponse
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public List<string> Races { get; set; }
        public List<string> Players { get; set; }
        public DateTime Gametime { get; set; }
        public int Duration { get; set; }
        public int PlayerCount { get; set; }
        public string GameMode { get; set; }
        public int MaxLeaver { get; set; }
        public int MaxKillsum { get; set; }
        public int Winner { get; set; }
        public bool DefaultFilter { get; set; }
        public decimal Mid1 { get; set; }
        public decimal Mid2 { get; set; }
    }
}
