using System;
using System.Collections.Generic;

namespace TheEliteWpf.Datas
{
    public class Standing
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Player Author { get; set; }
        public Player Slayer { get; set; }
        public IReadOnlyCollection<long> Times { get; set; }
        public int? Days { get; set; }
    }
}
