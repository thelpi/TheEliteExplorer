using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerInfrastructure;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Stage controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Route("stages")]
    public class StageController : Controller
    {
        private readonly ISqlContext _sqlContext;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        public StageController(ISqlContext sqlContext)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
        }

        /// <summary>
        /// Gets every sweep of a specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>Collection of <see cref="Sweep"/>.</returns>
        [HttpGet("games/{game}")]
        public async Task<IReadOnlyCollection<Sweep>> GetSweepsAsync([FromRoute] Game game)
        {
            IReadOnlyCollection<EntryDto> entries = await _sqlContext.GetEntriesForEachStageAndLevelAsync(game).ConfigureAwait(false);

            // DO STUFF HERE

            return new List<Sweep>();
        }

        private DateTime ValidateDateParameter(string date)
        {
            if (!DateTime.TryParse(date, out DateTime realDate))
            {
                realDate = ServiceProviderAccessor.ClockProvider.Now;
            }
            return realDate.AddDays(1).Date;
        }
    }
}
