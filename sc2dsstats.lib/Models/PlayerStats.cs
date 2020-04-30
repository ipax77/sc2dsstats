using sc2dsstats.lib.Db;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using sc2dsstats.lib.Data;
using System.Data.SqlTypes;
using System.Threading.Tasks.Sources;

namespace sc2dsstats.lib.Models
{
    public class PlayerStats
    {

        public string MostPlayedCmdr { get; set; }
        public float MostPlayedCmdrWr { get; set; }
        public string BestCmdr { get; set; }
        public float BestCmdrWr { get; set; }
        public string WorstCmdr { get; set; }
        public float WorstCmdrWr { get; set; }
        public string BestMatchUp { get; set; }
        public string WorstMatchUp { get; set; }

        public List<PlayerStat> Stats { get; set; } = new List<PlayerStat>();
        public List<PlayerStat> OppStats { get; set; } = new List<PlayerStat>();
        public PlayerStat TStats { get; set; } = new PlayerStat();
        public PlayerStat WorstStat { get; set; } = new PlayerStat();
        public PlayerStat BestStat { get; set; } = new PlayerStat();
        public List<PlayerStat> PosStats { get; set; } = new List<PlayerStat>();

        public void Init(DSReplayContext _context, List<string> names)
        {
            if (!names.Any()) return;
            var reps = from p in _context.DSPlayers
                       where names.Contains(p.NAME)
                       select new
                       {
                           p.REALPOS,
                           p.RACE,
                           p.OPPRACE,
                           p.PDURATION,
                           p.WIN
                       };

            int total = 0;
            int totalwins = 0;
            int tduration = 0;

            foreach (string cmdr in DSdata.s_races_cmdr)
            {
                var cmdrreps = reps.Where(x => x.RACE == cmdr);
                PlayerStat stat = new PlayerStat();
                int duration = 0;
                if (cmdrreps.Any())
                {
                    stat.Race = cmdr;
                    stat.Count = cmdrreps.Count();
                    stat.Wins = cmdrreps.Where(x => x.WIN == true).Count();
                    stat.Winrate = MathF.Round((float)stat.Wins * 100 / (float)stat.Count, 2);
                    duration = cmdrreps.Sum(s => s.PDURATION);
                    stat.AvgGameDuration = TimeSpan.FromSeconds(duration / stat.Count);
                    Stats.Add(stat);


                    foreach (string oppcmdr in DSdata.s_races_cmdr)
                    {
                        var vsreps = cmdrreps.Where(o => o.OPPRACE == oppcmdr);
                        if (vsreps.Count() > 1)
                        {
                            PlayerStat vsstat = new PlayerStat();
                            vsstat.Count = vsreps.Count();
                            vsstat.Race = cmdr;
                            vsstat.OppRace = oppcmdr;
                            vsstat.Wins = vsreps.Where(x => x.WIN == true).Count();
                            vsstat.Winrate = MathF.Round((float)vsstat.Wins * 100 / (float)vsstat.Count, 2);
                            int vduration = vsreps.Sum(s => s.PDURATION);
                            vsstat.AvgGameDuration = TimeSpan.FromSeconds(vduration / vsstat.Count);
                            stat.Vs.Add(stat);

                            if (WorstStat.Race == String.Empty)
                                WorstStat = vsstat;
                            else if (vsstat.Winrate < WorstStat.Winrate)
                                WorstStat = vsstat;

                            if (BestStat.Race == String.Empty)
                                BestStat = vsstat;
                            else if (vsstat.Winrate > BestStat.Winrate)
                                BestStat = vsstat;
                        }
                    }
                }
                

                var oppcmdrreps = reps.Where(x => x.OPPRACE == cmdr);
                PlayerStat oppstat = new PlayerStat();
                if (oppcmdrreps.Any())
                {
                    
                    oppstat.Race = cmdr;
                    oppstat.Count = oppcmdrreps.Count();
                    oppstat.Wins = oppcmdrreps.Where(x => x.WIN == true).Count();
                    oppstat.Winrate = MathF.Round((float)oppstat.Wins * 100 / (float)oppstat.Count, 2);
                    int oppduration = oppcmdrreps.Sum(s => s.PDURATION);
                    oppstat.AvgGameDuration = TimeSpan.FromSeconds(oppduration / oppstat.Count);
                    OppStats.Add(oppstat);
                }

                total += stat.Count;
                totalwins += stat.Wins;
                tduration += duration;
            }

            TStats.Race = "Total";
            TStats.Count = total;
            if (total > 0)
            {
                TStats.Winrate = MathF.Round((float)totalwins * 100 / total, 2);
                TStats.AvgGameDuration = TimeSpan.FromSeconds(tduration / total);
            }

            for (int i = 1; i <= 6; i++)
            {
                var preps = reps.Where(x => x.REALPOS == i);
                PlayerStat stat = new PlayerStat();
                if (preps.Any())
                {
                    stat.Race = String.Empty;
                    stat.Pos = i;
                    stat.Count = preps.Count();
                    stat.Wins = preps.Where(x => x.WIN == true).Count();
                    stat.Winrate = MathF.Round((float)stat.Wins * 100 / (float)stat.Count, 2);
                    int pduration = preps.Sum(s => s.PDURATION);
                    stat.AvgGameDuration = TimeSpan.FromSeconds(pduration / stat.Count);
                    PosStats.Add(stat);
                }
            }

        }
    }

    public class PlayerStat
    {
        public string Race { get; set; } = String.Empty;
        public string OppRace { get; set; } = String.Empty;
        public int Pos { get; set; } = 0;
        public int Count { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public float Winrate { get; set; } = 0;
        public List<PlayerStat> Vs { get; set; } = new List<PlayerStat>();
        public TimeSpan AvgGameDuration { get; set; } = TimeSpan.Zero;
    }
}
