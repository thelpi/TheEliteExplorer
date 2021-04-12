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
        /// <param name="full"><c>True</c> to get details for each ranking entry.</param>
        /// <returns>
        /// Collection of <see cref="RankingEntryLight"/>;
        /// sorted by <see cref="RankingEntryLight.Points"/> descending.
        /// </returns>
        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntries(Game game, DateTime rankingDate, bool full);

        /// <summary>
        /// Rebuilds the ranking history for a single stage and level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <returns>Nothing.</returns>
        Task RebuildRankingHistory(Stage stage, Level level);

        /// <summary>
        /// Rebuilds the ranking history for a full game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        Task RebuildRankingHistory(Game game);
    }
}
