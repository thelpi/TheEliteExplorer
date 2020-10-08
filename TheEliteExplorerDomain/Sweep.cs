using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Represents a sweep, when a player has the world record for each <see cref="Level"/> of a <see cref="Stage"/>.
    /// </summary>
    public class Sweep
    {
        private Dictionary<DateTime, int> _untiedCountChanges;

        /// <summary>
        /// Start date.
        /// </summary>
        public DateTime StartDate { get; }
        /// <summary>
        /// End date; <c>Null</c> in two cases: the instance is not readonly yet or the sweep is currently active.
        /// </summary>
        public DateTime? EndDate { get; private set; }
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; }
        /// <summary>
        /// Player.
        /// </summary>
        public Player Player { get; }
        /// <summary>
        /// Readonly; happens when <see cref="CloseSweep(DateTime?)"/> is called.
        /// </summary>
        public bool Readonly { get; private set; }
        /// <summary>
        /// History of untied count changes.
        /// </summary>
        public IReadOnlyDictionary<DateTime, int> UntiedCountChanges { get { return _untiedCountChanges; } }
        
        /// <summary>
        /// Inferred; days count.
        /// </summary>
        public int TotalDays
        {
            get
            {
                return (EndDate.GetValueOrDefault(ServiceProviderAccessor.ClockProvider.Now).Date - StartDate.Date).Days;
            }
        }
        /// <summary>
        /// Inferred; cumulative days count untied.
        /// </summary>
        public int TotalDaysUntied
        {
            get
            {
                int days = 0;
                DateTime? start = null;
                foreach (DateTime changeDate in UntiedCountChanges.Keys)
                {
                    if (UntiedCountChanges[changeDate] == SystemExtensions.Count<Level>())
                    {
                        start = changeDate;
                    }
                    else if (start.HasValue)
                    {
                        days += (changeDate - start.Value).Days;
                        start = null;
                    }
                }
                return days;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="stage"></param>
        /// <param name="startDate"></param>
        /// <param name="untiedCount"></param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="untiedCount"/> is out of range.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public Sweep(Player player, Stage stage, DateTime startDate, int untiedCount)
        {
            CheckUntiedCount(untiedCount);

            Player = player ?? throw new ArgumentNullException(nameof(player));
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            StartDate = startDate;
            _untiedCountChanges = new Dictionary<DateTime, int>
            {
                { StartDate, untiedCount }
            };
        }

        /// <summary>
        /// Closes the sweep.
        /// </summary>
        /// <param name="endDate">End date.</param>
        /// <exception cref="InvalidOperationException">The instance is already closed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="endDate"/> is prior to <see cref="StartDate"/>.</exception>
        public void CloseSweep(DateTime? endDate)
        {
            CheckReadonlyness();

            if (endDate < StartDate)
            {
                throw new ArgumentOutOfRangeException(nameof(endDate), endDate.Value, $"{nameof(endDate)} is prior to {nameof(StartDate)}.");
            }

            EndDate = endDate;
            Readonly = true;
        }

        /// <summary>
        /// Removes an untied to the count.
        /// </summary>
        /// <param name="date">Date of the change.</param>
        /// <exception cref="ArgumentOutOfRangeException">The new untied count is out of range.</exception>
        /// <exception cref="InvalidOperationException">The instance is already closed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="date"/>  is prior to the latest change.</exception>
        public void RemoveUntied(DateTime date)
        {
            AddChange(date, true);
        }

        /// <summary>
        /// Adds an untied to the count.
        /// </summary>
        /// <param name="date">Date of the change.</param>
        /// <exception cref="ArgumentOutOfRangeException">The new untied count is out of range.</exception>
        /// <exception cref="InvalidOperationException">The instance is already closed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="date"/>  is prior to the latest change.</exception>
        public void AddUntied(DateTime date)
        {
            AddChange(date, false);
        }

        private static void CheckUntiedCount(int untiedCount)
        {
            if (untiedCount < 0 || untiedCount > SystemExtensions.Count<Level>())
            {
                throw new ArgumentOutOfRangeException(nameof(untiedCount), untiedCount, $"{nameof(untiedCount)} is out of range.");
            }
        }

        private void CheckReadonlyness()
        {
            if (Readonly)
            {
                throw new InvalidOperationException("The instance is already closed.");
            }
        }

        private void AddChange(DateTime date, bool minus)
        {
            CheckReadonlyness();

            KeyValuePair<DateTime, int> kvp = _untiedCountChanges.Last();

            if (date < kvp.Key)
            {
                throw new ArgumentOutOfRangeException(nameof(date), date, $"{nameof(date)} is prior to the latest change.");
            }

            int newValue = kvp.Value + (minus ? -1 : 1);
            CheckUntiedCount(newValue);

            _untiedCountChanges.Add(date, newValue);
        }
    }
}
