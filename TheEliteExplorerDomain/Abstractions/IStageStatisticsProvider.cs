using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Stage statistics provider interface.
    /// </summary>
    public interface IStageStatisticsProvider
    {
        /// <summary>
        /// Gets entries count, and some related statistics, for every stage of the specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="startDate">Start date inclusive.</param>
        /// <param name="endDate">End date exclusive.</param>
        /// <param name="levelDetails"><c>True</c> to get detailed datas for each <see cref="Level"/>.</param>
        /// <param name="globalStartDate">Global start date for <see cref="StageEntryCount.TotalEntriesCount"/> (inclusive).</param>
        /// <param name="globalEndDate">Global end date for <see cref="StageEntryCount.TotalEntriesCount"/> (exclusive).</param>
        /// <returns>Collection of <see cref="StageEntryCount"/>; one per stage (and level if <paramref name="levelDetails"/>) in the game.</returns>
        Task<IReadOnlyCollection<StageEntryCount>> GetStagesEntriesCount(Game game, DateTime startDate, DateTime endDate, bool levelDetails, DateTime? globalStartDate, DateTime? globalEndDate);
    }
}
