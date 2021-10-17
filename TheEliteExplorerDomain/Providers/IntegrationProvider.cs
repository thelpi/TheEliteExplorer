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
        private const decimal DuplicateEntriesRate = 0.5M;

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
                    foreach (var stage in game.GetStages())
                    {
                        await _writeRepository
                            .DeletePlayerStageEntries(stage, player.Id)
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
                foreach (var stage in game.GetStages())
                {
                    await _writeRepository
                        .DeletePlayerStageEntries(stage, playerId)
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
            var players = await _readRepository
                .GetDirtyPlayers()
                .ConfigureAwait(false);

            foreach (var p in players)
            {
                var entries = await _readRepository
                    .GetPlayerEntries(p.Id)
                    .ConfigureAwait(false);

                var duplicates = new List<EntryDto>();
                foreach (var entry in entries)
                {
                    duplicates.AddRange(
                        await _readRepository
                            .GetEntriesByEntry(entry.Id)
                            .ConfigureAwait(false));
                }

                var potentialPlayerMatch = duplicates
                    .GroupBy(d => d.PlayerId)
                    .OrderByDescending(d => d.Count())
                    .FirstOrDefault();

                if (potentialPlayerMatch?.Count() > entries.Count * DuplicateEntriesRate)
                {

                }
                else
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

        /// <inheritdoc />
        public async Task ScanStageTimes(Stage stage)
        {
            var entries = await _siteParser.ExtractStageAllTimeEntries(stage).ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await CreateEntry(entry, stage.GetGame()).ConfigureAwait(false);
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
            var players = await _readRepository
                .GetPlayers()
                .ConfigureAwait(false);
            var dirtyPlayers = await _readRepository
                .GetDirtyPlayers()
                .ConfigureAwait(false);

            var match = players
                .Concat(dirtyPlayers)
                .FirstOrDefault(p =>
                    p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase));

            long playerId;
            if (match == null)
            {
                playerId = await _writeRepository
                    .InsertPlayer(entry.PlayerUrlName, entry.Date, Player.DefaultPlayerHexColor)
                    .ConfigureAwait(false);
            }
            else
            {
                playerId = match.Id;
            }

            var requestEntry = entry.ToEntry(playerId);

            var entries = await _readRepository.GetEntries(
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
                    .InsertTimeEntry(requestEntry, game)
                    .ConfigureAwait(false);
            }
            else if (!matchEntry.Date.HasValue && requestEntry.Date.HasValue)
            {
                await _writeRepository
                    .UpdateEntryDate(matchEntry.Id, requestEntry.Date.Value)
                    .ConfigureAwait(false);
            }
        }
    }
}
