using System;
using System.Collections.Generic;
using System.Linq;

namespace sc2dsstats._2022.Shared
{
    public class DsGameResponse
    {
        public int Id { get; set; }
        public DateTime GameTime { get; set; }
        public int Duration { get; set; }
        public string GameMode { get; set; }
        public List<MiddleResponse> Middle { get; set; }
        public int Objective { get; set; }
        public int Winner { get; set; }
        public int MaxKills { get; set; }
        public int Bunker { get; set; }
        public int Cannon { get; set; }
        public decimal Mid1 { get; set; }
        public decimal Mid2 { get; set; }
        public List<DsPlayerResponse> Players { get; set; }

        public int GetMiddle(int gameloop, int team)
        {
            KeyValuePair<int, int> lastent = new KeyValuePair<int, int>(0, 0);
            int mid = 0;
            bool hasInfo = false;
            foreach (var ent in Middle.OrderBy(o => o.Gameloop))
            {
                if (ent.Gameloop > gameloop)
                {
                    hasInfo = true;
                    if (lastent.Value == team + 1)
                        mid += gameloop - lastent.Key;
                    break;
                }

                if (lastent.Key > 0 && lastent.Value == team + 1)
                    mid += ent.Gameloop - lastent.Key;

                lastent = new KeyValuePair<int, int>(ent.Gameloop, ent.Team);
            }
            if (Middle.Any())
                if (hasInfo == false && Middle.OrderBy(o => o.Gameloop).Last().Team == team + 1)
                    mid += gameloop - Middle.OrderBy(o => o.Gameloop).Last().Gameloop;

            if (mid < 0) mid = 0;
            return mid;
        }
    }

    public class MiddleResponse
    {
        public int Gameloop { get; set; }
        public byte Team { get; set; }
    }
}
