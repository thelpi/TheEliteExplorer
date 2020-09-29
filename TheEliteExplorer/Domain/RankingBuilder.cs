using System;
using System.Collections.Generic;
using TheEliteExplorer.Infrastructure.Dtos;

namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Builds a ranking.
    /// </summary>
    public class RankingBuilder
    {
        private readonly IReadOnlyCollection<EntryDto> _entries;
        private readonly IReadOnlyCollection<PlayerDto> _players;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entries">Collection of <see cref="EntryDto"/>.</param>
        /// <param name="players">Collection of <see cref="PlayerDto"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="players"/> is <c>Null</c>.</exception>
        public RankingBuilder(IReadOnlyCollection<EntryDto> entries, IReadOnlyCollection<PlayerDto> players)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _players = players ?? throw new ArgumentNullException(nameof(players));
        }

        /// <summary>
        /// Computes and gets the ranking.
        /// </summary>
        /// <returns>
        /// Collection of <see cref="RankingEntry"/>;
        /// sorted by <see cref="RankingEntry.Points"/> descending.
        /// </returns>
        public IReadOnlyCollection<RankingEntry> GetRankingEntries()
        {
            throw new NotImplementedException();
        }
    }
}
