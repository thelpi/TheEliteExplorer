using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TheEliteExplorer.Infrastructure.Configuration;
using TheEliteExplorer.Infrastructure.Dtos;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// SQL context.
    /// </summary>
    /// <seealso cref="ISqlContext"/>
    public class SqlContext : ISqlContext
    {
        private const string _getPlayersCacheKey = "players";

        private const string _defaultPlayerColor = "000000";

        private const string _getEveryPlayersPsName = "[dbo].[select_player]";
        private const string _getEntriesByCriteriaPsName = "[dbo].[select_entry]";
        private const string _insertPlayerPsName = "[dbo].[insert_player]";
        private const string _insertEntryPsName = "[dbo].[insert_entry]";

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
            // TODO: it might not be the best place to put it
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, long levelId, DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<EntryDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   _getEntriesByCriteriaPsName,
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
        public async Task<long> InsertOrRetrieveTimeEntryAsync(long playerId, long levelId, long stageId, DateTime? date, long? time, long? systemId)
        {
            IReadOnlyCollection<EntryDto> entries = await GetEntriesAsync(stageId, levelId,
                date.HasValue ? date.Value.Date : default(DateTime?),
                date.HasValue ? date.Value.Date.AddDays(1) : default(DateTime?));

            EntryDto match = entries.FirstOrDefault(e => e.PlayerId == playerId && e.Time == time && e.SystemId == systemId);
            if (match != null)
            {
                return match.Id;
            }

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.AddDynamicParams(new
                {
                    player_id = playerId,
                    level_id = levelId,
                    stage_id = stageId,
                    date,
                    time,
                    system_id = systemId
                });
                dynamicParameters.Add("@id", dbType: DbType.Int64, direction: ParameterDirection.Output);

                await connection.QueryAsync(
                   _insertEntryPsName,
                   dynamicParameters,
                   commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                long id = dynamicParameters.Get<long>("@id");

                return id;
            }
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerAsync(string urlName, string realName, string surname, string color, string controlStyle)
        {
            return await InsertOrRetrievePlayerInternalAsync(urlName, realName, surname, color, controlStyle, false).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName)
        {
            return await InsertOrRetrievePlayerInternalAsync(urlName, urlName, urlName, _defaultPlayerColor, null, true).ConfigureAwait(false);
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
                   _getEveryPlayersPsName, new { },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                if (results != null)
                {
                    players.AddRange(results);
                }
            }

            return players;
        }

        private async Task<long> InsertOrRetrievePlayerInternalAsync(string urlName, string realName, string surname, string color, string controlStyle, bool isDirty)
        {
            IReadOnlyCollection<PlayerDto> players = await GetPlayersAsync().ConfigureAwait(false);

            PlayerDto match = players.FirstOrDefault(p => p.UrlName.Equals(urlName, StringComparison.InvariantCultureIgnoreCase));
            if (match != null)
            {
                return match.Id;
            }

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.AddDynamicParams(new
                {
                    url_name = urlName,
                    real_name = realName,
                    surname,
                    color,
                    control_style = controlStyle,
                    is_dirty = (isDirty ? 1 : 0)
                });
                dynamicParameters.Add("@id", dbType: DbType.Int64, direction: ParameterDirection.Output);

                await connection.QueryAsync(
                   _insertPlayerPsName,
                   dynamicParameters,
                   commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                long id = dynamicParameters.Get<long>("@id");

                // invalidates cache
                await _cache.RemoveAsync(_getPlayersCacheKey).ConfigureAwait(false);

                return id;
            }
        }
    }
}
