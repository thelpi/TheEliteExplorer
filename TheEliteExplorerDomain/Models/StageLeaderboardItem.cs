using System;

namespace TheEliteExplorerDomain.Models
{
    public class StageLeaderboardItem
    {
        public Player Player { get; set; }
        public int Points { get; set; }
        public DateTime LatestTime { get; set; }
        public int Rank { get; set; }
    }
}
