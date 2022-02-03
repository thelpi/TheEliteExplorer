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
        /// <param name="game">Game.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <param name="full"><c>True</c> to get details for each ranking entry.</param>
        /// <param name="simulatedPlayerId">Optionnal player identifier for which the ranking uses his latest entries instead of <paramref name="rankingDate"/> entries.</param>
        /// <param name="monthsOfFreshTimes">Optionnal; if specified, ignores entries older than <paramref name="rankingDate"/> minus this number of months.</param>
        /// <param name="skipStages">Stages to skip while computing the ranking.</param>
        /// <param name="excludeWinners"><c>True</c> or <c>Null</c> to exclude players with WR or untied WR.</param>
        /// <returns>
        /// Collection of <see cref="RankingEntryLight"/>;
        /// sorted by <see cref="RankingEntryLight.Points"/> descending.
        /// </returns>
        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            Game game,
            DateTime rankingDate,
            bool full,
            long? simulatedPlayerId = null,
            int? monthsOfFreshTimes = null,
            Stage[] skipStages = null,
            bool? excludeWinners = false);

        /// <summary>
        /// Gets the full ranking at any date for a game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <param name="noDateEntryRankingRule">No date entry ranking rule.</param>
        /// <returns>Full ranking.</returns>
        Task<IReadOnlyCollection<GameRank>> GetGameRankingAsync(
            Game game,
            DateTime rankingDate,
            NoDateEntryRankingRule noDateEntryRankingRule);
    }
}
