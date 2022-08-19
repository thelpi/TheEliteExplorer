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
        /// Gets every player.
        /// </summary>
        /// <returns>Collection of <see cref="Player"/>.</returns>
        Task<IReadOnlyCollection<Player>> GetPlayersAsync();

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

        /// <summary>
        /// Gets longest standing times.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="endDate">End date ton consider; <c>Null</c> for now.</param>
        /// <param name="standingType">Type of standing request.</param>
        /// <param name="stillOngoing">Still ongoing; or not; or both (<c>Null</c>).</param>
        /// <param name="engine">Engine filter.</param>
        /// <returns>Ordered collection of <see cref="Standing"/>.</returns>
        Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine);

        Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(Stage stage, LeaderboardGroupOptions groupOption, int daysStep);
    }
}
