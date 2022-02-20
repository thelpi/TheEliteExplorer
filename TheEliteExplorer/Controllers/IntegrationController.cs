using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Datas integration controller
    /// </summary>
    /// <seealso cref="Controller"/>
    public class IntegrationController : Controller
    {
        private const long StandardRankingTypeId = 1;

        private readonly IIntegrationProvider _integrationProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="integrationProvider">Instance of <see cref="IIntegrationProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="integrationProvider"/> is <c>Null</c>.</exception>
        public IntegrationController(IIntegrationProvider integrationProvider)
        {
            _integrationProvider = integrationProvider ?? throw new ArgumentNullException(nameof(integrationProvider));
        }

        /// <summary>
        /// Scans the site to get new times and new players to integrate in the database.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="startDate">Start date.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("games/{game}/new-entries")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ScanTimePageAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? startDate)
        {
            try
            {
                await _integrationProvider
                    .ScanTimePageAsync(game, startDate)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                using (var w = new System.IO.StreamWriter($@"S:\iis_logs\api_global_app.log", true))
                {
                    w.WriteLine($"{DateTime.Now.ToString("yyyyMMddhhmmss")}\t{ex.Message}\t{ex.StackTrace}");
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Scans the site to get times from a bunch of stages to integrate in the database.
        /// </summary>
        /// <param name="stages">Stages collection.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("stages/entries")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ScanTimePageAsync(
            [FromQuery] Stage[] stages)
        {
            foreach (var stage in stages)
            {
                await _integrationProvider
                    .ScanStageTimesAsync(stage)
                    .ConfigureAwait(false);
            }

            return NoContent();
        }

        /// <summary>
        /// Scans the site to get every time for every player on a game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        [HttpPut("games/{game}/entries")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ScanAllPlayersTimesAsync(
            [FromRoute] Game game)
        {
            await _integrationProvider
                .ScanAllPlayersEntriesHistoryAsync(game)
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Scans the site to get every time for a single player on a game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        [HttpPut("games/{game}/players/{playerId}/entries")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ScanPlayerTimesAsync(
            [FromRoute] Game game,
            [FromRoute] long playerId)
        {
            await _integrationProvider
                .ScanPlayerEntriesHistoryAsync(game, playerId)
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Gets dirty players with valid time page.
        /// </summary>
        /// <returns>Collection of players.</returns>
        [HttpGet("cleanable-dirty-players")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Player>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyCollection<Player>>> GetCleanableDirtyPlayersAsync()
        {
            var players = await _integrationProvider
                .GetCleanableDirtyPlayersAsync()
                .ConfigureAwait(false);

            return Ok(players);
        }

        /// <summary>
        /// Checks for dirty players and update them in database.
        /// </summary>
        /// <returns>Nothing.</returns>
        [HttpPost("dirty-players")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> CheckDirtyPlayersAsync()
        {
            await _integrationProvider
                   .CheckDirtyPlayersAsync()
                   .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Cleans a dirty player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        [HttpPatch("dirty-players/{playerId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CleanDirtyPlayerAsync([FromRoute] long playerId)
        {
            if (playerId <= 0)
            {
                return BadRequest();
            }

            var success = await _integrationProvider
                .CleanDirtyPlayerAsync(playerId)
                .ConfigureAwait(false);

            return success ? (IActionResult)NoContent() : NotFound();
        }
    }
}
