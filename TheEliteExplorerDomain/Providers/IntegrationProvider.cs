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
        private readonly IWriteRepository _writeRepository;
        private readonly IReadRepository _readRepository;
        private readonly ITheEliteWebSiteParser _siteParser;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="writeRepository">Instance of <see cref="IWriteRepository"/>.</param>
        /// <param name="readRepository">Instance of <see cref="IReadRepository"/>.</param>
        /// <param name="siteParser">Instance of <see cref="ITheEliteWebSiteParser"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writeRepository"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="readRepository"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="siteParser"/> is <c>Null</c>.</exception>
        public IntegrationProvider(
            IWriteRepository writeRepository,
            IReadRepository readRepository,
            ITheEliteWebSiteParser siteParser)
        {
            _writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _siteParser = siteParser ?? throw new ArgumentNullException(nameof(siteParser));
        }

        /// <inheritdoc />
        public async Task ScanAllPlayersEntriesHistory(
            Game game)
        {
            var players = await _readRepository
                .GetPlayers()
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
                        await _writeRepository
                            .DeletePlayerStageEntries(stage.Id, player.Id)
                            .ConfigureAwait(false);
                    }

                    foreach (var entry in entries)
                    {
                        await CreateEntry(entry, game)
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
            var players = await _readRepository
                .GetPlayers()
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
                    await _writeRepository
                        .DeletePlayerStageEntries(stage.Id, playerId)
                        .ConfigureAwait(false);
                }

                foreach (var entry in entries)
                {
                    await CreateEntry(entry, game)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task CleanDirtyPlayers()
        {
            // TODO: ignore players permanently without sheet
            var players = await _readRepository
                .GetDirtyPlayers()
                .ConfigureAwait(false);

            foreach (var p in players)
            {
                var pInfo = await _siteParser
                    .GetPlayerInformation(p.UrlName, Player.DefaultPlayerHexColor)
                    .ConfigureAwait(false);

                if (pInfo != null)
                {
                    pInfo.Id = p.Id;
                    await _writeRepository
                        .UpdatePlayerInformation(pInfo)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task ScanTimePage(
            Game game,
            DateTime? startDate)
        {
            var realStart = await GetStartDate(game, startDate)
                .ConfigureAwait(false);

            foreach (var loopDate in realStart.LoopBetweenDates(DateStep.Month))
            {
                var results = await _siteParser
                    .ExtractTimeEntries(game, loopDate.Year, loopDate.Month, realStart)
                    .ConfigureAwait(false);

                foreach (var entry in results)
                {
                    await CreateEntry(entry, game).ConfigureAwait(false);
                }
            }
        }

        private async Task<DateTime> GetStartDate(
            Game game,
            DateTime? startDate)
        {
            if (!startDate.HasValue)
            {
                startDate = await _readRepository
                    .GetLatestEntryDate()
                    .ConfigureAwait(false);

                if (!startDate.HasValue)
                {
                    startDate = game.GetEliteFirstDate();
                }
            }

            return startDate.Value;
        }

        private async Task CreateEntry(
            EntryWebDto entry,
            Game game)
        {
            var playerId = await _writeRepository
                .InsertOrRetrievePlayerDirty(entry.PlayerUrlName, entry.Date, Player.DefaultPlayerHexColor)
                .ConfigureAwait(false);

            await _writeRepository
                .InsertOrRetrieveTimeEntry(entry.ToEntry(playerId), (long)game)
                .ConfigureAwait(false);
        }
    }
}
