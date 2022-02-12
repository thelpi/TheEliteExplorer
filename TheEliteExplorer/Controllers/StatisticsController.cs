using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        /// Gets statistics about entries count for a specified game across all stages and levels.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <param name="levelDetails">With or without details by level.</param>
        /// <param name="globalStartDate">Start date for <see cref="StageEntryCount.TotalEntriesCount"/> (inclusive).</param>
        /// <param name="globalEndDate">End date for <see cref="StageEntryCount.TotalEntriesCount"/> (exclusive).</param>
        /// <returns>Collection of <see cref="StageEntryCount"/>.</returns>
        [HttpGet("games/{game}/entries-count")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StageEntryCount>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<StageEntryCount>>> GetLongestStandingWrsAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] DateTime? globalStartDate,
            [FromQuery] DateTime? globalEndDate,
            [FromQuery] bool levelDetails)
        {
            var results = await _statisticsProvider
                .GetStagesEntriesCountAsync(game, startDate, endDate, levelDetails, globalStartDate, globalEndDate)
                .ConfigureAwait(false);

            return Ok(results);
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
    }
}
