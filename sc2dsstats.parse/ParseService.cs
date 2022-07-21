using s2protocol.NET;
using s2protocol.NET.Models;
using sc2dsstats._2022.Shared;
using System.Reflection;

namespace sc2dsstats.parse;

public class ParseService
{
    private static readonly string? _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private static readonly List<string> SpawnUnits = new List<string>
    {
        "TrophyRiftPremium",
        "MineralIncome",
        "ParasiticBombRelayDummy",
        "Biomass",
        "PurifierAdeptShade",
        "PurifierTalisShade",
        "HornerReaperLD9ClusterCharges",
        "Broodling",
        "Raptorling",
        "InfestedLiberatorViralSwarm",
        "SplitterlingSpawn",
        "GuardianShell",
        "BroodlingStetmann",
    };

    private static readonly int[] AreaTeam1 = new int[8] { 107, 162, 160, 106, 218, 160, 162, 216 };
    private static readonly int[] AreaTeam2 = new int[8] { 35, 88, 92, 30, 142, 99, 100, 144 };

    public static async Task<DsReplayDto?> ParseReplayAsync(string replayPath, string? assemblyPath = null)
    {
        if (!File.Exists(replayPath))
        {
            return null;
        }

        if (assemblyPath == null)
        {
            assemblyPath = _assemblyPath;
        }

        if (!Directory.Exists(assemblyPath))
        {
            return null;
        }

        ReplayDecoderOptions options = new ReplayDecoderOptions()
        {
            Details = true,
            Metadata = false,
            MessageEvents = false,
            TrackerEvents = true
        };

        ReplayDecoder decoder = new ReplayDecoder(assemblyPath);
        Sc2Replay? replay = await decoder.DecodeAsync(replayPath);

        if (replay == null)
        {
            return null;
        }

        return GetReplayDto(replay);
    }

    private static DsReplayDto? GetReplayDto(Sc2Replay replay)
    {
        if (replay.Details == null || replay.TrackerEvents == null)
        {
            return null;
        }

        DsReplayDto replayDto = new DsReplayDto();

        ParseDetails(replayDto, replay.Details);
        ParseTrackerEvents(replayDto, replay.TrackerEvents);

        return replayDto;
    }

    private static void ParseTrackerEvents(DsReplayDto replay, TrackerEvents trackerEvents)
    {
        ParseTrackerUnitEvents(replay, trackerEvents.SUnitBornEvents, trackerEvents.SUnitDiedEvents, trackerEvents.SUnitTypeChangeEvents);
        ParseUpgradeEvents(replay, trackerEvents.SUpgradeEvents);

    }

    private static void ParseUpgradeEvents(DsReplayDto replay, ICollection<SUpgradeEvent> upgradeEvetns)
    {
        foreach (SUpgradeEvent upgradeEvent in upgradeEvetns)
        {

        }
    }

    private static void ParseTrackerUnitEvents(DsReplayDto replay, ICollection<SUnitBornEvent> bornEvents, ICollection<SUnitDiedEvent> diedEvents, ICollection<SUnitTypeChangeEvent> changeEvents)
    {
        SUnitBornEvent? nexusBornEvent = null;
        SUnitBornEvent? planetaryBornEvent = null;

        List<SUnitBornEvent> Refineries = new List<SUnitBornEvent>();
        List<SUnitBornEvent> Units = new List<SUnitBornEvent>();

        int i = 0;
        foreach (SUnitBornEvent bornEvent in bornEvents)
        {
            i++;
            DSPlayerDto? player = null;
            bool noStagingAreaNextSpawn = false;
            if (replay.GAMETIME < new DateTime(2019, 03, 24, 21, 46, 15))
            {
                noStagingAreaNextSpawn = true;
            }

            if (bornEvent.UnitTypeName.StartsWith("Worker"))
            {
                player = replay.DSPlayer.SingleOrDefault(f => f.POS == bornEvent.ControlPlayerId);
                if (player == null)
                {
                    player = replay.DSPlayer.SingleOrDefault(s => s.REALPOS == bornEvent.ControlPlayerId - 1);
                    if (player == null)
                    {
                        continue;
                    }
                    else
                    {
                        player.POS = bornEvent.ControlPlayerId;
                    }
                }
                player.REALPOS = 0;
                player.RACE = bornEvent.UnitTypeName[6..];
            }



            if (bornEvent.Gameloop == 0)
            {
                if (replay.OBJECTIVE == 0 && (nexusBornEvent == null || planetaryBornEvent == null))
                {
                    if (bornEvent.UnitTypeName == "ObjectivePlanetaryFortress")
                    {
                        planetaryBornEvent = bornEvent;
                    }
                    else if (bornEvent.UnitTypeName == "ObjectiveNexus")
                    {
                        nexusBornEvent = bornEvent;
                    }
                    if (planetaryBornEvent != null && nexusBornEvent != null)
                    {
                        SetObjective(replay, nexusBornEvent, planetaryBornEvent);
                    }
                }

                if (bornEvent.UnitTypeName.StartsWith("MineralField"))
                {
                    Refineries.Add(bornEvent);
                }

            }
            else
            {
                if (bornEvent.UnitTypeName.StartsWith("DeathBurst"))
                {
                    replay.DURATION = (int)(bornEvent.Gameloop / 22.4);
                    replay.WINNER = bornEvent.PlayerId == 13 ? 1 : 0;
                    Console.WriteLine($"born events handled: {i}");
                    Console.WriteLine($"units collected: {Units.Count}");
                    return;
                }

                if (bornEvent.ControlPlayerId == 0 || bornEvent.ControlPlayerId > 12)
                {
                    continue;
                }

                if (bornEvent.Gameloop < 480)
                {
                    continue;
                }

                if (SpawnUnits.Contains(bornEvent.UnitTypeName))
                {
                    continue;
                }

                if (CheckSquare(AreaTeam1, bornEvent.X, bornEvent.Y) || CheckSquare(AreaTeam2, bornEvent.X, bornEvent.Y))
                {
                    Units.Add(bornEvent);
                }

                if (player == null)
                {
                    player = replay.DSPlayer.SingleOrDefault(s => s.POS == bornEvent.ControlPlayerId);
                    if (player == null)
                    {
                        continue;
                    }
                }

                if (player.REALPOS == 0)
                {
                    SetPlayerRealPos(replay, player, bornEvent);
                }

            }
        }
    }

    private static void ParseDetails(DsReplayDto replay, Details details)
    {
        replay.DSPlayer = new List<DSPlayerDto>();

        int failsafe_pos = 0;
        foreach (var player in details.Players)
        {
            if (player.Observe > 0)
            {
                continue;
            }

            failsafe_pos++;

            replay.DSPlayer.Add(new DSPlayerDto()
            {
                NAME = player.Name,
                RACE = player.Race,
                POS = failsafe_pos,
                REALPOS = player.WorkingSetSlotId > 0 ? player.WorkingSetSlotId : failsafe_pos,
            });
        }
        replay.GAMETIME = details.DateTimeUTC;
    }

    private static void SetPlayerRealPos(DsReplayDto replay, DSPlayerDto player, SUnitBornEvent bornEvent)
    {
        int pos = 0;

        if (replay.PLAYERCOUNT == 2)
            pos = 1;
        else if ((bornEvent.Gameloop - 480) % 1440 == 0)
            pos = 1;
        else if ((bornEvent.Gameloop - 481) % 1440 == 0)
            pos = 1;
        else if ((bornEvent.Gameloop - 960) % 1440 == 0)
            pos = 2;
        else if ((bornEvent.Gameloop - 961) % 1440 == 0)
            pos = 2;
        else if ((bornEvent.Gameloop - 1440) % 1440 == 0)
            pos = 3;
        else if ((bornEvent.Gameloop - 1441) % 1440 == 0)
            pos = 3;

        if (replay.PLAYERCOUNT == 4 && pos == 3) pos = 1;

        if (pos > 0)
        {
            bool isTeam1 = CheckSquare(AreaTeam1, bornEvent.X, bornEvent.Y);
            if (isTeam1)
            {
                player.REALPOS = pos;
                player.TEAM = 0;
            }
            else
            {
                bool isTeam2 = CheckSquare(AreaTeam2, bornEvent.X, bornEvent.Y);
                if (isTeam2)
                {
                    player.REALPOS = pos + 3;
                    player.TEAM = 1;
                }
            }
        }
    }

    private static float ChackArea(int x1, int y1, int x2,
                  int y2, int x3, int y3)
    {
        return MathF.Abs((x1 * (y2 - y3) +
                                x2 * (y3 - y1) +
                                x3 * (y1 - y2)) / 2.0f);
    }

    private static bool CheckSquare(int[] teamArea, int x, int y)
    {
        int x1 = teamArea[0];
        int y1 = teamArea[1];
        int x2 = teamArea[2];
        int y2 = teamArea[3];
        int x3 = teamArea[4];
        int y3 = teamArea[5];
        int x4 = teamArea[6];
        int y4 = teamArea[7];

        float A = ChackArea(x1, y1, x2, y2, x3, y3) +
                  ChackArea(x1, y1, x4, y4, x3, y3);

        float A1 = ChackArea(x, y, x1, y1, x2, y2);

        float A2 = ChackArea(x, y, x2, y2, x3, y3);

        float A3 = ChackArea(x, y, x3, y3, x4, y4);

        float A4 = ChackArea(x, y, x1, y1, x4, y4);

        return (A == A1 + A2 + A3 + A4);
    }

    private static void SetObjective(DsReplayDto replay, SUnitBornEvent nexus, SUnitBornEvent planetary)
    {
        KeyValuePair<int, int> Center = new KeyValuePair<int, int>((nexus.X + planetary.X) / 2, (nexus.Y + planetary.Y) / 2);
        replay.OBJECTIVE = (Center.Key, Center.Value) switch
        {
            (128, 120) => 1,
            (120, 120) => 2,
            (128, 122) => 3,
            _ => 0
        };
    }

}
