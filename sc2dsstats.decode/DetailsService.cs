using sc2dsstats.lib.Models;
using System.Text.RegularExpressions;

namespace sc2dsstats.decode
{
    public static class DetailsService
    {
        public static Regex rx_subname = new Regex(@"<sp\/>(.*)$", RegexOptions.Singleline);

        public static DSReplay GetDetails(dynamic details)
        {
            DSReplay replay = new DSReplay()
            {
                DSPlayer = new HashSet<DSPlayer>()
            };

            byte failsafe_pos = 0;
            foreach (var player in details["m_playerList"])
            {
                if ((int)player["m_observe"] > 0) continue;

                failsafe_pos++;
                string name = DecodeService.GetString(player, "m_name");

                Match m2 = rx_subname.Match(name);
                if (m2.Success) name = m2.Groups[1].Value;

                replay.DSPlayer.Add(new DSPlayer()
                {
                    NAME = name,
                    RACE = DecodeService.GetString(player, "m_race"),
                    RESULT = (byte)(int)player["m_result"],
                    TEAM = (byte)(int)player["m_teamId"],
                    POS = failsafe_pos,
                    WORKINGSETSLOT = player["m_workingSetSlotId"] != null ? (byte)(int)player["m_workingSetSlotId"] : (byte)0,
                    Stats = new List<DbStats>(),
                    Spawns = new List<DbSpawn>(),
                    Refineries = new List<DbRefinery>(),
                    Upgrades = new List<DbUpgrade>()
                });
            }

            replay.PLAYERCOUNT = (byte)replay.DSPlayer.Count;

            long timeutc = (long)details["m_timeUTC"];
            long georgian = timeutc;
            replay.GAMETIME = DateTime.FromFileTime(georgian);
            replay.GAMETIME = new DateTime(replay.GAMETIME.Year, replay.GAMETIME.Month, replay.GAMETIME.Day, replay.GAMETIME.Hour, replay.GAMETIME.Minute, replay.GAMETIME.Second, 0, replay.GAMETIME.Kind);
            return replay;
        }
    }
}
