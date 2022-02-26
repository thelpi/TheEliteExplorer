using System;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a sweeep (untied or unslay).
    /// </summary>
    public class Sweep
    {
        private DateTime _startDate;
        private DateTime? _endDate;

        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; internal set; }

        /// <summary>
        /// Start date.
        /// </summary>
        public DateTime StartDate { get { return _startDate; } internal set { _startDate = value.Date; } }

        /// <summary>
        /// End date (<c>Null</c> if still ongoing).
        /// </summary>
        public DateTime? EndDate { get { return _endDate; } internal set { _endDate = value?.Date; } }

        /// <summary>
        /// Author.
        /// </summary>
        public Player Author { get; internal set; }

        /// <summary>
        /// Number of days while sweep; <c>Null if not computed yet</c>.
        /// </summary>
        public int? Days { get; private set; }

        /// <summary>
        /// Sets <see cref="Days"/>.
        /// </summary>
        /// <param name="dateIfNull">End date if ongoing.</param>
        /// <returns>The instance.</returns>
        public Sweep WithDays(DateTime dateIfNull)
        {
            Days = (int)(EndDate.GetValueOrDefault(dateIfNull) - StartDate).TotalDays;
            return this;
        }
    }
}
