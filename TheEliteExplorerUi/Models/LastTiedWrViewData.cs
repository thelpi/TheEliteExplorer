using System;
using System.Collections.Generic;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerUi.Models
{
    public class LastTiedWrViewData
    {
        public List<LastTiedWrStageItemData> StageDetails { get; set; }
        public List<LastTiedWrLevelItemData> TopDetails { get; set; }
    }

    public class LastTiedWrStageItemData
    {
        public string Name { get; set; }
        public string Image { get; set; }

        public LastTiedWrLevelItemData EasyData { get; set; }
        public LastTiedWrLevelItemData MediumData { get; set; }
        public LastTiedWrLevelItemData HardData { get; set; }
    }

    public class LastTiedWrLevelItemData
    {
        public string PlayerInitials { get; set; }
        public string PlayerName { get; set; }
        public string PlayerColor { get; set; }
        public TimeSpan EntryTime { get; set; }
        public DateTime EntryDate { get; set; }
        public int EntryDays { get; set; }
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public char Untied { get; set; }
    }
}
