using Microsoft.EntityFrameworkCore;
using System.Text;
using static sc2dsstats.shared.DSData;

namespace sc2dsstats.db.Stats
{
    public partial class StatsService
    {
        public static void TeamStats(sc2dsstatsContext context)
        {
            //var replays1 = from r in context.Dsreplays
            //               from p in r.Dsplayers
            //               where r.Winner >= 0 
            //                && r.Gametime > new DateTime(2021, 01, 01)
            //                && r.Playercount == 6 
            //                && r.Maxleaver < 90 
            //                && p.Race <= 3 
            //                && p.Team == 1
            //               select new
            //               {
            //                   r.Id,
            //                   r.Winner,
            //                   p.Race
            //               };
            var teamResults = new List<TeamResult>();
            using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "select r.id, r.winner = 0 as winner, group_concat(p1.race order by p1.RACE) as races"
                    + " from dsreplays as r"
                    + " inner join dsplayers as p1 on p1.DSReplayID = r.ID"
                    + " where r.GAMETIME > '2020-01-01'"
                    //+ " and r.WINNER >= 0"
                    //+ " and r.PLAYERCOUNT = 6"
                    //+ " and r.MAXLEAVER < 90"
                    + " and r.DefaultFilter = 1"
                    + " and p1.RACE > 3"
                    + " and p1.TEAM = 0"
                    + " group by r.Id;";
                context.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {

                    while (result.Read())
                    {
                        var teamResult = new TeamResult()
                        {
                            id = result.GetInt32(0),
                            winner = result.GetByte(1),
                            races = result.GetString(2)
                        };
                        var races = teamResult.races.Split(",");
                        if (races.Length == 3)
                            teamResults.Add(teamResult);
                    }
                }
            }
            using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "select r.id, r.winner = 1 as winner, group_concat(p1.race order by p1.RACE) as races"
                    + " from dsreplays as r"
                    + " inner join dsplayers as p1 on p1.DSReplayID = r.ID"
                    + " where r.GAMETIME > '2020-01-01'"
                    //+ " and r.WINNER >= 0"
                    //+ " and r.PLAYERCOUNT = 6"
                    //+ " and r.MAXLEAVER < 90"
                    + " and r.DefaultFilter = 1"
                    + " and p1.RACE > 3"
                    + " and p1.TEAM = 1"
                    + " group by r.Id;";
                context.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {

                    while (result.Read())
                    {
                        var teamResult = new TeamResult()
                        {
                            id = result.GetInt32(0),
                            winner = result.GetByte(1),
                            races = result.GetString(2)
                        };
                        var races = teamResult.races.Split(",");
                        if (races.Length == 3)
                            teamResults.Add(teamResult);
                    }
                }
            }

            var teamGroupResults = teamResults.GroupBy(g => g.races).Select(s => new TeamGroupResult { team = s.Key, count = s.Count(), wins = s.Count(c => c.winner == 1) });
            StringBuilder sb = new StringBuilder();
            foreach (var g in teamGroupResults.OrderByDescending(o => o.winrate))
            {
                var races = g.team.Split(",");
                string team = String.Join(",", races.Select(s => (Commander)int.Parse(s)));
                Console.WriteLine($"{team} => {g.winrate}% ({g.count})");
                sb.Append($"{team} => {g.winrate}% ({g.count})");
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText("/data/teamwins.txt", sb.ToString());
        }
    }

    public class TeamResult
    {
        public int id { get; set; }
        public byte winner { get; set; }
        public string races { get; set; }
    }

    public class TeamGroupResult
    {
        public string team { get; set; }
        public int count { get; set; }
        public int wins { get; set; }
        public double winrate => count == 0 ? 0 : Math.Round(wins * 100.0 / (double)count, 2);
    }
}
