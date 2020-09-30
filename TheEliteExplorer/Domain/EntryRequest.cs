using System;

namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Represents a time entry to process.
    /// </summary>
    public class EntryRequest
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
        /// Player URL name.
        /// </summary>
        public string PlayerUrlName { get; set; }
        /// <summary>
        /// Time.
        /// </summary>
        public long? Time { get; set; }
        /// <summary>
        /// Date.
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// Engine identifier.
        /// </summary>
        public long? EngineId { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Concat(StageId, ", ", LevelId, ", ", PlayerUrlName, ", ", Date, ", ", EngineId);
        }
    }
}
