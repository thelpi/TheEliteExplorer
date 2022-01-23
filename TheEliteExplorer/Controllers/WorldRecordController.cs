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
    /// World record controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    public class WorldRecordController : Controller
    {
        private readonly IWorldRecordProvider _worldRecordProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="worldRecordProvider">Instance of <see cref="IWorldRecordProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="worldRecordProvider"/> is <c>Null</c>.</exception>
        public WorldRecordController(IWorldRecordProvider worldRecordProvider)
        {
            _worldRecordProvider = worldRecordProvider ?? throw new ArgumentNullException(nameof(worldRecordProvider));
        }

        /// <summary>
        /// Gets longest standing world records.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied"><c>True</c> to only get untied world records.</param>
        /// <param name="stillStanding"><c>True</c> to get only world records currently standing.</param>
        /// <param name="atDate">Snapshot at a specified date; <c>Null</c> for current date.</param>
        /// <param name="page">page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <returns>Collection of longest standing world records.</returns>
        [HttpGet("games/{game}/longest-standing-world-records")]
        [ProducesResponseType(typeof(PaginatedCollection<StageWrStanding>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PaginatedCollection<StageWrStanding>>> GetLongestStandingWrs(
            [FromRoute] Game game,
            [FromQuery] bool untied,
            [FromQuery] bool stillStanding,
            [FromQuery] DateTime? atDate,
            [FromQuery] int page,
            [FromQuery] int count)
        {
            var results = await _worldRecordProvider
                .GetLongestStandingWrs(game, untied, stillStanding, atDate)
                .ConfigureAwait(false);

            return Ok(PaginatedCollection<StageWrStanding>.CreateInstance(results, page, count));
        }

        /// <summary>
        /// Gets the all-time leaderboard for a single stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="limit">Players count limit.</param>
        /// <returns>All-time leaderboard for the stage.</returns>
        [HttpGet("stages/{stage}/alltime-leaderboards")]
        [ProducesResponseType(typeof(StageAllTimeLeaderboard), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<StageAllTimeLeaderboard>> GetStageAllTimeLeaderboard(
            [FromRoute] Stage stage,
            [FromQuery] int limit)
        {
            var results = await _worldRecordProvider
                .GetStageAllTimeLeaderboard(stage, limit)
                .ConfigureAwait(false);

            return Ok(results);
        }

        /// <summary>
        /// Gets an history of every current "longest standing" world record (tied or not depending on <paramref name="untied"/>).
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied"><c>True</c> to only get untied world records.</param>
        /// <returns>Collection of standing world record.</returns>
        [HttpGet("games/{game}/current-longest-standing-history")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StageWrStanding>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<StageWrStanding>>> GetCurrentLongestStandingWrsHistory(
            [FromRoute] Game game,
            [FromQuery] bool untied)
        {
            var results = await _worldRecordProvider
                .GetCurrentLongestStandingWrsHistory(game, untied)
                .ConfigureAwait(false);

            return Ok(results);
        }
    }
}
