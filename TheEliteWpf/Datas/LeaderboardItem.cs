using System;

namespace TheEliteWpf.Datas
{
    public class LeaderboardItem
    {
        public Player Player { get; set; }
        public int Points { get; set; }
        public DateTime LatestTime { get; set; }
        public int Rank { get; set; }
    }
}
