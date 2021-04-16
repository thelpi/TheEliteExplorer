using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Stage all-time leaderboard.
    /// </summary>
    public class StageAllTimeLeaderboard
    {
        private const int DefaultLimit = 50;

        private readonly List<AllTimeLeaderboardEntry> _entries = new List<AllTimeLeaderboardEntry>();
        private readonly int _limit;

        /// <summary>
        /// Leaderboard by untied WR count.
        /// </summary>
        public IReadOnlyCollection<AllTimeLeaderboardEntry> UntiedsCountLeaderboard
        {
            get
            {
                return OrderBy(e => e.UntiedsCount);
            }
        }

        /// <summary>
        /// Leaderboard by tied WR count.
        /// </summary>
        public IReadOnlyCollection<AllTimeLeaderboardEntry> TiedsCountLeaderboard
        {
            get
            {
                return OrderBy(e => e.TiedsCount);
            }
        }

        /// <summary>
        /// Leaderboard by days count with a untied WR.
        /// </summary>
        public IReadOnlyCollection<AllTimeLeaderboardEntry> DaysUntiedLeaderboard
        {
            get
            {
                return OrderBy(e => e.DaysUntied);
            }
        }

        /// <summary>
        /// Leaderboard by days count with a tied WR.
        /// </summary>
        public IReadOnlyCollection<AllTimeLeaderboardEntry> DaysTiedLeaderboard
        {
            get
            {
                return OrderBy(e => e.DaysTied);
            }
        }

        internal StageAllTimeLeaderboard(int limit)
        {
            _limit = limit <= 0 ? DefaultLimit : _limit;
        }

        internal void SetEntry(Dtos.WrDto wr, IDictionary<long, Dtos.PlayerDto> players)
        {
            var playerEntry = _entries.SingleOrDefault(e => e.PlayerId == wr.PlayerId);
            if (playerEntry == null)
            {
                playerEntry = new AllTimeLeaderboardEntry(wr, players);
                _entries.Add(playerEntry);
            }
            else
            {
                playerEntry.SetNewWr(wr);
            }
            _entries.ForEach(e =>
            {
                if (e != playerEntry)
                {
                    e.SetNewWr(wr);
                }
            });
        }

        internal void SetTodayForEntries()
        {
            _entries.ForEach(e =>
            {
                e.SetToday();
            });
        }

        private List<AllTimeLeaderboardEntry> OrderBy(Func<AllTimeLeaderboardEntry, object> keySelector)
        {
            return _entries
                .OrderByDescending(keySelector)
                .ThenBy(e => e.PlayerName)
                .Take(_limit)
                .ToList();
        }
    }

    /// <summary>
    /// Represents a leaderboard entry for a player on a stage.
    /// </summary>
    public class AllTimeLeaderboardEntry
    {
        private Dtos.WrDto _wr;

        internal long PlayerId { get; }

        /// <summary>
        /// Player name.
        /// </summary>
        public string PlayerName { get; }
        /// <summary>
        /// Untied WR count.
        /// </summary>
        public int UntiedsCount { get; private set; }
        /// <summary>
        /// Tied WR count.
        /// </summary>
        public int TiedsCount { get; private set; }
        /// <summary>
        /// Untied WR days count.
        /// </summary>
        public int DaysUntied { get; private set; }
        /// <summary>
        /// Tied WR days count.
        /// </summary>
        public int DaysTied { get; private set; }

        internal AllTimeLeaderboardEntry(Dtos.WrDto wr, IDictionary<long, Dtos.PlayerDto> players)
        {
            PlayerId = wr.PlayerId;
            PlayerName = players[PlayerId].RealName;

            UpgradeWr(wr);
        }

        internal void SetNewWr(Dtos.WrDto wr)
        {
            if (wr.PlayerId == PlayerId)
            {
                if (!wr.Untied && _wr != null)
                {
                    throw new NotSupportedException("Should not happen.");
                }
                UpgradeWr(wr);
            }
            else if (_wr != null)
            {
                if (wr.Time < _wr.Time)
                {
                    SetDays(wr.Date);
                    _wr = null;
                }
                else
                {
                    if (_wr.Untied)
                    {
                        DaysUntied += (int)(wr.Date - _wr.Date).TotalDays;
                    }
                    _wr.Untied = false;
                }
            }
        }

        internal void SetToday()
        {
            if (_wr != null)
            {
                SetDays(ServiceProviderAccessor.ClockProvider.Now);
                _wr = null;
            }
        }

        private void SetDays(DateTime date)
        {
            var days = (int)(date - _wr.Date).TotalDays;
            if (_wr.Untied)
            {
                DaysUntied += days;
            }
            DaysTied += days;
        }

        private void UpgradeWr(Dtos.WrDto wr)
        {
            _wr = wr;
            UntiedsCount += wr.Untied ? 1 : 0;
            TiedsCount += 1;
        }
    }
}
