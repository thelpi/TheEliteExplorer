using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

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
        /// <returns>Nothing.</returns>
        [HttpPost("games/{game}/new-times")]
        public async Task ScanTimePageAsync([FromRoute] Game game)
        {
            var currentDate = await _sqlContext.GetLatestEntryDateAsync().ConfigureAwait(false);

            foreach (var loopDate in (currentDate ?? game.GetEliteFirstDate()).LoopBetweenDates(DateStep.Month))
            {
                var results =
                    await _siteParser
                        .ExtractTimeEntriesAsync(
                            game,
                            loopDate.Year,
                            loopDate.Month,
                            currentDate.Value)
                        .ConfigureAwait(false);

                foreach (var entry in results)
                {
                    await CreateEntryAsync(entry, game).ConfigureAwait(false);
                }

                // TODO: to remove
                System.Diagnostics.Debug.WriteLine($"Done: {loopDate}");
            }
        }

        /// <summary>
        /// Scans the site to get every time for a single stage.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <returns>Nothing.</returns>
        [HttpPost("stages/{stageId}/times")]
        public async Task ScanStageTimesAsync([FromRoute] int stageId)
        {
            var game = Stage.Get().FirstOrDefault(s => s.Id == stageId).Game;

            bool haveEntries = await CheckForExistingEntries(stageId).ConfigureAwait(false);
            if (haveEntries)
            {
                throw new Exception("Unables to scan a stage already scanned.");
            }

            var entries = await _siteParser
                .ExtractStageAllTimeEntriesAsync(stageId)
                .ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await CreateEntryAsync(entry, game)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Cleans dirty players.
        /// </summary>
        /// <returns>Nothing.</returns>
        [HttpPatch("dirty-players")]
        public async Task CleanDirtyPlayersAsync()
        {
            var players = await _sqlContext
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            foreach (var p in players)
            {
                var pInfo = await _siteParser
                    .GetPlayerInformation(p.UrlName, Player.DefaultPlayerHexColor)
                    .ConfigureAwait(false);

                if (pInfo != null)
                {
                    pInfo.Id = p.Id;
                    await _sqlContext
                        .UpdatePlayerInformationAsync(pInfo)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> CheckForExistingEntries(int stageId)
        {
            foreach (Level level in SystemExtensions.Enumerate<Level>())
            {
                IReadOnlyCollection<EntryDto> levelEntries = await _sqlContext
                    .GetEntriesAsync(stageId, level, null, null)
                    .ConfigureAwait(false);
                if (levelEntries.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task CreateEntryAsync(EntryWebDto entry, Game game)
        {
            long playerId = await _sqlContext
                .InsertOrRetrievePlayerDirtyAsync(entry.PlayerUrlName, entry.Date, Player.DefaultPlayerHexColor)
                .ConfigureAwait(false);

            await _sqlContext
                .InsertOrRetrieveTimeEntryAsync(entry.ToEntry(playerId), (long)game)
                .ConfigureAwait(false);
        }
    }
}
