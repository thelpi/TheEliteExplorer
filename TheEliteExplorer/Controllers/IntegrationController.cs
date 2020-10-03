using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerInfrastructure;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Datas integration controller
    /// </summary>
    /// <seealso cref="Controller"/>
    [Route("datas-integration")]
    public class IntegrationController : Controller
    {
        private readonly ISqlContext _sqlContext;
        private readonly ITheEliteWebSiteParser _siteParser;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
        /// <param name="siteParser">Instance of <see cref="ITheEliteWebSiteParser"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="siteParser"/> is <c>Null</c>.</exception>
        public IntegrationController(ISqlContext sqlContext,
            ITheEliteWebSiteParser siteParser)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
            _siteParser = siteParser ?? throw new ArgumentNullException(nameof(siteParser));
        }

        /// <summary>
        /// Scans the site to get new times and new players to integrate in the database.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>A list of logs.</returns>
        [HttpPost("new-times/games/{game}")]
        public async Task<IReadOnlyCollection<string>> ScanTimePageAsync([FromRoute] Game game)
        {
            DateTime currentDate = await _sqlContext.GetLatestEntryDateAsync().ConfigureAwait(false);

            var logs = new List<string>();
            var entries = new List<EntryRequest>();

            foreach ((int, int) monthAndYear in currentDate.LoopMonthAndYear(ServiceProviderAccessor.ClockProvider.Now))
            {
                (IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>) resultsAndLogs =
                    await _siteParser
                        .ExtractTimeEntryAsync(
                            game,
                            monthAndYear.Item2,
                            monthAndYear.Item1,
                            currentDate)
                        .ConfigureAwait(false);

                logs.AddRange(resultsAndLogs.Item2);
                entries.AddRange(resultsAndLogs.Item1);
            }

            foreach (EntryRequest entry in entries)
            {
                string log = await CreateEntryAsync(entry).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(log))
                {
                    logs.Add(log);
                }
            }

            return logs;
        }

        private async Task<string> CreateEntryAsync(EntryRequest entry)
        {
            try
            {
                long playerId = await _sqlContext
                    .InsertOrRetrievePlayerDirtyAsync(entry.PlayerUrlName)
                    .ConfigureAwait(false);

                await _sqlContext
                    .InsertOrRetrieveTimeEntryAsync(playerId, entry.LevelId, entry.StageId, entry.Date, entry.Time, entry.EngineId)
                    .ConfigureAwait(false);

                return null;
            }
            catch (Exception ex)
            {
                return $"An error occured during the entry integration - {entry.ToString()} - {ex.Message}";
            }
        }
    }
}
