using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using System.Security.Cryptography;
using System.Text;
using static sc2dsstats._2022.Shared.DSData;

namespace sc2dsstats.db.Services
{
    public class DbService
    {
        public static void SetDefaultFilter(sc2dsstatsContext context)
        {
            var replays = context.Dsreplays.OrderBy(o => o.Id);
            int step = 1000;
            int i = 0;
            var stepReplays = replays.Skip(i * step).Take(step).ToList();
            while (stepReplays.Any())
            {
                ReplayFilter.SetDefaultFilter(stepReplays);
                context.SaveChanges();
                i++;
                stepReplays = replays.Skip(i * step).Take(step).ToList();
                if (i % 100 == 0)
                {
                    Console.WriteLine(i);
                }
            }
        }

        public static void SetIsPlayer(sc2dsstatsContext context, List<string> names = null)
        {
            if (names == null)
            {
                foreach (var pl in context.Dsplayers.Where(x => x.Name.Length == 64))
                    pl.isPlayer = true;
            }
            else
            {
                foreach (var pl in context.Dsplayers.Where(x => names.Contains(x.Name)))
                {
                    pl.isPlayer = true;
                }
            }
            context.SaveChanges();
        }

        public static void SetMid(sc2dsstatsContext context)
        {
            var replays = context.Dsreplays.Include(i => i.Middles);
            int i = 0;
            foreach (var replay in replays)
            {
                SetMid(replay);
                i++;
                if (i % 1000 == 0)
                {
                    Console.WriteLine(i);
                    context.SaveChanges();
                }
            }
            context.SaveChanges();
        }

        public static void SetMid(List<Dsreplay> replays)
        {
            foreach (var replay in replays)
                SetMid(replay);
        }

        public static void SetMid(Dsreplay replay)
        {
            if (replay.Middles != null && replay.Middles.Count > 1)
            {
                int loopDuration = Convert.ToInt32(replay.Duration * 22.4);
                int sumTeam1 = 0;
                int sumTeam2 = 0;
                int lastteam = 0;
                int lastloop = 0;

                foreach (var middle in replay.Middles.OrderBy(o => o.Gameloop))
                {
                    if (lastteam > 0)
                        if (middle.Team == 1)
                            sumTeam2 += middle.Gameloop - lastloop;
                        else if (middle.Team == 2)
                            sumTeam1 += middle.Gameloop - lastloop;
                    lastteam = middle.Team;
                    lastloop = middle.Gameloop;
                }
                if (lastteam == 1)
                    sumTeam1 += loopDuration - lastloop;
                else if (lastteam == 2)
                    sumTeam2 += loopDuration - lastloop;

                double mid1 = sumTeam1 / (double)loopDuration * 100.0;
                double mid2 = sumTeam2 / (double)loopDuration * 100.0;
                replay.Mid1 = Decimal.Round((decimal)mid1, 2);
                replay.Mid2 = Decimal.Round((decimal)mid2, 2);
            }
        }

        public static async Task GetInfo(sc2dsstatsContext context, string timespan)
        {
            DsRequest request = new DsRequest() { Filter = new DsFilter() };
            request.Filter.SetOff();
            request.SetTime(timespan);
            request.Filter.GameModes = new List<int>() { (int)Gamemode.Commanders, (int)Gamemode.CommandersHeroic };

            var replays = ReplayFilter.Filter(context, request);

            var leaver = from r in replays
                         group r by r.Maxleaver > 89 into g
                         select new
                         {
                             Leaver = g.Key,
                             Count = g.Count()
                         };
            var lleaver = await leaver.ToListAsync();

            int count1 = lleaver[0].Count;
            int count2 = lleaver[1].Count;

        }

        public static async Task<List<CmdrStats>> GetStats(sc2dsstatsContext context, bool player)
        {
            var stats = player
                ? from r in context.Dsreplays
                  from p in r.Dsplayers
                  where r.DefaultFilter && p.isPlayer
                  group new { r, p } by new { year = r.Gametime.Year, month = r.Gametime.Month, race = p.Race, opprace = p.Opprace } into g
                  select new CmdrStats()
                  {
                      year = g.Key.year,
                      month = g.Key.month,
                      RACE = g.Key.race,
                      OPPRACE = g.Key.opprace,
                      count = g.Count(),
                      wins = g.Count(c => c.p.Win),
                      mvp = g.Count(c => c.p.Killsum == c.r.Maxkillsum),
                      army = g.Sum(s => s.p.Army),
                      kills = g.Sum(s => s.p.Killsum),
                      duration = g.Sum(s => s.r.Duration),
                      replays = g.Select(s => s.r.Id).Distinct().Count()
                  }
                : from r in context.Dsreplays
                  from p in r.Dsplayers
                  where r.DefaultFilter
                  group new { r, p } by new { year = r.Gametime.Year, month = r.Gametime.Month, race = p.Race, opprace = p.Opprace } into g
                  select new CmdrStats()
                  {
                      year = g.Key.year,
                      month = g.Key.month,
                      RACE = g.Key.race,
                      OPPRACE = g.Key.opprace,
                      count = g.Count(),
                      wins = g.Count(c => c.p.Win),
                      mvp = g.Count(c => c.p.Killsum == c.r.Maxkillsum),
                      army = g.Sum(s => s.p.Army),
                      kills = g.Sum(s => s.p.Killsum),
                      duration = g.Sum(s => s.r.Duration),
                      replays = g.Select(s => s.r.Id).Distinct().Count()
                  };
            var cmdrstats = await stats.ToListAsync();
            cmdrstats = cmdrstats.Where(x => DSData.GetCommanders.Select(s => (byte)s).Contains(x.RACE)).ToList();
            cmdrstats = cmdrstats.Where(x => DSData.GetCommanders.Select(s => (byte)s).Contains(x.OPPRACE)).ToList();
            return cmdrstats;
        }

        public static string GetReplayHash(Dsreplay replay)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pl in replay.Dsplayers.OrderBy(o => o.Pos))
            {
                sb.Append(pl.Pos);
                sb.Append(((DSData.Commander)pl.Race).ToString());
            }
            sb.Append(replay.Minarmy);
            sb.Append(replay.Minkillsum);
            sb.Append(replay.Minincome);
            sb.Append(replay.Maxkillsum);
            return GetMd5Hash(sb.ToString());
        }

        public static string GetMd5Hash(string input)
        {
            byte[] data;
            using (MD5 md5Hash = MD5.Create())
            {
                data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}
