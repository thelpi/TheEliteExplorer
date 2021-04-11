using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;

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
        /// Generates world records for a whole game; clears previous world records already registered.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        [HttpPut("games/{game}/world-records")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> GenerateWorldRecords(
            [FromRoute] Game game)
        {
            await _worldRecordProvider
                .GenerateWorldRecords(game)
                .ConfigureAwait(false);

            return NoContent();
        }
    }
}
