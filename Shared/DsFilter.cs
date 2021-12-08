using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using static sc2dsstats._2022.Shared.DSData;

namespace sc2dsstats._2022.Shared
{
    public class DsFilter
    {
        [JsonIgnore]
        public DateTime StartTime { get; set; }
        [JsonIgnore]
        public DateTime EndTime { get; set; }
        [Range(0, int.MaxValue)]
        public int MinDuration { get; set; } = 300;
        [Range(0, int.MaxValue)]
        public int MaxDuration { get; set; } = 0;
        public int MinArmy { get; set; } = 1500;
        [Range(0, int.MaxValue)]
        public int MinIncome { get; set; } = 1500;
        [Range(0, int.MaxValue)]
        public int MaxLeaver { get; set; } = 90;
        [Range(0, int.MaxValue)]
        public int MinKills { get; set; } = 1500;
        [Required]
        [Range(0, 6)]
        public int PlayerCount { get; set; } = 6;
        public bool Mid { get; set; } = false;
        public bool DefaultTime { get; set; } = true;
        public List<int> GameModes { get; set; }
        public List<string> Players { get; set; }
        public bool isDefault => CheckDefault();
        [JsonIgnore]
        public List<EditEnt> GameEnts { get; set; }
        [JsonIgnore]
        public List<EditEnt> PlayerEnts { get; set; }

        private bool CheckDefault()
        {
            DsFilter defaultFilter = new DsFilter();
            if (MinDuration == defaultFilter.MinDuration
                && MaxDuration == defaultFilter.MaxDuration
                && MinArmy == defaultFilter.MinArmy
                && MinIncome == defaultFilter.MinIncome
                && MaxLeaver == defaultFilter.MaxLeaver
                && MinKills == defaultFilter.MinKills
                && PlayerCount == defaultFilter.PlayerCount
                && Mid == defaultFilter.Mid
                && !(GameModes.Except(new List<int>() { (int)Gamemode.Commanders, (int)Gamemode.CommandersHeroic }).Any()
                   || (new List<int>() { (int)Gamemode.Commanders, (int)Gamemode.CommandersHeroic }).Except(GameModes).Any())
                && !Players.Any()
                && DefaultTime
            )
            {
                return true;
            }
            return false;
        }

        public void SetLists()
        {
            GameModes = GameEnts.Where(x => x.selected).Select(s => (int)GetGameMode(s.ent)).ToList();
            Players = PlayerEnts == null ? new List<string>() : PlayerEnts.Where(x => x.selected).Select(s => s.ent).ToList();
        }

        public void SetDefault()
        {
            DsFilter defaultFilter = new DsFilter();
            MinDuration = defaultFilter.MinDuration;
            MaxDuration = defaultFilter.MaxDuration;
            MinArmy = defaultFilter.MinArmy;
            MinIncome = defaultFilter.MinIncome;
            MaxLeaver = defaultFilter.MaxLeaver;
            MinKills = defaultFilter.MinKills;
            PlayerCount = defaultFilter.PlayerCount;
            Mid = defaultFilter.Mid;
            GameEnts = DSData.gamemodes.Select(s => new EditEnt() { ent = s, selected = false }).ToList();
            GameEnts.First(f => f.ent == "GameModeCommanders").selected = true;
            GameEnts.First(f => f.ent == "GameModeCommandersHeroic").selected = true;
            GameModes = new List<int>() { (int)Gamemode.Commanders, (int)Gamemode.CommandersHeroic };
            if (PlayerEnts != null && PlayerEnts.Any())
                PlayerEnts.ForEach(f => f.selected = false);
            Players = new List<string>();
        }

        public void SetOff()
        {
            MinDuration = 0;
            MaxDuration = 0;
            MinArmy = 0;
            MinIncome = 0;
            MaxLeaver = 0;
            MinKills = 0;
            PlayerCount = 0;
            Mid = false;
            GameEnts = DSData.gamemodes.Select(s => new EditEnt() { ent = s, selected = true }).ToList();
            GameModes = new List<int>();
            if (PlayerEnts != null && PlayerEnts.Any())
                PlayerEnts.ForEach(f => f.selected = false);
            Players = new List<string>();
        }
    }

    public class EditEnt
    {
        public string ent { get; set; }
        public bool selected { get; set; }
    }
}
