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
            var players = await GetPlayersAsync().ConfigureAwait(false);

            foreach (var player in players.validPlayers)
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
                        await CreateEntryAsync(entry, game, GetSameDayEngines(entries, entry), players.validPlayers, players.bannedPlayers)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private static Engine[] GetSameDayEngines(IEnumerable<EntryWebDto> entries, EntryWebDto entry)
        {
            return entries
                .Where(e => e != entry
                    && e.Date == entry.Date
                    && e.Level == entry.Level
                    && e.PlayerUrlName == entry.PlayerUrlName
                    && e.Stage == entry.Stage
                    && e.Time == entry.Time)
                .Select(e => e.Engine)
                .ToArray();
        }

        /// <inheritdoc />
        public async Task ScanPlayerEntriesHistoryAsync(
            Game game,
            long playerId)
        {
            var players = await GetPlayersAsync().ConfigureAwait(false);

            var player = players.validPlayers.FirstOrDefault(p => p.Id == playerId);
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
                    await CreateEntryAsync(entry, game, GetSameDayEngines(entries, entry), players.validPlayers, players.bannedPlayers)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync()
        {
            var okPlayers = new List<Player>();

            var players = await _readRepository
                .GetDirtyPlayersAsync(false)
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

            var players = await GetPlayersAsync().ConfigureAwait(false);

            foreach (var loopDate in realStart.LoopBetweenDates(DateStep.Month).Reverse())
            {
                var results = await _siteParser
                    .ExtractTimeEntriesAsync(game, loopDate.Year, loopDate.Month, realStart)
                    .ConfigureAwait(false);

                var collectedIds = new List<long>();
                foreach (var entry in results)
                {
                    var entryId = await CreateEntryAsync(
                            entry,
                            game,
                            GetSameDayEngines(results, entry),
                            players.validPlayers,
                            players.bannedPlayers)
                        .ConfigureAwait(false);
                    collectedIds.Add(entryId);
                }

                var entries = await _readRepository
                    .GetEntriesAsync(null, null, loopDate, loopDate.AddMonths(1))
                    .ConfigureAwait(false);

                var entriesToRemove = entries
                    .Where(e => e.Stage.GetGame() == game && !collectedIds.Contains(e.Id))
                    .ToList();

                foreach (var entry in entriesToRemove)
                {
                    await _writeRepository
                        .RemoveEntryAsync(entry.Id)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task ScanStageTimesAsync(Stage stage)
        {
            var players = await GetPlayersAsync().ConfigureAwait(false);

            var entries = await _siteParser.ExtractStageAllTimeEntriesAsync(stage).ConfigureAwait(false);

            foreach (var entry in entries)
            {
                await CreateEntryAsync(entry, stage.GetGame(), GetSameDayEngines(entries, entry), players.validPlayers, players.bannedPlayers).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CleanDirtyPlayerAsync(long playerId)
        {
            var players = await _readRepository
                .GetDirtyPlayersAsync(false)
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

        private async Task<(List<PlayerDto> validPlayers, List<PlayerDto> bannedPlayers)> GetPlayersAsync()
        {
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var dirtyPlayers = await _readRepository
                .GetDirtyPlayersAsync(true)
                .ConfigureAwait(false);

            return (players.Concat(dirtyPlayers.Where(p => !p.IsBanned)).ToList(),
                dirtyPlayers.Where(p => p.IsBanned).ToList());
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

        private async Task<long> CreateEntryAsync(
            EntryWebDto entry,
            Game game,
            Engine[] enginesTheSameDay,
            List<PlayerDto> validPlayers,
            List<PlayerDto> bannedPlayers)
        {
            if (bannedPlayers.Any(p =>
                p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return 0;
            }

            var match = validPlayers
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

            // same stage, same level, same player, same time
            // potentially multiple engines
            var playerTimeEntries = await _readRepository
                .GetEntriesAsync(
                    requestEntry.Stage,
                    requestEntry.Level,
                    requestEntry.PlayerId,
                    requestEntry.Time)
                .ConfigureAwait(false);

            if (playerTimeEntries.Count == 0)
            {
                // no match: insert
                return await _writeRepository
                    .InsertTimeEntryAsync(requestEntry, game)
                    .ConfigureAwait(false);
            }
            else if (requestEntry.Engine != Engine.UNK)
            {
                // engine is known
                // does an entry exist with this engine?
                var matchByEngine = playerTimeEntries.FirstOrDefault(e => e.Engine == requestEntry.Engine);
                if (matchByEngine == null)
                {
                    // no: insert
                    return await _writeRepository
                        .InsertTimeEntryAsync(requestEntry, game)
                        .ConfigureAwait(false);
                }
                else
                {
                    // yes: check the date
                    if (matchByEngine.Date != requestEntry.Date)
                    {
                        // needs an update
                        await _writeRepository
                            .UpdateEntryAsync(matchByEngine.Id, requestEntry.Date.Value, requestEntry.Engine)
                            .ConfigureAwait(false);
                    }
                    return matchByEngine.Id;
                }
            }
            else
            {
                // engine is unknown
                // does an entry exist at the same date?
                var matchByDate = playerTimeEntries.FirstOrDefault(e => e.Date == requestEntry.Date);
                if (matchByDate == null)
                {
                    // no: insert
                    return await _writeRepository
                        .InsertTimeEntryAsync(requestEntry, game)
                        .ConfigureAwait(false);
                }
                else
                {
                    // engine updated?
                    if (matchByDate.Engine != requestEntry.Engine)
                    {
                        if (!enginesTheSameDay.Contains(matchByDate.Engine))
                        {
                            // the engine list of the same day doesn't contain the matching entry
                            // it means the engine on the match must be reset
                            await _writeRepository
                               .UpdateEntryAsync(matchByDate.Id, requestEntry.Date.Value, requestEntry.Engine)
                               .ConfigureAwait(false);
                            return matchByDate.Id;
                        }
                        else
                        {
                            // the engine list of the same day contains the matching entry
                            // it means the match is worthless and we have to create a new entry
                            return await _writeRepository
                                .InsertTimeEntryAsync(requestEntry, game)
                                .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // same date, same unknown engine
                        return matchByDate.Id;
                    }
                }
            }
        }
    }
}
