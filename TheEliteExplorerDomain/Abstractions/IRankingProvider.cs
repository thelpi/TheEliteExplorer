using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Ranking provider interface.
    /// </summary>
    public interface IRankingProvider
    {
        /// <summary>
        /// Computes and gets the full ranking at the specified date.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <returns>
        /// Collection of <see cref="RankingEntry"/>;
        /// sorted by <see cref="RankingEntry.Points"/> descending.
        /// </returns>
        Task<IReadOnlyCollection<RankingEntry>> GetRankingEntries(Game game, DateTime rankingDate);

        /// <summary>
        /// Rebuilds the ranking history for a single stage and level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stage"/> is <c>Null</c>.</exception>
        Task RebuildRankingHistory(Stage stage, Level level);
    }
}
