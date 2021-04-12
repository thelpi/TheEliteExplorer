using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerInfrastructure.Configuration;

namespace TheEliteExplorerInfrastructure.Repositories
{
    /// <summary>
    /// Read operations in repository (default implementation).
    /// </summary>
    /// <seealso cref="IReadRepository"/>
    /// <seealso cref="BaseRepository"/>
    public sealed class ReadRepository : BaseRepository, IReadRepository
    {
        private const string _getPlayersCacheKey = "players";
        private const string _getEntriesCacheKeyFormat = "entries_{0}_{1}"; // stageId, levelId
        private const string _getAllEntriesCacheKeyFormat = "entries_all_{0}"; // stageId

        private const string _getStageLevelWr = "select_stage_level_wr";
        private const string _getRankingsPsName = "select_ranking";
        private const string _getEveryPlayersPsName = "select_player";
        private const string _getEntriesByCriteriaPsName = "select_entry";
        private const string _getLatestEntryDatePsName = "select_latest_entry_date";
        private const string _getLatestRankingDatePsName = "select_latest_ranking_date";
        private const string _getDuplicatePlayersPsName = "select_duplicate_players";
        private const string _getEntriesByGamePsName = "select_all_entry";

        private readonly IConnectionProvider _connectionProvider;
        private readonly CacheConfiguration _cacheConfiguration;
        private readonly IDistributedCache _cache;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionProvider">Instance of <see cref="IConnectionProvider"/>.</param>
        /// <param name="cacheConfiguration">Cache configuration.</param>
        /// <param name="cache">Instance of <see cref="IDistributedCache"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionProvider"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cacheConfiguration"/> or its inner value is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cache"/> is <c>Null</c>.</exception>
        public ReadRepository(
            IConnectionProvider connectionProvider,
            IOptions<CacheConfiguration> cacheConfiguration,
            IDistributedCache cache)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _cacheConfiguration = cacheConfiguration?.Value ?? throw new ArgumentNullException(nameof(cacheConfiguration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntries(long stageId, Level level, DateTime? startDate, DateTime? endDate)
        {
            if (!_cacheConfiguration.Enabled || startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesWithoutCache(stageId, level, startDate, endDate).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCache(
                string.Format(_getEntriesCacheKeyFormat, stageId, level),
                GetCacheOptions(),
                () => GetEntriesWithoutCache(stageId, level, null, null));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntries(long stageId)
        {
            if (!_cacheConfiguration.Enabled)
            {
                return await GetEntriesWithoutCache(stageId).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCache(
                string.Format(_getAllEntriesCacheKeyFormat, stageId),
                GetCacheOptions(),
                () => GetEntriesWithoutCache(stageId));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayers()
        {
            if (!_cacheConfiguration.Enabled)
            {
                return await GetPlayersWithoutCache(false).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCache(
                _getPlayersCacheKey,
                GetCacheOptions(),
                () => GetPlayersWithoutCache(false));
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetLatestEntryDate()
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var datas = await connection
                    .QueryAsync<DateTime?>(
                        ToPsName(_getLatestEntryDatePsName),
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
                return datas.FirstOrDefault();
            }
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetLatestRankingDate(Game game)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                DateTime? data = await connection.QuerySingleAsync<DateTime?>(
                    ToPsName(_getLatestRankingDatePsName), new { game_id = (int)game },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return data;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<DuplicatePlayerDto>> GetDuplicatePlayers()
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var duplicatePlayers = await connection.QueryAsync<DuplicatePlayerDto>(
                    ToPsName(_getDuplicatePlayersPsName),
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return duplicatePlayers.ToList();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayers()
        {
            return await GetPlayersWithoutCache(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<RankingDto>> GetStageLevelDateRankings(long stageId, Level level, DateTime date)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var rankings = await connection
                    .QueryAsync<RankingDto>(
                        ToPsName(_getRankingsPsName),
                        new
                        {
                            stage_id = stageId,
                            level_id = (long)level,
                            date
                        },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
                return rankings.ToList();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<WrDto>> GetStageLevelWrs(long stageId, Level level)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var wrs = await connection.QueryAsync<WrDto>(
                    ToPsName(_getStageLevelWr),
                    new
                    {
                        stage_id = stageId,
                        level_id = (long)level
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return wrs.ToList();
            }
        }

        private DistributedCacheEntryOptions GetCacheOptions()
        {
            return new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(_cacheConfiguration.MinutesBeforeExpiration));
        }

        private async Task<List<PlayerDto>> GetPlayersWithoutCache(bool isDirty)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var players = await connection.QueryAsync<PlayerDto>(
                   ToPsName(_getEveryPlayersPsName),
                   new
                   {
                       is_dirty = isDirty
                   },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                return players.ToList();
            }
        }

        private async Task<List<EntryDto>> GetEntriesWithoutCache(long stageId, Level level, DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<EntryDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByCriteriaPsName),
                   new
                   {
                       stage_id = stageId,
                       level_id = (int)level,
                       start_date = startDate,
                       end_date = endDate
                   },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                if (results != null)
                {
                    entries.AddRange(results);
                }
            }

            return entries;
        }

        private async Task<List<EntryDto>> GetEntriesWithoutCache(long stageId)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByGamePsName),
                   new
                   {
                       stage_id = stageId
                   },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                return results.ToList();
            }
        }
    }
}
