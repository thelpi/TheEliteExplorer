using System;
using System.Linq;
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
        public async Task ScanAllPlayersEntriesHistory(
            Game game)
        {
            var players = await _sqlContext
                .GetPlayersAsync()
                .ConfigureAwait(false);

            foreach (var player in  players)
            {
                var entries = await _siteParser
                    .GetPlayerEntriesHistory(game, player.UrlName)
                    .ConfigureAwait(false);

                if (entries != null)
                {
                    foreach (var stage in Stage.Get(game))
                    {
                        await _sqlContext
                            .DeletePlayerStageEntries(stage.Id, player.Id)
                            .ConfigureAwait(false);
                    }

                    foreach (var entry in entries)
                    {
                        await CreateEntryAsync(entry, game)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task ScanPlayerEntriesHistory(
            Game game,
            long playerId)
        {
            var players = await _sqlContext
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var player = players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                throw new ArgumentException($"invalid {nameof(playerId)}.", nameof(playerId));
            }

            var entries = await _siteParser
                .GetPlayerEntriesHistory(game, player.UrlName)
                .ConfigureAwait(false);

            if (entries != null)
            {
                foreach (var stage in Stage.Get(game))
                {
                    await _sqlContext
                        .DeletePlayerStageEntries(stage.Id, playerId)
                        .ConfigureAwait(false);
                }

                foreach (var entry in entries)
                {
                    await CreateEntryAsync(entry, game)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task CleanDirtyPlayersAsync()
        {
            // TODO: ignore players permanently without sheet
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
    }
}
