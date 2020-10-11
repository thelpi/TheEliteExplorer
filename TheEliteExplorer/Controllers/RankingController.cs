using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Ranking controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Route("rankings")]
    public class RankingController : Controller
    {
        private readonly ISqlContext _sqlContext;
        private readonly RankingConfiguration _configuration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
        /// <param name="configuration">Instance of <see cref="RankingConfiguration"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> or inner value is <c>Null</c>.</exception>
        public RankingController(ISqlContext sqlContext, IOptions<RankingConfiguration> configuration)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
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
            DateTime realDate = ValidateDateParameter(date);

            var builder = new RankingBuilder(
                _configuration,
                await _sqlContext.GetPlayersAsync().ConfigureAwait(false),
                game
            );

            IReadOnlyCollection<RankingEntry> rankingEntries = await builder.GetRankingEntriesAsync(
                await _sqlContext.GetEntriesForEachStageAndLevelAsync(game).ConfigureAwait(false),
                realDate
            ).ConfigureAwait(false);

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
            IReadOnlyCollection<EntryDto> entries = await _sqlContext.GetEntriesForEachStageAndLevelAsync(game).ConfigureAwait(false);
            DateTime? startDate = await _sqlContext.GetLatestRankingDateAsync((int)game).ConfigureAwait(false);

            var builder = new RankingBuilder(
                _configuration,
                await _sqlContext.GetPlayersAsync().ConfigureAwait(false),
                game
            );

            foreach (DateTime date in GetRealStartDate(startDate, entries).LoopBetweenDates(DateStep.Day))
            {
                await builder
                    .GenerateRankings(entries, date, (r) => _sqlContext.InsertRankingAsync(r))
                    .ConfigureAwait(false);
            }
        }

        private DateTime GetRealStartDate(DateTime? startDate, IReadOnlyCollection<EntryDto> entries)
        {
            return startDate ?? entries.Where(e => e.Date.HasValue).Min(e => e.Date.Value);
        }

        private DateTime ValidateDateParameter(string date)
        {
            if (!DateTime.TryParse(date, out DateTime realDate))
            {
                realDate = ServiceProviderAccessor.ClockProvider.Now;
            }
            return realDate.Date;
        }
    }
}
