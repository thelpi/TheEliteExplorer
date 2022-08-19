using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Stage statistics controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    public class StatisticsController : Controller
    {
        private readonly IStatisticsProvider _statisticsProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="statisticsProvider">Instance of <see cref="IStatisticsProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="statisticsProvider"/> is <c>Null</c>.</exception>
        public StatisticsController(IStatisticsProvider statisticsProvider)
        {
            _statisticsProvider = statisticsProvider ?? throw new ArgumentNullException(nameof(statisticsProvider));
        }

        /// <summary>
        /// Gets rankings for the current date or a specified date.
        /// </summary>
        /// <param name="game">The requested game.</param>
        /// <param name="date">String representation of date; empty or <c>Null</c> for current date.</param>
        /// <param name="page">page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <param name="full"><c>True</c> to get full details for each ranking entry.</param>
        /// <param name="simulatedPlayerId">A player identifier, that we want the latest times instead of <paramref name="date"/> times.</param>
        /// <returns>Paginated collection of <see cref="RankingEntry"/>.</returns>
        [HttpGet("games/{game}/rankings/{date}")]
        [ProducesResponseType(typeof(PaginatedCollection<RankingEntryLight>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PaginatedCollection<RankingEntryLight>>> GetRankingAsync(
            [FromRoute] Game game,
            [FromRoute] DateTime? date,
            [FromQuery] int page,
            [FromQuery] int count,
            [FromQuery] bool full,
            [FromQuery] long? simulatedPlayerId)
        {
            var request = new RankingRequest
            {
                Game = game,
                FullDetails = full
            };

            var now = ServiceProviderAccessor.ClockProvider.Now;
            if (simulatedPlayerId.HasValue && date.HasValue)
            {
                request.PlayerVsLegacy = (simulatedPlayerId.Value, date.Value);
                request.RankingDate = now;
            }
            else
            {
                request.RankingDate = date ?? now;
            }

            var rankingEntries = await _statisticsProvider
                .GetRankingEntriesAsync(request)
                .ConfigureAwait(false);

            return Ok(PaginatedCollection<RankingEntryLight>.CreateInstance(rankingEntries, page, count));
        }

        /// <summary>
        /// Gets sweeps.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied">Is untied y/n.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>Collection of untied sweeps.</returns>
        [HttpGet("games/{game}/sweeps")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StageSweep>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<StageSweep>>> GetSweepsAsync(
            [FromRoute] Game game,
            [FromQuery][Required] bool untied,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var sweeps = await _statisticsProvider
                .GetSweepsAsync(game, untied, startDate, endDate, null)
                .ConfigureAwait(false);

            return Ok(sweeps);
        }

        /// <summary>
        /// Gets ambiguous world records.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untiedSlayAmbiguous">
        /// <c>True</c> to check between untied (1th) and slay (2nd);
        /// otherwise checks between slay (2nd) and third.
        /// </param>
        /// <returns>Collection of untied sweeps.</returns>
        [HttpGet("games/{game}/ambiguous-world-records")]
        [ProducesResponseType(typeof(IReadOnlyCollection<WrBase>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<WrBase>>> GetAmbiguousWorldRecordsAsync(
            [FromRoute] Game game,
            [FromQuery][Required] bool untiedSlayAmbiguous)
        {
            var wrs = await _statisticsProvider
                .GetAmbiguousWorldRecordsAsync(game, untiedSlayAmbiguous)
                .ConfigureAwait(false);

            return Ok(wrs);
        }

        /// <summary>
        /// Gets longest standing world records.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="endDate">End date to consider.</param>
        /// <param name="standingType">Type of standing.</param>
        /// <param name="stillOngoing"></param>
        /// <param name="count">Number of results expected.</param>
        /// <param name="engine">Engine filter.</param>
        /// <returns>Collection of standing world records.</returns>
        [HttpGet("games/{game}/longest-standings")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Standing>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Standing>>> GetLongestStandingsAsync(
            [FromRoute] Game game,
            [FromQuery][Required] StandingType standingType,
            [FromQuery] bool? stillOngoing,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? count,
            [FromQuery] Engine? engine)
        {
            var standings = await _statisticsProvider
                .GetLongestStandingsAsync(game, endDate, standingType, stillOngoing, engine)
                .ConfigureAwait(false);

            return Ok(standings.Take(count ?? 100).ToList());
        }

        [HttpGet("stages/{stage}/leaderboard-history")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StageLeaderboard>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<StageLeaderboard>>> GetStageLeaderboardHistoryAsync(
            [FromRoute] Stage stage,
            [FromQuery] LeaderboardGroupOptions groupOption,
            [FromQuery] int daysStep)
        {
            var datas = await _statisticsProvider
                .GetStageLeaderboardHistoryAsync(stage, groupOption, daysStep)
                .ConfigureAwait(false);

            return Ok(datas);
        }
    }
}
