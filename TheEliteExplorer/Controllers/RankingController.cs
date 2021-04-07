using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [Route("rankings")]
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
        /// <param name="page">page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <param name="date">String representation of date; <c>Null</c> for current date.</param>
        /// <returns>Paginated collection of <see cref="RankingEntry"/>.</returns>
        [HttpGet("games/{game}")]
        public async Task<PaginatedCollection<RankingEntry>> GetRankingAsync([FromRoute] Game game, [FromQuery] int page, [FromQuery] int count, [FromQuery] string date)
        {
            var rankingEntries = await _rankingProvider
                .GetRankingEntries(game, ValidateDateParameter(date))
                .ConfigureAwait(false);

            return PaginatedCollection<RankingEntry>.CreateInstance(rankingEntries, page, count);
        }

        /// <summary>
        /// Generates ranking for the specified game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("games/{game}")]
        public async Task CreatesRanking([FromRoute] Game game)
        {
            await _rankingProvider
                .GenerateRankings(game)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets sweeps.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied">Is untied y/n.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>Collection of untied sweeps.</returns>
        [HttpGet("sweeps/{game}")]
        public async Task<IReadOnlyCollection<StageSweep>> GetSweeps(
            [FromRoute] Game game,
            [FromQuery][Required] bool untied,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            return await _stageSweepProvider
                .GetSweepsAsync(game, untied, startDate, endDate)
                .ConfigureAwait(false);
        }

        private DateTime ValidateDateParameter(string date)
        {
            return DateTime.TryParse(date, out DateTime realDate)
                ? realDate
                : ServiceProviderAccessor.ClockProvider.Now;
        }
    }
}
