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
        private const string _getEntriesByGamePsName = "select_all_entry";
        private const string _getEntriesByCriteriaPsName = "select_entry";
        private const string _getEntriesCountPsName = "select_entry_count";
        private const string _getLatestEntryDatePsName = "select_latest_entry_date";
        private const string _getEveryPlayersPsName = "select_player";
        private const string _getStageLevelRankingPsName = "select_stage_level_ranking";

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
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage stage, Level level, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesByCriteriaInternalAsync(stage, level, startDate, endDate).ConfigureAwait(false);
            }

            return await GetEntriesByCriteriaInternalAsync(stage, level, null, null).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage stage)
        {
            return await GetStageEntriesInternalAsync(stage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync()
        {
            return await GetPlayersInternalAsync(false).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetLatestEntryDateAsync()
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
        public async Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync()
        {
            return await GetPlayersInternalAsync(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetEntriesCountAsync(Stage stage, Level? level, DateTime? startDate, DateTime? endDate)
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

        /// <inheritdoc />
        public async Task<IReadOnlyList<RankingBaseDto>> GetStageLevelRankingAsync(Stage stage, Level level, DateTime rankingDate, NoDateEntryRankingRule noDateRule)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                return (
                    await connection
                        .QueryAsync<RankingBaseDto>(
                            ToPsName(_getStageLevelRankingPsName),
                            new
                            {
                                stage_id = (long)stage,
                                level_id = (long)level,
                                date = rankingDate,
                                rule_id = (long)noDateRule
                            },
                            commandType: CommandType.StoredProcedure)
                        .ConfigureAwait(false)
                ).ToList();
            }
        }

        private async Task<List<PlayerDto>> GetPlayersInternalAsync(bool isDirty)
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

        private async Task<List<EntryDto>> GetEntriesByCriteriaInternalAsync(Stage stage, Level level, DateTime? startDate, DateTime? endDate)
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

        private async Task<List<EntryDto>> GetStageEntriesInternalAsync(Stage stage)
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
