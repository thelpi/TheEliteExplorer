using System;
using System.Threading.Tasks;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Providers
{
    /// <summary>
    /// Integration provider.
    /// </summary>
    /// <seealso cref="IIntegrationProvider"/>
    public sealed class IntegrationProvider : IIntegrationProvider
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
        public IntegrationProvider(
            ISqlContext sqlContext,
            ITheEliteWebSiteParser siteParser)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
            _siteParser = siteParser ?? throw new ArgumentNullException(nameof(siteParser));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task ScanStageTimesAsync(Stage stage, bool clear)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            await CheckExistentEntries(stage, clear)
                .ConfigureAwait(false);

            var entries = await _siteParser
                .ExtractStageAllTimeEntriesAsync(stage.Id)
                .ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await CreateEntryAsync(entry, stage.Game)
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task ScanTimePageAsync(
            Game game,
            DateTime? startDate)
        {
            var realStart = await GetStartDate(game, startDate)
                .ConfigureAwait(false);

            foreach (var loopDate in realStart.LoopBetweenDates(DateStep.Month))
            {
                var results = await _siteParser
                    .ExtractTimeEntriesAsync(game, loopDate.Year, loopDate.Month, realStart)
                    .ConfigureAwait(false);

                foreach (var entry in results)
                {
                    await CreateEntryAsync(entry, game).ConfigureAwait(false);
                }
            }
        }

        private async Task<DateTime> GetStartDate(
            Game game,
            DateTime? startDate)
        {
            if (!startDate.HasValue)
            {
                startDate = await _sqlContext
                    .GetLatestEntryDateAsync()
                    .ConfigureAwait(false);

                if (!startDate.HasValue)
                {
                    startDate = game.GetEliteFirstDate();
                }
            }

            return startDate.Value;
        }

        private async Task<bool> CheckForExistingEntries(
            Stage stage)
        {
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var levelEntries = await _sqlContext
                    .GetEntriesAsync(stage.Id, level, null, null)
                    .ConfigureAwait(false);
                if (levelEntries.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task CreateEntryAsync(
            EntryWebDto entry,
            Game game)
        {
            var playerId = await _sqlContext
                .InsertOrRetrievePlayerDirtyAsync(entry.PlayerUrlName, entry.Date, Player.DefaultPlayerHexColor)
                .ConfigureAwait(false);

            await _sqlContext
                .InsertOrRetrieveTimeEntryAsync(entry.ToEntry(playerId), (long)game)
                .ConfigureAwait(false);
        }

        private async Task CheckExistentEntries(Stage stage, bool clear)
        {
            var haveEntries = await CheckForExistingEntries(stage)
                            .ConfigureAwait(false);

            if (haveEntries)
            {
                if (!clear)
                {
                    throw new InvalidOperationException("Unables to scan a stage already scanned.");
                }

                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    await _sqlContext
                        .DeleteStageLevelEntries(stage.Id, level)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
