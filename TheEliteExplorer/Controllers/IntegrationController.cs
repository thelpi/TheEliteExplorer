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
    /// Datas integration controller
    /// </summary>
    /// <seealso cref="Controller"/>
    public class IntegrationController : Controller
    {
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
        public async Task<IActionResult> ScanTimePage(
            [FromRoute] Game game,
            [FromQuery] DateTime? startDate)
        {
            try
            {
                await _integrationProvider
                    .ScanTimePage(game, startDate)
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
        public async Task<IActionResult> ScanTimePage(
            [FromQuery] Stage[] stages)
        {
            foreach (var stage in stages)
            {
                await _integrationProvider
                    .ScanStageTimes(stage)
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
        public async Task<IActionResult> ScanAllPlayersTimes(
            [FromRoute] Game game)
        {
            await _integrationProvider
                .ScanAllPlayersEntriesHistory(game)
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
        public async Task<IActionResult> ScanPlayerTimes(
            [FromRoute] Game game,
            [FromRoute] long playerId)
        {
            await _integrationProvider
                .ScanPlayerEntriesHistory(game, playerId)
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Gets dirty players with valid time page.
        /// </summary>
        /// <returns>Collection of players.</returns>
        [HttpGet("cleanable-dirty-players")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<ActionResult<IReadOnlyCollection<Player>>> GetCleanableDirtyPlayers()
        {
            var players = await _integrationProvider
                .GetCleanableDirtyPlayers()
                .ConfigureAwait(false);

            return Ok(players);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("dirty-players")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> CheckDirtyPlayers()
        {
            await _integrationProvider
                   .CheckDirtyPlayers()
                   .ConfigureAwait(false);

            return NoContent();
        }
    }
}
