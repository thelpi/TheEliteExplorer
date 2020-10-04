using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerInfrastructure.Configuration;

namespace TheEliteExplorerInfrastructure
{
    /// <summary>
    /// SQL context.
    /// </summary>
    /// <seealso cref="ISqlContext"/>
    public class SqlContext : ISqlContext
    {
        private const string _getPlayersCacheKey = "players";
        private const string _getEntriesCacheKeyFormat = "entries_{0}_{1}"; // stageId, levelId

        private const string _getEveryPlayersPsName = "select_player";
        private const string _getEntriesByCriteriaPsName = "select_entry";
        private const string _insertPlayerPsName = "insert_player";
        private const string _insertEntryPsName = "insert_entry";
        private const string _getLatestDateName = "select_latest_entry_date";

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
        public SqlContext(IConnectionProvider connectionProvider,
            IOptions<CacheConfiguration> cacheConfiguration,
            IDistributedCache cache)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _cacheConfiguration = cacheConfiguration?.Value ?? throw new ArgumentNullException(nameof(cacheConfiguration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, long levelId, DateTime? startDate, DateTime? endDate)
        {
            if (!_cacheConfiguration.Enabled || startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesWithoutCacheAsync(stageId, levelId, startDate, endDate).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCacheAsync(
                string.Format(_getEntriesCacheKeyFormat, stageId, levelId),
                GetCacheOptions(),
                () => GetEntriesWithoutCacheAsync(stageId, levelId, null, null));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync()
        {
            if (!_cacheConfiguration.Enabled)
            {
                return await GetPlayersWithoutCacheAsync().ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCacheAsync(
                _getPlayersCacheKey,
                GetCacheOptions(),
                GetPlayersWithoutCacheAsync);
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrieveTimeEntryAsync(EntryDto requestEntry)
        {
            if (requestEntry == null)
            {
                throw new ArgumentNullException(nameof(requestEntry));
            }

            IReadOnlyCollection<EntryDto> entries = await GetEntriesAsync(requestEntry.StageId, requestEntry.LevelId,
                requestEntry.Date?.Date,
                requestEntry.Date?.Date.AddDays(1));

            EntryDto match = entries.FirstOrDefault(e => e.PlayerId == requestEntry.PlayerId
                && e.Time == requestEntry.Time
                && e.SystemId == requestEntry.SystemId);
            if (match != null)
            {
                return match.Id;
            }

            long entryid = await InsertAndGetIdAsync(
                _insertEntryPsName,
                new
                {
                    player_id = requestEntry.PlayerId,
                    level_id = requestEntry.LevelId,
                    stage_id = requestEntry.StageId,
                    requestEntry.Date,
                    requestEntry.Time,
                    system_id = requestEntry.SystemId
                }).ConfigureAwait(false);

            // invalidates cache
            await _cache
                .RemoveAsync(string.Format(_getEntriesCacheKeyFormat, requestEntry.StageId, requestEntry.LevelId))
                .ConfigureAwait(false);

            return entryid;
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerAsync(PlayerDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return await InsertOrRetrievePlayerInternalAsync(dto.UrlName, dto.RealName, dto.SurName, dto.Color, dto.ControlStyle, false, dto.JoinDate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName, DateTime? joinDate)
        {
            return await InsertOrRetrievePlayerInternalAsync(urlName, urlName, urlName, Player.DefaultPlayerHexColor, null, true, joinDate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DateTime> GetLatestEntryDateAsync()
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                IEnumerable<DateTime> data = await connection.QueryAsync<DateTime>(
                    ToPsName(_getLatestDateName),
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return data.First();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesForEachStageAndLevelAsync(Game game)
        {
            var entries = new List<EntryDto>();

            foreach (Level level in SystemExtensions.Enumerate<Level>())
            {
                foreach (Stage stage in Stage.Get(game))
                {
                    entries.AddRange(
                        await GetEntriesAsync(stage.Position, (long)level, null, null).ConfigureAwait(false)
                    );
                }
            }

            return entries;
        }

        private string ToPsName(string baseName)
        {
            return $"[dbo].[{baseName}]";
        }

        private DistributedCacheEntryOptions GetCacheOptions()
        {
            return new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(_cacheConfiguration.MinutesBeforeExpiration));
        }

        private async Task<List<PlayerDto>> GetPlayersWithoutCacheAsync()
        {
            var players = new List<PlayerDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<PlayerDto>(
                   ToPsName(_getEveryPlayersPsName), new { },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                if (results != null)
                {
                    players.AddRange(results);
                }
            }

            return players;
        }

        private async Task<long> InsertOrRetrievePlayerInternalAsync(string urlName, string realName, string surname, string color, string controlStyle, bool isDirty, DateTime? joinDate)
        {
            IReadOnlyCollection<PlayerDto> players = await GetPlayersAsync().ConfigureAwait(false);

            PlayerDto match = players.FirstOrDefault(p => p.UrlName.Equals(urlName, StringComparison.InvariantCultureIgnoreCase));
            if (match != null)
            {
                return match.Id;
            }

            long id = await InsertAndGetIdAsync(
                _insertPlayerPsName,
                new
                {
                    url_name = urlName,
                    real_name = realName,
                    surname,
                    color,
                    control_style = controlStyle,
                    is_dirty = (isDirty ? 1 : 0),
                    join_date = joinDate?.Date
                }).ConfigureAwait(false);

            // invalidates cache
            await _cache.RemoveAsync(_getPlayersCacheKey).ConfigureAwait(false);

            return id;
        }

        private async Task<long> InsertAndGetIdAsync(string psNameBase, object lambdaParameters)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.AddDynamicParams(lambdaParameters);
                dynamicParameters.Add("@id", dbType: DbType.Int64, direction: ParameterDirection.Output);

                await connection.QueryAsync(
                   ToPsName(psNameBase),
                   dynamicParameters,
                   commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                return dynamicParameters.Get<long>("@id");
            }
        }

        private async Task<List<EntryDto>> GetEntriesWithoutCacheAsync(long stageId, long levelId, DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<EntryDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByCriteriaPsName),
                   new
                   {
                       stage_id = stageId,
                       level_id = levelId,
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
    }
}
