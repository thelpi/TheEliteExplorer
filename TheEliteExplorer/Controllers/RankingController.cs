using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;
using TheEliteExplorerDomain.Providers;
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
                game,
                await GetEntriesForEachStageAndLevelAsync(game).ConfigureAwait(false)
            );

            IReadOnlyCollection<RankingEntry> rankingEntries = builder.GetRankingEntries(realDate);

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
            DateTime? startDate = await _sqlContext.GetLatestRankingDateAsync(game).ConfigureAwait(false);

            var builder = new RankingBuilder(
                _configuration,
                await _sqlContext.GetPlayersAsync().ConfigureAwait(false),
                game,
                await GetEntriesForEachStageAndLevelAsync(game).ConfigureAwait(false)
            );

            foreach (DateTime date in GetRealStartDate(startDate, builder.Entries).LoopBetweenDates(DateStep.Day))
            {
                await builder
                    .GenerateRankings(date, (r) => _sqlContext.InsertRankingAsync(r))
                    .ConfigureAwait(false);
            }
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
            var entries = await GetEntriesForEachStageAndLevelAsync(Game.GoldenEye).ConfigureAwait(false);

            var players = await _sqlContext
                .GetPlayersAsync()
                .ConfigureAwait(false);

            return new StageSweepBuilder().GetSweeps(entries, players, untied, startDate, endDate);
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

        private async Task<IReadOnlyCollection<EntryDto>> GetEntriesForEachStageAndLevelAsync(Game game)
        {
            var entries = new List<EntryDto>();

            foreach (Level level in SystemExtensions.Enumerate<Level>())
            {
                foreach (Stage stage in Stage.Get(game))
                {
                    entries.AddRange(
                        await _sqlContext.GetEntriesAsync(stage.Position, level, null, null).ConfigureAwait(false)
                    );
                }
            }

            return entries;
        }
    }
}
