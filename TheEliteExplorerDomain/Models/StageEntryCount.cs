using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Entries count for a stage on a specific period
    /// </summary>
    public class StageEntryCount
    {
        /// <summary>
        /// The level (<c>Null</c> for every level).
        /// </summary>
        public Level? Level { get; set; }
        /// <summary>
        /// The stage
        /// </summary>
        public Stage Stage { get; set; }
        /// <summary>
        /// Entries count in the period.
        /// </summary>
        public int PeriodEntriesCount { get; set; }
        /// <summary>
        /// Entries count overall.
        /// </summary>
        public int TotalEntriesCount { get; set; }
        /// <summary>
        /// Entries count across all stages in the period.
        /// </summary>
        public int AllStagesEntriesCount { get; set; }
        /// <summary>
        /// Considered period start (inclusive).
        /// </summary>
        public System.DateTime StartDate { get; set; }
        /// <summary>
        /// Considered period end (exclusive).
        /// </summary>
        public System.DateTime EndDate { get; set; }
    }
}
