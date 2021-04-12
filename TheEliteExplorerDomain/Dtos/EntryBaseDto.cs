using System;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Time entry base DTO.
    /// </summary>
    public class EntryBaseDto
    {
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; set; }
        /// <summary>
        /// Level.
        /// </summary>
        public Level Level { get; set; }
        /// <summary>
        /// Entry time, in seconds.
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// Entry date.
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// Engine.
        /// </summary>
        public Engine? Engine { get; set; }
    }
}
