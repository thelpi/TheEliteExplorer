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
        [HttpPost("games/{game}/new-times")]
        public async Task<IReadOnlyCollection<string>> ScanTimePageAsync([FromRoute] Game game)
        {
            DateTime currentDate = await _sqlContext.GetLatestEntryDateAsync().ConfigureAwait(false);

            var logs = new List<string>();
            var entries = new List<EntryRequest>();

            foreach (DateTime loopDate in currentDate.LoopBetweenDates(DateStep.Month))
            {
                (IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>) resultsAndLogs =
                    await _siteParser
                        .ExtractTimeEntriesAsync(
                            game,
                            loopDate.Year,
                            loopDate.Month,
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

        /// <summary>
        /// Scans the site to get every time for a single stage.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <returns>A list of logs.</returns>
        [HttpPost("stages/{stageId}/times")]
        public async Task<IReadOnlyCollection<string>> ScanStageTimesAsync([FromRoute] long stageId)
        {
            bool haveEntries = await CheckForExistingEntries(stageId).ConfigureAwait(false);
            if (haveEntries)
            {
                return new List<string>
                {
                    "Unables to scan a stage already scanned."
                };
            }

            (IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>) entriesAngLogs =
                await _siteParser.ExtractStageAllTimeEntriesAsync(stageId).ConfigureAwait(false);

            var logs = new List<string>(entriesAngLogs.Item2);

            foreach (EntryRequest entry in entriesAngLogs.Item1)
            {
                string log = await CreateEntryAsync(entry).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(log))
                {
                    logs.Add(log);
                }
            }

            return logs;
        }

        /// <summary>
        /// Cleans dirty players.
        /// </summary>
        /// <returns>Collection of logs.</returns>
        [HttpPatch("dirty-players")]
        public async Task<IReadOnlyCollection<string>> CleanDirtyPlayersAsync()
        {
            var logs = new List<string>();

            var players = await _sqlContext
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            foreach (var p in players)
            {
                var pInfoAndLogs = await _siteParser
                    .GetPlayerInformation(p.UrlName)
                    .ConfigureAwait(false);

                if (pInfoAndLogs.Item1 != null)
                {
                    var pInfo = pInfoAndLogs.Item1;
                    pInfo.Id = p.Id;
                    await _sqlContext
                        .UpdatePlayerInformationAsync(pInfo)
                        .ConfigureAwait(false);
                }

                logs.AddRange(pInfoAndLogs.Item2);
            }

            return logs;
        }

        private async Task<bool> CheckForExistingEntries(long stageId)
        {
            foreach (Level level in SystemExtensions.Enumerate<Level>())
            {
                IReadOnlyCollection<TheEliteExplorerDomain.Dtos.EntryDto> levelEntries = await _sqlContext
                    .GetEntriesAsync(stageId, (long)level, null, null)
                    .ConfigureAwait(false);
                if (levelEntries.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<string> CreateEntryAsync(EntryRequest entry)
        {
            try
            {
                long playerId = await _sqlContext
                    .InsertOrRetrievePlayerDirtyAsync(entry.PlayerUrlName, entry.Date)
                    .ConfigureAwait(false);

                await _sqlContext
                    .InsertOrRetrieveTimeEntryAsync(new TheEliteExplorerDomain.Dtos.EntryDto(entry, playerId))
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
