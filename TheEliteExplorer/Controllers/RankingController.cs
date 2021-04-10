﻿using System;
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
        private readonly IStageSweepProvider _stageSweepProvider;
        private readonly IRankingProvider _rankingProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stageSweepProvider">Instance of <see cref="IStageSweepProvider"/>.</param>
        /// <param name="rankingProvider">Instance of <see cref="IRankingProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stageSweepProvider"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rankingProvider"/> is <c>Null</c>.</exception>
        public RankingController(
            IStageSweepProvider stageSweepProvider,
            IRankingProvider rankingProvider)
        {
            _stageSweepProvider = stageSweepProvider ?? throw new ArgumentNullException(nameof(stageSweepProvider));
            _rankingProvider = rankingProvider ?? throw new ArgumentNullException(nameof(rankingProvider));
        }

        /// <summary>
        /// Gets rankings for the current date or a specified date.
        /// </summary>
        /// <param name="game">The requested game.</param>
        /// <param name="date">String representation of date; empty or <c>Null</c> for current date.</param>
        /// <param name="page">page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <returns>Paginated collection of <see cref="RankingEntry"/>.</returns>
        [HttpGet("games/{game}/rankings/{date}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult<PaginatedCollection<RankingEntry>>> GetRankingAsync(
            [FromRoute] Game game,
            [FromRoute] DateTime? date,
            [FromQuery] int page,
            [FromQuery] int count)
        {
            var rankingEntries = await _rankingProvider
                .GetRankingEntries(game, date ?? ServiceProviderAccessor.ClockProvider.Now)
                .ConfigureAwait(false);

            return Ok(PaginatedCollection<RankingEntry>.CreateInstance(rankingEntries, page, count));
        }

        /// <summary>
        /// Builds or rebuilds the ranking history for a single stage and a single level.
        /// </summary>
        /// <param name="stageId">The stage identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("stages/{stageId}/levels/{level}/rankings")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RebuildRankingHistory([FromRoute] long stageId, [FromRoute] Level level)
        {
            var stage = Stage.Get(stageId);
            if (stage == null)
            {
                return BadRequest();
            }

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
        public async Task<IActionResult> RebuildRankingHistory([FromRoute] Game game)
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
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<StageSweep>>> GetSweeps(
            [FromRoute] Game game,
            [FromQuery][Required] bool untied,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var sweeps = await _stageSweepProvider
                .GetSweepsAsync(game, untied, startDate, endDate)
                .ConfigureAwait(false);

            return Ok(sweeps);
        }
    }
}
