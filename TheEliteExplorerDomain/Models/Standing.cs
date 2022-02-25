using System;
using System.Collections.Generic;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a standing while being untied or unslayed.
    /// </summary>
    public class Standing
    {
        private DateTime _startDate;
        private DateTime? _endDate;
        private List<long> _times;

        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; internal set; }

        /// <summary>
        /// Level.
        /// </summary>
        public Level Level { get; internal set; }

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
        public PlayerDto Author { get; internal set; }

        /// <summary>
        /// Slayer (<c>Null</c> if still ongoing).
        /// </summary>
        public PlayerDto Slayer { get; internal set; }

        /// <summary>
        /// Consecutive times while standing (one in most case).
        /// </summary>
        public IReadOnlyCollection<long> Times => _times;

        internal Standing WithTime(long time)
        {
            _times.Add(time);
            return this;
        }

        /// <summary>
        /// Gets the number of days while standing.
        /// </summary>
        /// <param name="dateIfNull">End date if ongoing.</param>
        /// <returns>Number of days.</returns>
        public int GetDays(DateTime dateIfNull)
        {
            return (int)(EndDate.GetValueOrDefault(dateIfNull) - StartDate).TotalDays;
        }
    }
}
