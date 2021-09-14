using System;
using System.Collections.Generic;

namespace TheEliteExplorerUi.Models
{
    public class StageWorldRecordItemData
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Image { get; set; }
        public TimeSpan EasyTime { get; set; }
        public TimeSpan MediumTime { get; set; }
        public TimeSpan HardTime { get; set; }
        public List<(string, string, string)> EasyColoredInitials { get; set; }
        public List<(string, string, string)> MediumColoredInitials { get; set; }
        public List<(string, string, string)> HardColoredInitials { get; set; }
    }
}
