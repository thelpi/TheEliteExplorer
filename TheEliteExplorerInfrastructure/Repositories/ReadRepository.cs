using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

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
        private const string _getDuplicatePlayersPsName = "select_duplicate_players";
        private const string _getEntriesByGamePsName = "select_all_entry";
        private const string _getEntriesCountPsName = "select_entry_count";
        private const string _getEntriesByPlayerPsName = "select_player_entry";
        private const string _getEntriesByEntryPsName = "select_entry_by_entry";

        private readonly IConnectionProvider _connectionProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionProvider">Instance of <see cref="IConnectionProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionProvider"/> is <c>Null</c>.</exception>
        public ReadRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntries(Stage stage, Level level, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesWithoutCache(stage, level, startDate, endDate).ConfigureAwait(false);
            }

            return await GetEntriesWithoutCache(stage, level, null, null).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetPlayerEntries(long playerId)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                       ToPsName(_getEntriesByPlayerPsName),
                       new { player_id = playerId },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);

                return results?.ToList() ?? new List<EntryDto>();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesByEntry(long entryId)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                       ToPsName(_getEntriesByEntryPsName),
                       new { entry_id = entryId },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);

                return results?.ToList() ?? new List<EntryDto>();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntries(Stage stage)
        {
            return await GetEntriesWithoutCache(stage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayers()
        {
            return await GetPlayersWithoutCache(false).ConfigureAwait(false);
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
        public async Task<IReadOnlyCollection<RankingDto>> GetStageLevelDateRankings(Stage stage, Level level, DateTime date)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var rankings = await connection
                    .QueryAsync<RankingDto>(
                        ToPsName(_getRankingsPsName),
                        new
                        {
                            stage_id = (long)stage,
                            level_id = (long)level,
                            date
                        },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
                return rankings.ToList();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<WrDto>> GetStageLevelWrs(Stage stage, Level level)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var wrs = await connection.QueryAsync<WrDto>(
                    ToPsName(_getStageLevelWr),
                    new
                    {
                        stage_id = (long)stage,
                        level_id = (long)level
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return wrs.ToList();
            }
        }

        /// <inheritdoc />
        public async Task<int> GetEntriesCount(Stage stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                return await connection
                    .QueryFirstAsync<int>(
                        ToPsName(_getEntriesCountPsName),
                        new
                        {
                            stage_id = (long)stage,
                            level_id = (long?)level,
                            start_date = startDate,
                            end_date = endDate
                        },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
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

        private async Task<List<EntryDto>> GetEntriesWithoutCache(Stage stage, Level level, DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<EntryDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByCriteriaPsName),
                   new
                   {
                       stage_id = (long)stage,
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

        private async Task<List<EntryDto>> GetEntriesWithoutCache(Stage stage)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByGamePsName),
                   new
                   {
                       stage_id = (long)stage
                   },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                return results.ToList();
            }
        }
    }
}
