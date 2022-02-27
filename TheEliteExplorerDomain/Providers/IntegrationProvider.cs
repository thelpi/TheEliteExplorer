using System;
using System.Collections.Generic;
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
        public async Task ScanAllPlayersEntriesHistoryAsync(
            Game game)
        {
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            foreach (var player in  players)
            {
                var entries = await _siteParser
                    .GetPlayerEntriesHistoryAsync(game, player.UrlName)
                    .ConfigureAwait(false);

                if (entries != null)
                {
                    foreach (var stage in game.GetStages())
                    {
                        await _writeRepository
                            .DeletePlayerStageEntriesAsync(stage, player.Id)
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
        public async Task ScanPlayerEntriesHistoryAsync(
            Game game,
            long playerId)
        {
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var player = players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                throw new ArgumentException($"invalid {nameof(playerId)}.", nameof(playerId));
            }

            var entries = await _siteParser
                .GetPlayerEntriesHistoryAsync(game, player.UrlName)
                .ConfigureAwait(false);

            if (entries != null)
            {
                foreach (var stage in game.GetStages())
                {
                    await _writeRepository
                        .DeletePlayerStageEntriesAsync(stage, playerId)
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
        public async Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync()
        {
            var okPlayers = new List<Player>();

            var players = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            foreach (var p in players)
            {
                var pInfo = await _siteParser
                        .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                        .ConfigureAwait(false);

                if (pInfo != null)
                {
                    pInfo.Id = p.Id;
                    okPlayers.Add(new Player(pInfo));
                }
            }

            return okPlayers;
        }

        /// <inheritdoc />
        public async Task ScanTimePageAsync(
            Game game,
            DateTime? startDate)
        {
            var realStart = await GetStartDateAsync(game, startDate)
                .ConfigureAwait(false);

            foreach (var loopDate in realStart.LoopBetweenDates(DateStep.Month).Reverse())
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

        /// <inheritdoc />
        public async Task ScanStageTimesAsync(Stage stage)
        {
            var entries = await _siteParser.ExtractStageAllTimeEntriesAsync(stage).ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await CreateEntryAsync(entry, stage.GetGame()).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CleanDirtyPlayerAsync(long playerId)
        {
            var players = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            var p = players.SingleOrDefault(_ => _.Id == playerId);
            if (p == null)
                return false;

            var pInfo = await _siteParser
                .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                .ConfigureAwait(false);
            if (pInfo == null)
                return false;

            pInfo.Id = playerId;

            await _writeRepository
                .CleanPlayerAsync(pInfo)
                .ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc />
        public async Task CheckDirtyPlayersAsync()
        {
            var nonDirtyPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            const int parallel = 4;
            for (var i = 0; i < nonDirtyPlayers.Count; i += parallel)
            {
                await Task.WhenAll(nonDirtyPlayers.Skip(i).Take(parallel).Select(async p =>
                {
                    var res = await _siteParser
                        .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                        .ConfigureAwait(false);

                    if (res == null)
                    {
                        await _writeRepository
                            .UpdateDirtyPlayerAsync(p.Id)
                            .ConfigureAwait(false);
                    }
                })).ConfigureAwait(false);
            }
        }

        private async Task<DateTime> GetStartDateAsync(
            Game game,
            DateTime? startDate)
        {
            if (!startDate.HasValue)
            {
                startDate = await _readRepository
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
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);
            var dirtyPlayers = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            var match = players
                .Concat(dirtyPlayers)
                .FirstOrDefault(p =>
                    p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase));

            long playerId;
            if (match == null)
            {
                playerId = await _writeRepository
                    .InsertPlayerAsync(entry.PlayerUrlName, Player.DefaultPlayerHexColor)
                    .ConfigureAwait(false);
            }
            else
            {
                playerId = match.Id;
            }

            var requestEntry = entry.ToEntry(playerId);

            var entries = await _readRepository.GetEntriesAsync(
               requestEntry.Stage,
               requestEntry.Level,
               requestEntry.Date?.Date,
               requestEntry.Date?.Date.AddDays(1));

            var matchEntry = entries.FirstOrDefault(e =>
                e.PlayerId == requestEntry.PlayerId
                && e.Time == requestEntry.Time
                && e.Engine == requestEntry.Engine);

            if (matchEntry == null)
            {
                await _writeRepository
                    .InsertTimeEntryAsync(requestEntry, game)
                    .ConfigureAwait(false);
            }
            else if (!matchEntry.Date.HasValue && requestEntry.Date.HasValue)
            {
                await _writeRepository
                    .UpdateEntryDateAsync(matchEntry.Id, requestEntry.Date.Value)
                    .ConfigureAwait(false);
            }
        }
    }
}
