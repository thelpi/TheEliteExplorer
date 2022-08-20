using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    public interface IStatisticsProvider
    {
        Task<IReadOnlyCollection<Player>> GetPlayersAsync();

        Task<IReadOnlyCollection<StageSweep>> GetSweepsAsync(
            Game game,
            bool untied,
            DateTime? startDate,
            DateTime? endDate,
            Stage? stage);

        Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            RankingRequest request);

        Task<IReadOnlyCollection<WrBase>> GetAmbiguousWorldRecordsAsync(
            Game game,
            bool untiedSlayAmbiguous);

        Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing,
            Engine? engine);

        Task<IReadOnlyCollection<StageLeaderboard>> GetStageLeaderboardHistoryAsync(
            Stage stage,
            LeaderboardGroupOptions groupOption,
            int daysStep);
    }
}
