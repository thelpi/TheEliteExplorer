using System;
using System.Collections.Generic;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerUi.Models
{
    public class PlayerDetailsViewData
    {
        public string PlayerName { get; set; }
        public Game Game { get; set; }
        public int OverallPoints { get; set; }
        public int OverallRanking { get; set; }
        public TimeSpan OverallTime { get; set; }
        public int EasyPoints { get; set; }
        public TimeSpan EasyTime { get; set; }
        public int MediumPoints { get; set; }
        public TimeSpan MediumTime { get; set; }
        public int HardPoints { get; set; }
        public TimeSpan HardTime { get; set; }
        public List<PlayerStageDetailsItemData> DetailsByStage { get; set; }
    }

    public class PlayerStageDetailsItemData
    {
        public Stage Stage { get; set; }
        public string Image { get; set; }
        public int EasyPoints { get; set; }
        public int MediumPoints { get; set; }
        public int HardPoints { get; set; }
        public TimeSpan? EasyTime { get; set; }
        public TimeSpan? MediumTime { get; set; }
        public TimeSpan? HardTime { get; set; }
    }
}
