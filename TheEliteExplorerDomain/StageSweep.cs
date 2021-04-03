using System;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Represents a sweep on stage (untied or not).
    /// </summary>
    public class StageSweep
    {
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; set; }

        /// <summary>
        /// Start date.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date (exclusive).
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Player.
        /// </summary>
        public Player Player { get; set; }

        /// <summary>
        /// Player identifier.
        /// </summary>
        internal long PlayerId { get; set; }

        /// <summary>
        /// Days count.
        /// </summary>
        public int Days { get { return (int)(EndDate - StartDate).TotalDays; } }
    }
}
