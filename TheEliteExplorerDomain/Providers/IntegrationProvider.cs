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
        public async Task ScanAllPlayersEntriesHistoryAsync(Game game)
        {
            var (validPlayers, bannedPlayers) = await GetPlayersAsync().ConfigureAwait(false);

            const int parallel = 8;
            for (var i = 0; i < validPlayers.Count; i += parallel)
            {
                await Task.WhenAll(validPlayers.Skip(i).Take(parallel).Select(async player =>
                {
                    await ExtractPlayerTimesAsync(game, player)
                        .ConfigureAwait(false);
                })).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task ScanPlayerEntriesHistoryAsync(Game game, long playerId)
        {
            var (validPlayers, bannedPlayers) = await GetPlayersAsync().ConfigureAwait(false);

            var player = validPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                throw new ArgumentException($"invalid {nameof(playerId)}.", nameof(playerId));
            }

            await ExtractPlayerTimesAsync(game, player).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync()
        {
            var okPlayers = new System.Collections.Concurrent.ConcurrentBag<Player>();

            var players = await _readRepository
                .GetDirtyPlayersAsync(false)
                .ConfigureAwait(false);

            const int parallel = 8;
            for (var i = 0; i < players.Count; i += parallel)
            {
                await Task.WhenAll(players.Skip(i).Take(parallel).Select(async p =>
                {
                    var pInfo = await _siteParser
                        .GetPlayerInformationAsync(p.UrlName, Player.DefaultPlayerHexColor)
                        .ConfigureAwait(false);

                    if (pInfo != null)
                    {
                        pInfo.Id = p.Id;
                        okPlayers.Add(new Player(pInfo));
                    }
                })).ConfigureAwait(false);
            }

            return okPlayers;
        }

        /// <inheritdoc />
        public async Task ScanTimePageForNewPlayersAsync(DateTime? stopAt)
        {
            var (validPlayers, bannedPlayers) = await GetPlayersAsync().ConfigureAwait(false);

            // Takes GoldenEye as default date (older than Perfect Dark)
            var allDatesToLoop = (stopAt ?? Game.GoldenEye.GetEliteFirstDate()).LoopBetweenDates(DateStep.Month).Reverse().ToList();

            var playersToCreate = new System.Collections.Concurrent.ConcurrentBag<string>();

            const int parallel = 8;
            for (var i = 0; i < allDatesToLoop.Count; i += parallel)
            {
                await Task.WhenAll(allDatesToLoop.Skip(i).Take(parallel).Select(async loopDate =>
                {
                    var results = await _siteParser
                        .ExtractTimeEntriesAsync(loopDate.Year, loopDate.Month, false)
                        .ConfigureAwait(false);

                    foreach (var entry in results)
                    {
                        if (!bannedPlayers.Any(p => p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase))
                            && !validPlayers.Any(p => p.UrlName.Equals(entry.PlayerUrlName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            playersToCreate.Add(entry.PlayerUrlName);
                        }
                    }
                })).ConfigureAwait(false);
            }

            foreach (var pName in playersToCreate.Distinct())
            {
                await _writeRepository
                    .InsertPlayerAsync(pName, Player.DefaultPlayerHexColor)
                    .ConfigureAwait(false);
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
        public async Task CheckPotentialBannedPlayersAsync()
        {
            var nonDirtyPlayers = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            const int parallel = 8;
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

        private async Task ExtractPlayerTimesAsync(Game game, PlayerDto player)
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

                var groupEntries = entries.GroupBy(e => (e.Stage, e.Level, e.Time, e.Engine));
                foreach (var group in groupEntries)
                {
                    var groupEntry = group.OrderBy(d => d.Date ?? DateTime.MaxValue).First();
                    await _writeRepository
                        .InsertTimeEntryAsync(groupEntry.ToEntry(player.Id), game)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
