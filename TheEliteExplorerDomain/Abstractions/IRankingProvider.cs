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
        /// <param name="simulatedPlayerId">Optionnal player identifier for which the ranking uses his latest entries instead of <paramref name="rankingDate"/> entries.</param>
        /// <param name="monthsOfFreshTimes">Optionnal; if specified, ignores entries older than <paramref name="rankingDate"/> minus this number of months.</param>
        /// <param name="skipStages">Stages to skip while computing the ranking.</param>
        /// <returns>
        /// Collection of <see cref="RankingEntryLight"/>;
        /// sorted by <see cref="RankingEntryLight.Points"/> descending.
        /// </returns>
        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntries(
            Game game,
            DateTime rankingDate,
            bool full,
            long? simulatedPlayerId = null,
            int? monthsOfFreshTimes = null,
            Stage[] skipStages = null,
            bool excludeWinners = false);

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
