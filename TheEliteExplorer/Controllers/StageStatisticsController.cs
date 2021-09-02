using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Stage statistics controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    public class StageStatisticsController : Controller
    {
        private readonly IStageStatisticsProvider _stageStatisticsProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stageStatisticsProvider">Instance of <see cref="IStageStatisticsProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stageStatisticsProvider"/> is <c>Null</c>.</exception>
        public StageStatisticsController(IStageStatisticsProvider stageStatisticsProvider)
        {
            _stageStatisticsProvider = stageStatisticsProvider ?? throw new ArgumentNullException(nameof(stageStatisticsProvider));
        }

        /// <summary>
        /// Gets statistics about entries count for a specified game across all stages and levels.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <param name="levelDetails">With or without details by level.</param>
        /// <returns>Collection of <see cref="StageEntryCount"/>.</returns>
        [HttpGet("games/{game}/entries-count")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StageEntryCount>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<StageEntryCount>>> GetLongestStandingWrs(
            [FromRoute] Game game,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool levelDetails)
        {
            var results = await _stageStatisticsProvider
                .GetStagesEntriesCount(game, startDate, endDate, levelDetails)
                .ConfigureAwait(false);

            return Ok(results);
        }
    }
}
