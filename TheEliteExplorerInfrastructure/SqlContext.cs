using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
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

            return await InsertAndGetIdAsync(
                _insertEntryPsName,
                new
                {
                    player_id = playerId,
                    level_id = levelId,
                    stage_id = stageId,
                    date,
                    time,
                    system_id = systemId
                }).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerAsync(string urlName, string realName, string surname, string color, string controlStyle)
        {
            return await InsertOrRetrievePlayerInternalAsync(urlName, realName, surname, color, controlStyle, false).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName)
        {
            return await InsertOrRetrievePlayerInternalAsync(urlName, urlName, urlName, Player.DefaultPlayerHexColor, null, true).ConfigureAwait(false);
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

        private async Task<long> InsertOrRetrievePlayerInternalAsync(string urlName, string realName, string surname, string color, string controlStyle, bool isDirty)
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
                    is_dirty = (isDirty ? 1 : 0)
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
    }
}
