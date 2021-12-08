using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace sc2dsstats._2022.Shared
{
    public class DsRequest
    {
        public DsRequest()
        {
        }

        public DsRequest(string mode, string timespan, bool player, string interest = "ALL", string versus = "ALL") : this()
        {
            Mode = mode;
            Player = player;
            Interest = interest;
            Versus = versus;
            SetTime(timespan);
        }

        [JsonInclude]
        public string Mode { get; private set; }
        public string Interest { get; set; }
        public string Versus { get; set; }
        [JsonInclude]
        public string Timespan { get; private set; }
        [JsonInclude]
        public DateTime StartTime { get; private set; }
        [JsonInclude]
        public DateTime EndTime { get; private set; }
        [JsonInclude]
        public bool Player { get; private set; }
        public DsFilter Filter { get; set; }
        [NotMapped]
        [JsonIgnore]
        public bool BeginAtZero { get; set; }
        [NotMapped]
        [JsonIgnore]
        public string ChartType => Mode switch
        {
            "Winrate" => "Bar",
            "MVP" => "Bar",
            "DPS" => "Bar",
            "Synergy" => "Radar",
            "AntiSynergy" => "Radar",
            "Timeline" => "Line",
            "Duration" => "Line",
            "Count" => "Pie",
            "Playerstats" => "Pie",
            "Matchups" => "Radar",
            _ => "Bar"
        };
        [NotMapped]
        [JsonIgnore]
        public List<SelectHelper> CmdrsSelected { get; set; }
        [NotMapped]
        [JsonIgnore]
        public List<DsResponse> Responses { get; set; } = new List<DsResponse>();
        [NotMapped]
        [JsonIgnore]
        public bool doReloadSelected { get; private set; }

        private void ResetSelections(string chartType)
        {
            if (this.ChartType == chartType)
            {
                doReloadSelected = true;
                return;
            }
            doReloadSelected = false;
            Responses.Clear();
            if (CmdrsSelected == null)
            {
                CmdrsSelected = new List<SelectHelper>();
                CmdrsSelected.Add(new SelectHelper()
                {
                    Name = "ALL",
                    Selected = false
                });

                foreach (var cmdr in DSData.cmdrs)
                {
                    CmdrsSelected.Add(new SelectHelper()
                    {
                        Name = cmdr,
                        Selected = false
                    });
                }
            }
            else
            {
                CmdrsSelected.ForEach(f => f.Selected = false);
            }
            if (ChartType == "Bar" || ChartType == "Pie")
            {
                CmdrsSelected.Find(f => f.Name == "ALL").Selected = true;
                Interest = "ALL";
                BeginAtZero = false;
            }
            else
            {
                CmdrsSelected.Find(f => f.Name == "Abathur").Selected = true;
                Interest = "Abathur";
                if (Mode == "Synergy" || Mode == "AntiSynergy")
                    BeginAtZero = true;
                else
                    BeginAtZero = false;
            }
        }

        public void SetMode(string mode)
        {
            string chartType = this.ChartType;
            this.Mode = mode;
            ResetSelections(chartType);
        }

        public void SetPlayer(bool player)
        {
            this.Player = player;
            ResetSelections(this.ChartType);
        }

        public void SetTime(string timeString)
        {
            this.Timespan = timeString;
            (this.StartTime, this.EndTime) = DSData.TimeperiodSelected(timeString);
            if (Filter != null)
                Filter.DefaultTime = true;
            ResetSelections(this.ChartType);
        }

        public void SetTimeString()
        {
            this.Timespan = "Custom";
            this.StartTime = this.Filter.StartTime;
            this.EndTime = this.Filter.EndTime;

            foreach (var timespan in DSData.timespans)
            {
                (DateTime start, DateTime end) = DSData.TimeperiodSelected(timespan);
                if (this.StartTime == start && this.EndTime == end)
                {
                    this.Timespan = timespan;
                    break;
                }
            }
            if (this.Timespan == "Custom" && this.Filter != null)
                this.Filter.DefaultTime = false;
            else if (this.Filter != null)
                this.Filter.DefaultTime = true;
        }

        public string GenHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Mode);
            sb.Append(Interest);
            sb.Append(Versus);
            sb.Append(StartTime.ToString("yyyyMMdd"));
            if (EndTime != DateTime.Today)
                sb.Append(EndTime.ToString("yyyyMMdd"));
            sb.Append(Player);
            return sb.ToString();
        }

        public string GenCountHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Interest);
            sb.Append(Versus);
            sb.Append(StartTime.ToString("yyyyMMdd"));
            if (EndTime != DateTime.Today)
                sb.Append(EndTime.ToString("yyyyMMdd"));
            sb.Append(Player);
            return sb.ToString();
        }
    }
}
