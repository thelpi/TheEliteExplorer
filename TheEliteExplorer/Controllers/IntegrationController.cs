using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorer.Domain;
using TheEliteExplorer.Infrastructure;

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
        public IntegrationController(ISqlContext sqlContext, ITheEliteWebSiteParser siteParser)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
            _siteParser = siteParser ?? throw new ArgumentNullException(nameof(siteParser));
        }

        /// <summary>
        /// Scans the site to get new times and new players to integrate in the database.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="year">The year to scan; <c>Null</c> for current.</param>
        /// <param name="month">The month to scan; <c>Null</c> for current.</param>
        /// <param name="minimalDateToScan">
        /// String representation of the optionnal date where to stop scan;
        /// if not a date, the full page will be scanned.
        /// </param>
        /// <returns>A list of logs.</returns>
        [HttpGet("new-times/games/{game}")] // TODO: POST
        public async Task<IReadOnlyCollection<string>> ScanTimePageAsync([FromRoute] Game game, [FromQuery] int? year, [FromQuery] int? month, [FromQuery] string minimalDateToScan)
        {
            (IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>) resultsAndLogs =
                await _siteParser
                    .ExtractTimeEntryAsync(
                        game,
                        year ?? DateTime.Now.Year,
                        month ?? DateTime.Now.Month,
                        minimalDateToScan.ToDateTime())
                    .ConfigureAwait(false);

            List<string> logs = new List<string>(resultsAndLogs.Item2);

            foreach (EntryRequest entry in resultsAndLogs.Item1)
            {
                try
                {
                    long playerId = await _sqlContext
                        .InsertOrRetrievePlayerDirtyAsync(entry.PlayerUrlName)
                        .ConfigureAwait(false);

                    await _sqlContext
                        .InsertOrRetrieveTimeEntryAsync(playerId, entry.LevelId, entry.StageId, entry.Date, entry.Time, entry.EngineId)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logs.Add($"An error occured during the entry integration - {entry.ToString()} - {ex.Message}");
                }
            }

            return logs;
        }
    }
}
