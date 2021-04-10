﻿using System;
using System.Linq;
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
        public async Task<IActionResult> ScanTimePageAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? startDate)
        {
            await _integrationProvider
                .ScanTimePageAsync(game, startDate)
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Scans the site to get every time for a single stage.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <param name="clear">Clears previous entries y/n.</param>
        /// <returns>Nothing.</returns>
        [HttpPut("stages/{stageId}/entries")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ScanStageTimesAsync(
            [FromRoute] long stageId,
            [FromQuery] bool clear)
        {
            var stage = Stage.Get(stageId);
            if (stage == null)
            {
                return BadRequest();
            }

            await _integrationProvider
                .ScanStageTimesAsync(stage, clear)
                .ConfigureAwait(false);

            return NoContent();
        }

        /// <summary>
        /// Cleans dirty players.
        /// </summary>
        /// <returns>Nothing.</returns>
        [HttpPatch("dirty-players")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> CleanDirtyPlayersAsync()
        {
            await _integrationProvider
                .CleanDirtyPlayersAsync()
                .ConfigureAwait(false);

            return NoContent();
        }
    }
}
