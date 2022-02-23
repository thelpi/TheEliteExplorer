using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Statistics provider interface.
    /// </summary>
    public interface IStatisticsProvider
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
        Task<IReadOnlyCollection<StageEntryCount>> GetStagesEntriesCountAsync(Game game, DateTime startDate, DateTime endDate, bool levelDetails, DateTime? globalStartDate, DateTime? globalEndDate);

        /// <summary>
        /// Gets sweeps.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied">Untied y/n.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <param name="stage">Stage</param>
        /// <returns>Collection of sweeps</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startDate"/> is greater than <paramref name="endDate"/>.</exception>
        Task<IReadOnlyCollection<StageSweep>> GetSweepsAsync(
            Game game,
            bool untied,
            DateTime? startDate,
            DateTime? endDate,
            Stage? stage);

        /// <summary>
        /// Gets, at a specified date, the latest WR entry for every stage and level.
        /// </summary>
        /// <param name="game">Game</param>
        /// <param name="date">Date</param>
        /// <returns>Last WR entry by stage an level</returns>
        Task<Dictionary<Stage, Dictionary<Level, (Dtos.EntryDto, bool)>>> GetLastTiedWrsAsync(
            Game game,
            DateTime date);

        /// <summary>
        /// Computes and gets the full ranking at the specified date.
        /// </summary>
        /// <param name="request">Ranking request.</param>
        /// <returns>
        /// Collection of <see cref="RankingEntryLight"/>;
        /// sorted by <see cref="RankingEntryLight.Points"/> descending.
        /// </returns>
        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(RankingRequest request);

        /// <summary>
        /// Gets ambiguous world records; ie same date between two milestones.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untiedSlayAmbiguous">
        /// <c>True</c> to check between untied (1th) and slay (2nd);
        /// otherwise checks between slay (2nd) and third.
        /// </param>
        /// <returns>Collection of ambiguous world records.</returns>
        Task<IReadOnlyCollection<WrBase>> GetAmbiguousWorldRecordsAsync(
            Game game,
            bool untiedSlayAmbiguous);
        /*/// <summary>
        /// Generates a permanent typed ranking for each day between two dates.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="fromDate">Start date.</param>
        /// <param name="toDate">End date (<c>Null</c> for current).</param>
        /// <param name="rankingTypeId">Ranking type identifier.</param>
        /// <returns>Nothing.</returns>
        Task GeneratePermanentRankingsBetweenDatesAsync(
            Game game,
            DateTime fromDate,
            DateTime? toDate,
            long rankingTypeId);*/
    }
}
