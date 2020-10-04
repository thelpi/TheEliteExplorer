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
        /// Gets ranking
        /// </summary>
        /// <param name="date">Ranking date.</param>
        /// <param name="game">The requested game.</param>
        /// <param name="page">page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <returns>Paginated collection of <see cref="RankingEntry"/>.</returns>
        [HttpGet("{date}/games/{game}")]
        public async Task<PaginatedCollection<RankingEntry>> GetRankingAsync([FromRoute] string date, [FromRoute] Game game, [FromQuery] int page, [FromQuery] int count)
        {
            DateTime realDate = ValidateDateParameter(date);

            var builder = new RankingBuilder(game,
                await GetEntriesForEachStageAndLevelAsync(realDate, game).ConfigureAwait(false),
                await _sqlContext.GetPlayersAsync().ConfigureAwait(false)
            );

            return PaginatedCollection<RankingEntry>.CreateInstance(
                builder.GetRankingEntries(), page, count);
        }

        private DateTime ValidateDateParameter(string date)
        {
            if (!DateTime.TryParse(date, out DateTime realDate))
            {
                realDate = ServiceProviderAccessor.ClockProvider.Now;
            }
            return realDate.AddDays(1).Date;
        }

        private async Task<IReadOnlyCollection<EntryDto>> GetEntriesForEachStageAndLevelAsync(DateTime dateTime, Game game)
        {
            var entries = new List<EntryDto>();

            foreach (Level level in SystemExtensions.Enumerate<Level>())
            {
                foreach (Stage stage in Stage.Get(game))
                {
                    entries.AddRange(
                        await _sqlContext
                            .GetEntriesAsync(stage.Position, (long)level, null, dateTime, _configuration.IncludeUnknownDate)
                            .ConfigureAwait(false)
                    );
                }
            }

            return entries;
        }
    }
}
