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
        /// Computes and inserts in database rankings, by level and by stage, for each missing day.
        /// Days without entries are skipped.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        Task GenerateRankings(Game game);
    }
}
