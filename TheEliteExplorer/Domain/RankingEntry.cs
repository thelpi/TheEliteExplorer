using System.Collections.Generic;

namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Represents a ranking entry.
    /// </summary>
    public class RankingEntry
    {
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; }
        /// <summary>
        /// Player name (surname).
        /// </summary>
        public string PlayerName { get; }
        /// <summary>
        /// Points.
        /// </summary>
        public int Points { get; }
        /// <summary>
        /// Count of untied world records.
        /// </summary>
        public int UntiedRecordsCount { get; }
        /// <summary>
        /// Count of world records.
        /// </summary>
        public int RecordsCount { get; }
        /// <summary>
        /// Points by <see cref="Level"/>.
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelPoints { get; }
        /// <summary>
        /// Count of untied world records by <see cref="Level"/>.
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelUntiedRecordsCount { get; }
        /// <summary>
        /// Count of world records by <see cref="Level"/>.
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelRecordsCount { get; }
    }
}
