using System;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a time entry
    /// </summary>
    public class Entry
    {
        /// <summary>
        /// Date.
        /// </summary>
        public DateTime? Date { get; }
        /// <summary>
        /// Time.
        /// </summary>
        public long Time { get; }
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; }

        internal Entry(Dtos.EntryDto entry)
        {
            Date = entry.Date;
            Time = entry.Time;
            PlayerId = entry.PlayerId;
        }
    }
}
