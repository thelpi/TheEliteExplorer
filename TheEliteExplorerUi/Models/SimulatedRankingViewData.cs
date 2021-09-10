using System;
using System.Collections.Generic;

namespace TheEliteExplorerUi.Models
{
    public class SimulatedRankingViewData
    { 
        public List<TimeRankingItemData> TimeRankingEntries { get; set; }
        public List<PointsRankingItemData> PointsRankingEntries { get; set; }
        public List<StageWorldRecordItemData> StageWorldRecordEntries { get; set; }
        public TimeSpan CombinedTime { get; set; }
    }
}
