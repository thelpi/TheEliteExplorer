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
    /// Ranking controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    public class RankingController : Controller
    {
        private readonly IWorldRecordProvider _wrProvider;
        private readonly IRankingProvider _rankingProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="wrProvider">Instance of <see cref="IWorldRecordProvider"/>.</param>
        /// <param name="rankingProvider">Instance of <see cref="IRankingProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="wrProvider"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rankingProvider"/> is <c>Null</c>.</exception>
        public RankingController(
            IWorldRecordProvider wrProvider,
            IRankingProvider rankingProvider)
        {
            _wrProvider = wrProvider ?? throw new ArgumentNullException(nameof(wrProvider));
            _rankingProvider = rankingProvider ?? throw new ArgumentNullException(nameof(rankingProvider));
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
        public async Task<ActionResult<PaginatedCollection<RankingEntryLight>>> GetRanking(
            [FromRoute] Game game,
            [FromRoute] DateTime? date,
            [FromQuery] int page,
            [FromQuery] int count,
            [FromQuery] bool full,
            [FromQuery] long? simulatedPlayerId)
        {
            var rankingEntries = await _rankingProvider
                .GetRankingEntries(game, date ?? ServiceProviderAccessor.ClockProvider.Now, full, simulatedPlayerId)
                .ConfigureAwait(false);

            return Ok(PaginatedCollection<RankingEntryLight>.CreateInstance(rankingEntries, page, count));
        }

        /// <summary>
        /// Builds or rebuilds the ranking history for a single stage and a single level.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <param name="level">The level.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("stages/{stageId}/levels/{level}/rankings")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> RebuildRankingHistory(
            [FromRoute] Stage stage,
            [FromRoute] Level level)
        {
            await _rankingProvider
                .RebuildRankingHistory(stage, level)
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Builds or rebuilds the ranking history for a full game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("games/{game}/rankings")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> RebuildRankingHistory(
            [FromRoute] Game game)
        {
            await _rankingProvider
                .RebuildRankingHistory(game)
                .ConfigureAwait(false);

            return NoContent();
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
        public async Task<ActionResult<IReadOnlyCollection<StageSweep>>> GetSweeps(
            [FromRoute] Game game,
            [FromQuery][Required] bool untied,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var sweeps = await _wrProvider
                .GetSweeps(game, untied, startDate, endDate, null)
                .ConfigureAwait(false);

            return Ok(sweeps);
        }
    }
}
