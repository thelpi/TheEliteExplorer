using System;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a time entry to process from the web datas.
    /// </summary>
    public class EntryWebDto : EntryBaseDto
    {
        /// <summary>
        /// Player URL name.
        /// </summary>
        public string PlayerUrlName { get; set; }

        internal EntryDto ToEntry(long playerId)
        {
            return new EntryDto
            {
                PlayerId = playerId,
                StageId = StageId,
                LevelId = LevelId,
                Date = Date,
                Time = Time,
                SystemId = SystemId
            };
        }
    }
}
