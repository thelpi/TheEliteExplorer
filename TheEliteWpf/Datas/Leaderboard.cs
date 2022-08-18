using System;
using System.Collections.Generic;

namespace TheEliteWpf.Datas
{
    public class Leaderboard
    {
        public IReadOnlyCollection<LeaderboardItem> Items { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public Stage Stage { get; set; }
    }
}
