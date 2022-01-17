using System;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain.Models
{
    public class PlayerProgression
    {
        public PlayerDto Player { get; set; }

        public DateTime FirstDate { get; set; }

        public DateTime ReachTargetDate { get; set; }

        public int Days => (int)(ReachTargetDate - FirstDate).TotalDays;
    }
}
