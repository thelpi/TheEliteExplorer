using System;
using System.Collections.Generic;

namespace TheEliteExplorerDomain.Models
{
    public class DateCountWr
    {
        public DateTime Date { get; set; }
        public int UntiedsCount { get; set; }
        public int TiedsCount { get; set; }
        public List<long> UntiedPlayers { get; set; }
        public List<long> TiedPlayers { get; set; }
    }
}
