using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Ranking request parameters
    /// </summary>
    public class RankingRequest
    {
        private DateTime _rankingDate;
        private DateTime? _rankingStartDate;
        private (long, DateTime)? _playerVsLegacy;
        private List<Stage> _skipStages = new List<Stage>();

        /// <summary>
        /// Game.
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        /// Ranking date.
        /// </summary>
        public DateTime RankingDate
        {
            get { return _rankingDate; }
            set { _rankingDate = value.Date; }
        }

        /// <summary>
        /// Start date to consider entries (optionnal).
        /// </summary>
        public DateTime? RankingStartDate
        {
            get { return _rankingStartDate; }
            set { _rankingStartDate = value?.Date; }
        }

        /// <summary>
        /// Player at <see cref="RankingDate"/> versus everyone else at specified date (optionnal).
        /// </summary>
        public (long, DateTime)? PlayerVsLegacy
        {
            get { return _playerVsLegacy; }
            set
            {
                _playerVsLegacy = value.HasValue
                    ? (value.Value.Item1, value.Value.Item2.Date)
                    : default((long, DateTime)?);
            }
        }

        /// <summary>
        /// Stages to skip.
        /// </summary>
        public IReadOnlyCollection<Stage> SkipStages
        {
            get { return _skipStages; }
            set { _skipStages = value?.ToList() ?? _skipStages; }
        }

        /// <summary>
        /// Type of player to exclude (optionnal).
        /// </summary>
        public ExcludePlayerType? ExcludePlayer { get; set; }

        /// <summary>
        /// Get full details about ranking or not.
        /// </summary>
        public bool FullDetails { get; set; }

        // Full collection of players by identifier.
        internal IReadOnlyDictionary<long, PlayerDto> Players { get; set; }

        // Every entries (with valuated date) by stage and level.
        internal ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>> Entries { get; }
            = new ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>>();

        /// <summary>
        /// Enumeration of different cases of players to exclude from the ranking
        /// </summary>
        public enum ExcludePlayerType
        {
            /// <summary>
            /// Remove players with untied WR.
            /// </summary>
            HasUntied,
            /// <summary>
            /// Remove players with WR.
            /// </summary>
            HasWorldRecord
        }
    }
}
