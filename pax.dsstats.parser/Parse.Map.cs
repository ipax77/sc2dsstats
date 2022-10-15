﻿using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.parser;

public static partial class Parse
{
    public static ReplayDto GetReplayDto(DsReplay replay, bool allSpawns = false)
    {
        var players = GetPlayers(replay, allSpawns);
        var duration = (int)(replay.Duration / 22.4);

        ReplayDto replayDto = new()
        {
            FileName = replay.FileName,
            GameTime = replay.GameTime,
            Duration = duration,
            WinnerTeam = replay.WinnerTeam,
            GameMode = Data.GetGameMode(replay.GameMode),
            // Objective = GetObjective(replay.Center),
            Objective = 0,
            Bunker = (int)(replay.Bunker / 22.4),
            Cannon = (int)(replay.Cannon / 22.4),
            Playercount = (byte)replay.Players.Count,
            Middle = GetMiddleString(replay.Middles),
            Players = players,
            Minkillsum = players.Select(s => s.Kills).Min(),
            Maxkillsum = players.Select(s => s.Kills).Max(),
            Minarmy = players.Select(s => s.Army).Min(),
            Minincome = players.Select(s => s.Income).Min(),
            Maxleaver = duration - players.Select(s => s.Duration).Min(),
            CommandersTeam1 = "|" + String.Join("|", players.Where(x => x.Team == 1).Select(s => (int)s.Race)) + "|",
            CommandersTeam2 = "|" + String.Join("|", players.Where(x => x.Team == 2).Select(s => (int)s.Race)) + "|",
        };

        replayDto.ReplayHash = GenHash(replayDto);
        return replayDto;
    }

    private static string GetMiddleString(List<DsMiddle> middles)
    {
        int firstTeam = middles.FirstOrDefault()?.Team ?? 0;
        string middleString = String.Join("|", middles.Select(s => s.Gameloop));
        return $"{firstTeam}|{middleString}";
    }

    private static ICollection<ReplayPlayerDto> GetPlayers(DsReplay replay, bool allSpawns)
    {
        List<ReplayPlayerDto> dtos = new();

        foreach (var player in replay.Players)
        {
            var commander = Data.GetCommander(player.Race);

            dtos.Add(new()
            {
                Player = new() { Name = player.Name, ToonId = player.ToonId },
                Clan = player.Clan,
                GamePos = player.GamePos,
                Team = player.Team,
                Duration = (int)(player.Duration / 22.4),
                Race = commander,
                OppRace = GetOppRace(player, replay.Players),
                PlayerResult = replay.WinnerTeam == 0 ? PlayerResult.None : player.Team == replay.WinnerTeam ? PlayerResult.Win : PlayerResult.Los,
                APM = Convert.ToInt32(player.APM),
                IsUploader = false,
                TierUpgrades = String.Join("|", player.TierUpgrades),
                Refineries = String.Join("|", player.Refineries.Select(s => s.Gameloop)),
                Kills = player.Kills,
                Income = player.Income,
                Army = player.Army,
                UpgradesSpent = player.UpgradesSpent,
                Upgrades = player.Upgrades.Select(s => new PlayerUpgradeDto() { Upgrade = new UpgradeDto() { Name = s.Upgrade }, Gameloop = s.Gameloop }).ToList(),
                Spawns = GetSpawns(player.SpawnStats, player.Race, allSpawns)
            });
        }

        return dtos;
    }

    private static ICollection<SpawnDto> GetSpawns(List<PlayerSpawnStats> spawns, string race, bool allSpawns)
    {
        var dtos = new List<SpawnDto>();

        //todo: switch commander in game?        
        var commander = Data.GetCommander(race);

        foreach (var spawn in spawns)
        {
            int gameloop = spawn.Units.FirstOrDefault()?.Gameloop ?? 0;

            if (!allSpawns)
            {
                // 5min: 6240, 6720, 7200
                // 10min: 12960, 13440, 13920
                // 15min: 19680, 20160, 20640
                if (!((gameloop > 6240 && gameloop < 7209)
                    || (gameloop > 12960 && gameloop < 13928)
                    || (gameloop > 19680 && gameloop < 20649)))
                {
                    continue;
                }
            }

            dtos.Add(new()
            {
                Gameloop = gameloop,
                Income = spawn.Income,
                GasCount = spawn.GasCount,
                ArmyValue = spawn.ArmyValue,
                KilledValue = spawn.KilledValue,
                UpgradeSpent = spawn.UpgradesSpent,
                Units = GetUnits(spawn.Units, commander)
            });
        }

        if (!allSpawns)
        {
            var lastSpawn = spawns.LastOrDefault();
            if (lastSpawn != null && dtos.LastOrDefault()?.Income != lastSpawn.Income)
            {
                dtos.Add(new()
                {
                    Gameloop = lastSpawn.Units.FirstOrDefault()?.Gameloop ?? 0,
                    Income = lastSpawn.Income,
                    GasCount = lastSpawn.GasCount,
                    ArmyValue = lastSpawn.ArmyValue,
                    KilledValue = lastSpawn.KilledValue,
                    UpgradeSpent = lastSpawn.UpgradesSpent,
                    Units = GetUnits(lastSpawn.Units, commander)
                });
            }
        }

        return dtos;
    }

    private static ICollection<SpawnUnitDto> GetUnits(List<DsUnit> units, Commander commander)
    {
        List<UnitBuilder> unitBuilds = new();
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];

            var unitBuild = unitBuilds.FirstOrDefault(f => f.Name == unit.Name);
            if (unitBuild == null)
            {
                unitBuilds.Add(new(unit.Name, unit.Position));
            }
            else
            {
                unitBuild.Add(unit.Position);
            }
        }

        return unitBuilds.Select(s => new SpawnUnitDto()
        {
            Unit = new() { Name = s.Name, Commander = commander },
            Count = s.Count > 255 ? byte.MaxValue : Convert.ToByte(s.Count),
            Poss = s.StringBuilder.ToString()
        }).ToList();
    }

    private static Commander GetOppRace(DsPlayer player, List<DsPlayer> players)
    {
        var oppPos = GetOpp(player.GamePos);
        var oppPlayer = players.FirstOrDefault(f => f.GamePos == oppPos);
        if (oppPlayer == null)
        {
            return Commander.None;
        }
        else
        {
            return Data.GetCommander(oppPlayer.Race);
        }
    }

    private static int GetOpp(int pos)
    {
        if (pos <= 3)
            return pos + 3;
        else
            return pos - 3;
    }

    private static string GenHash(ReplayDto replay)
    {
        string md5 = "";
        string hashstring = "";
        foreach (var pl in replay.Players.OrderBy(o => o.GamePos))
        {
            hashstring += pl.GamePos + pl.Race;
        }
        hashstring += replay.Minarmy + replay.Minkillsum + replay.Minincome + replay.Maxkillsum;
        using (MD5 md5Hash = MD5.Create())
        {
            md5 = GetMd5Hash(md5Hash, hashstring);
        }
        return md5;
    }

    static string GetMd5Hash(MD5 md5Hash, string input)
    {

        // Convert the input string to a byte array and compute the hash.
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        StringBuilder sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    private record UnitBuilder
    {
        public UnitBuilder(string name, Position pos)
        {
            Name = name;
            Count = 1;
            StringBuilder = new($"{pos.X},{pos.Y}");
        }

        public void Add(Position pos)
        {
            Count++;
            StringBuilder.Append($",{pos.X},{pos.Y}");
        }

        public string Name { get; init; }
        public int Count { get; set; }
        public StringBuilder StringBuilder { get; init; }
    }
}