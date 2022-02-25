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
        public Player Author { get; internal set; }

        /// <summary>
        /// Slayer (<c>Null</c> if still ongoing).
        /// </summary>
        public Player Slayer { get; internal set; }

        /// <summary>
        /// Consecutive times while standing (one in most case).
        /// </summary>
        public IReadOnlyCollection<long> Times => _times;

        /// <summary>
        /// Number of days while standing; <c>Null if not computed yet</c>.
        /// </summary>
        public int? Days { get; private set; }

        internal Standing(long time)
        {
            _times = new List<long>
            {
                time
            };
        }

        internal void AddTime(long time)
        {
            if (!_times.Contains(time))
                _times.Add(time);
        }

        /// <summary>
        /// Sets <see cref="Days"/>.
        /// </summary>
        /// <param name="dateIfNull">End date if ongoing.</param>
        /// <returns>The instance.</returns>
        public Standing WithDays(DateTime dateIfNull)
        {
            Days = (int)(EndDate.GetValueOrDefault(dateIfNull) - StartDate).TotalDays;
            return this;
        }
    }
}
