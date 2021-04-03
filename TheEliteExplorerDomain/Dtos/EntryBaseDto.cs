using System;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Time entry base DTO.
    /// </summary>
    public class EntryBaseDto
    {
        /// <summary>
        /// Stage identifier.
        /// </summary>
        public long StageId { get; set; }
        /// <summary>
        /// Level identifier.
        /// </summary>
        public long LevelId { get; set; }
        /// <summary>
        /// Entry time, in seconds.
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// Entry date.
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// Engine/system identifier.
        /// </summary>
        public long? SystemId { get; set; }
    }
}
