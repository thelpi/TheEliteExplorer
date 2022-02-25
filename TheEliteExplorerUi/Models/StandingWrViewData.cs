using System;
using System.Collections.Generic;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerUi.Models
{
    public class StandingWrViewData
    {
        public List<StandingWrLevelItemData> TopDetails { get; set; }
    }

    public class StandingWrLevelItemData
    {
        public string PlayerName { get; set; }
        public string PlayerColor { get; set; }
        public string SlayerName { get; set; }
        public string SlayerColor { get; set; }
        public List<TimeSpan> EntryTimes { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int EntryDays { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
    }
}
