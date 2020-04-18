using IronPython.Runtime;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace sc2dsstats.decode
{
    public static class Details
    {
        public static Regex rx_subname = new Regex(@"<sp\/>(.*)$", RegexOptions.Singleline);

        public static DSReplay Get(string replay_file, dynamic details_dec)
        {
            DSReplay replay = new DSReplay();
            List<DSPlayer> Players = new List<DSPlayer>();
            replay.REPLAYPATH = replay_file;
            int failsafe_pos = 0;
            foreach (var player in details_dec["m_playerList"])
            {
                if (player["m_observe"] > 0) continue;

                failsafe_pos++;
                string name = "";
                Bytes bab = null;
                try
                {
                    bab = player["m_name"];
                }
                catch { }

                if (bab != null) name = Encoding.UTF8.GetString(bab.ToByteArray());
                else name = player["m_name"].ToString();

                Match m2 = rx_subname.Match(name);
                if (m2.Success) name = m2.Groups[1].Value;
                DSPlayer pl = new DSPlayer();

                pl.NAME = name;
                pl.RACE = player["m_race"].ToString();
                pl.RESULT = byte.Parse(player["m_result"].ToString());
                pl.TEAM = byte.Parse(player["m_teamId"].ToString());
                pl.POS = (byte)failsafe_pos;
                if (player["m_workingSetSlotId"] != null)
                    pl.WORKINGSETSLOT = byte.Parse(player["m_workingSetSlotId"].ToString());

                pl.Stats = new List<DbStats>();
                pl.Spawns = new List<DbSpawn>();
                pl.Refineries = new List<DbRefinery>();
                pl.Upgrades = new List<DbUpgrade>();

                pl.DSReplay = replay;
                Players.Add(pl);
            }

            replay.PLAYERCOUNT = (byte)Players.Count;

            //long offset = (long)details_dec["m_timeLocalOffset"];
            long timeutc = (long)details_dec["m_timeUTC"];
            long georgian = timeutc;
            replay.GAMETIME = DateTime.FromFileTime(georgian);
            replay.GAMETIME = new DateTime(replay.GAMETIME.Year, replay.GAMETIME.Month, replay.GAMETIME.Day, replay.GAMETIME.Hour, replay.GAMETIME.Minute, replay.GAMETIME.Second, 0, replay.GAMETIME.Kind);
            replay.DSPlayer = Players;
            return replay;
        }
    }
}
