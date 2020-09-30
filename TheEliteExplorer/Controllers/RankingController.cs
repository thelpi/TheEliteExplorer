using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorer.Domain;
using TheEliteExplorer.Infrastructure;
using TheEliteExplorer.Infrastructure.Dtos;

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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        public RankingController(ISqlContext sqlContext)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
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
            if (!DateTime.TryParse(date, out DateTime realDate))
            {
                realDate = DateTime.Now;
            }
            realDate = realDate.AddDays(1).Date;

            var builder = new RankingBuilder(
                await GetEntriesForEachStageAndLevelAsync(realDate, game).ConfigureAwait(false),
                await _sqlContext.GetPlayersAsync().ConfigureAwait(false)
            );

            return PaginatedCollection<RankingEntry>.CreateInstance(
                builder.GetRankingEntries(), page, count);
        }

        private async Task<IReadOnlyCollection<EntryDto>> GetEntriesForEachStageAndLevelAsync(DateTime dateTime, Game game)
        {
            var entries = new List<EntryDto>();

            foreach (Level level in TypeExtensions.Enumerate<Level>())
            {
                foreach (Stage stage in Stage.Get(game))
                {
                    entries.AddRange(
                        await _sqlContext
                            .GetEntriesAsync(stage.Position, (long)level, null, dateTime)
                            .ConfigureAwait(false)
                    );
                }
            }

            return entries;
        }
    }
}
