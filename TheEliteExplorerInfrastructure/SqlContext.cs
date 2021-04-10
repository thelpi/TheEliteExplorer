﻿using System;
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
        private const string _getAllEntriesCacheKeyFormat = "entries_all_{0}"; // gameId

        private const string _getRankingsPsName = "select_ranking";
        private const string _getEveryPlayersPsName = "select_player";
        private const string _getEntriesByCriteriaPsName = "select_entry";
        private const string _insertPlayerPsName = "insert_player";
        private const string _insertEntryPsName = "insert_entry";
        private const string _getLatestEntryDatePsName = "select_latest_entry_date";
        private const string _insertRankingPsName = "insert_ranking";
        private const string _getLatestRankingDatePsName = "select_latest_ranking_date";
        private const string _updateEntryPlayerPsName = "update_entry_player";
        private const string _selectDuplicatePlayersPsName = "select_duplicate_players";
        private const string _deletePlayerPsName = "delete_player";
        private const string _deleteRankingPsName = "delete_ranking";
        private const string _updatePlayerPsName = "update_player";
        private const string _getEntriesByGamePsName = "select_all_entry";

        private const string ColSeparator = ",";
        private const string RowSeparator = "\r\n";
        private static readonly string DataFilePath = Path.Combine(Environment.CurrentDirectory, "bulk_datas.csv");

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
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, Level level, DateTime? startDate, DateTime? endDate)
        {
            if (!_cacheConfiguration.Enabled || startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesWithoutCacheAsync(stageId, level, startDate, endDate).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCacheAsync(
                string.Format(_getEntriesCacheKeyFormat, stageId, level),
                GetCacheOptions(),
                () => GetEntriesWithoutCacheAsync(stageId, level, null, null));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long gameId)
        {
            if (!_cacheConfiguration.Enabled)
            {
                return await GetEntriesWithoutCacheAsync(gameId).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCacheAsync(
                string.Format(_getAllEntriesCacheKeyFormat, gameId),
                GetCacheOptions(),
                () => GetEntriesWithoutCacheAsync(gameId));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync()
        {
            if (!_cacheConfiguration.Enabled)
            {
                return await GetPlayersWithoutCacheAsync(false).ConfigureAwait(false);
            }

            return await _cache.GetOrSetFromCacheAsync(
                _getPlayersCacheKey,
                GetCacheOptions(),
                () => GetPlayersWithoutCacheAsync(false));
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrieveTimeEntryAsync(EntryDto requestEntry, long gameId)
        {
            IReadOnlyCollection<EntryDto> entries = await GetEntriesAsync(
                requestEntry.StageId,
                (Level)requestEntry.LevelId,
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

            // invalidates caches
            await _cache
                .RemoveAsync(string.Format(_getEntriesCacheKeyFormat, requestEntry.StageId, requestEntry.LevelId))
                .ConfigureAwait(false);
            await _cache
                .RemoveAsync(string.Format(_getAllEntriesCacheKeyFormat, gameId))
                .ConfigureAwait(false);

            return entryid;
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerAsync(PlayerDto dto)
        {
            return await InsertOrRetrievePlayerInternalAsync(dto.UrlName, dto.RealName, dto.SurName, dto.Color, dto.ControlStyle, false, dto.JoinDate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName, DateTime? joinDate, string defaultHexColor)
        {
            return await InsertOrRetrievePlayerInternalAsync(urlName, urlName, urlName, defaultHexColor, null, true, joinDate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DateTime> GetLatestEntryDateAsync()
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                IEnumerable<DateTime> data = await connection.QueryAsync<DateTime>(
                    ToPsName(_getLatestEntryDatePsName),
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return data.First();
            }
        }

        /// <inheritdoc />
        public async Task InsertRankingAsync(RankingDto ranking)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                   ToPsName(_insertRankingPsName),
                   new
                   {
                       date = ranking.Date,
                       level_id = ranking.LevelId,
                       player_id = ranking.PlayerId,
                       rank = ranking.Rank,
                       stage_id = ranking.StageId,
                       time = ranking.Time
                   },
                   commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task BulkInsertRankingsAsync(IReadOnlyCollection<RankingDto> rankings)
        {
            var itemsColumns = rankings
                .Select(r => new[]
                {
                    r.StageId.ToString(),
                    r.LevelId.ToString(),
                    r.Date.ToString("yyyy-MM-dd hh:mm:ss"),
                    r.PlayerId.ToString(),
                    r.Time.ToString(),
                    r.Rank.ToString()
                })
                .ToList();

            await BulkInsertInternalAsync(itemsColumns, "ranking");
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetLatestRankingDateAsync(Game game)
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
        public async Task<IReadOnlyCollection<DuplicatePlayerDto>> GetDuplicatePlayersAsync()
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var duplicatePlayers = await connection.QueryAsync<DuplicatePlayerDto>(
                    ToPsName(_selectDuplicatePlayersPsName),
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return duplicatePlayers.ToList();
            }
        }

        /// <inheritdoc />
        public async Task DeletePlayerAsync(long id)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_deletePlayerPsName),
                    new { id },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task UpdatePlayerEntriesAsync(long currentPlayerId, long newPlayerId)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_updateEntryPlayerPsName),
                    new
                    {
                        current_player_id = currentPlayerId,
                        new_player_id = newPlayerId
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task UpdatePlayerInformationAsync(PlayerDto player)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_updatePlayerPsName),
                    new
                    {
                        id = player.Id,
                        real_name = player.RealName,
                        surname = player.SurName,
                        color = player.Color,
                        control_style = player.ControlStyle
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync()
        {
            return await GetPlayersWithoutCacheAsync(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteStageLevelRankingHistory(long stageId, Level level)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_deleteRankingPsName),
                    new
                    {
                        stage_id = stageId,
                        level_id = (long)level
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
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

        private string ToPsName(string baseName)
        {
            return $"[dbo].[{baseName}]";
        }

        private DistributedCacheEntryOptions GetCacheOptions()
        {
            return new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(_cacheConfiguration.MinutesBeforeExpiration));
        }

        private async Task<List<PlayerDto>> GetPlayersWithoutCacheAsync(bool isDirty)
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

        private async Task<long> InsertOrRetrievePlayerInternalAsync(string urlName, string realName, string surname, string color, string controlStyle, bool isDirty, DateTime? joinDate)
        {
            var players = await GetPlayersWithoutCacheAsync(false).ConfigureAwait(false);
            var dirtyPlayers = await GetPlayersWithoutCacheAsync(true).ConfigureAwait(false);

            var match = players
                .Concat(dirtyPlayers)
                .FirstOrDefault(p =>
                    p.UrlName.Equals(urlName, StringComparison.InvariantCultureIgnoreCase));
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

            if (!isDirty)
            {
                // invalidates cache
                // dirty players are not cached
                await _cache.RemoveAsync(_getPlayersCacheKey).ConfigureAwait(false);
            }

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

        private async Task<List<EntryDto>> GetEntriesWithoutCacheAsync(long stageId, Level level, DateTime? startDate, DateTime? endDate)
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

        private async Task<List<EntryDto>> GetEntriesWithoutCacheAsync(long gameId)
        {
            var entries = new List<EntryDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByGamePsName),
                   new
                   {
                       game_id = gameId
                   },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                if (results != null)
                {
                    entries.AddRange(results);
                }
            }

            return entries;
        }

        private async Task BulkInsertInternalAsync(IReadOnlyCollection<string[]> itemsColumns, string tableName)
        {
            using (var sw = new StreamWriter(DataFilePath))
            {
                sw.NewLine = "\r\n";
                foreach (var itemColumns in itemsColumns)
                {
                    sw.WriteLine(string.Join(ColSeparator, itemColumns));
                }
            }

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .ExecuteAsync($"BULK INSERT [dbo].[{tableName}]" +
                        $"FROM '{DataFilePath}'" +
                        $"WITH" +
                        $"(" +
                        $"FIELDTERMINATOR = '{ColSeparator}'," +
                        $"ROWTERMINATOR = '{RowSeparator}'" +
                        $"); ")
                    .ConfigureAwait(false);
            }

            File.Delete(DataFilePath);
        }
    }
}
