using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerInfrastructure.Repositories
{
    /// <summary>
    /// Write operations in repository (default implementation).
    /// </summary>
    /// <seealso cref="IWriteRepository"/>
    /// <seealso cref="BaseRepository"/>
    public sealed class WriteRepository : BaseRepository, IWriteRepository
    {
        private const string _getPlayersCacheKey = "players";
        private const string _getEntriesCacheKeyFormat = "entries_{0}_{1}"; // stageId, levelId
        private const string _getAllEntriesCacheKeyFormat = "entries_all_{0}"; // stageId
        
        private const string _insertPlayerPsName = "insert_player";
        private const string _insertEntryPsName = "insert_entry";
        private const string _insertRankingPsName = "insert_ranking";
        private const string _updateEntryPlayerPsName = "update_entry_player";
        private const string _deletePlayerPsName = "delete_player";
        private const string _deleteRankingPsName = "delete_ranking";
        private const string _deletePlayerEntriesPsName = "delete_player_entry";
        private const string _updatePlayerPsName = "update_player";
        private const string _deleteStageLevelWrPsName = "delete_stage_level_wr";
        private const string _insertStageLevelWrPsName = "insert_wr";
        private const string _updateDirtyPlayerPsName = "update_dirty_player";

        private const string ColSeparator = ",";
        private const string RowSeparator = "\r\n";
        private static readonly string DataFilePath = Path.Combine(Environment.CurrentDirectory, "bulk_datas.csv");

        private readonly IConnectionProvider _connectionProvider;
        //private readonly IDistributedCache _cache;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionProvider">Instance of <see cref="IConnectionProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionProvider"/> is <c>Null</c>.</exception>
        public WriteRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        /// <inheritdoc />
        public async Task<long> InsertTimeEntry(EntryDto requestEntry, Game game)
        {
            if (requestEntry == null)
            {
                throw new ArgumentNullException(nameof(requestEntry));
            }

            long entryid = await InsertAndGetId(
                _insertEntryPsName,
                new
                {
                    player_id = requestEntry.PlayerId,
                    level_id = (long)requestEntry.Level,
                    stage_id = (long)requestEntry.Stage,
                    requestEntry.Date,
                    requestEntry.Time,
                    system_id = requestEntry.Engine.HasValue ? (long)requestEntry.Engine.Value : default(long?)
                }).ConfigureAwait(false);

            return entryid;
        }

        /// <inheritdoc />
        public async Task<long> InsertPlayer(string urlName, DateTime? joinDate, string defaultHexColor)
        {
            return await InsertAndGetId(
                    _insertPlayerPsName,
                    new
                    {
                        url_name = urlName,
                        real_name = urlName,
                        surname = urlName,
                        color = defaultHexColor,
                        control_style = default(string),
                        is_dirty = 1,
                        join_date = joinDate?.Date
                    })
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task InsertRanking(RankingDto ranking)
        {
            if (ranking == null)
            {
                throw new ArgumentNullException(nameof(ranking));
            }

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                   ToPsName(_insertRankingPsName),
                   new
                   {
                       date = ranking.Date,
                       level_id = (long)ranking.Level,
                       player_id = ranking.PlayerId,
                       rank = ranking.Rank,
                       stage_id = (long)ranking.Stage,
                       time = ranking.Time
                   },
                   commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task BulkInsertRankings(IReadOnlyCollection<RankingDto> rankings)
        {
            if (rankings == null)
            {
                throw new ArgumentNullException(nameof(rankings));
            }

            if (rankings.Count == 0)
            {
                throw new ArgumentException($"{nameof(rankings)} is empty.", nameof(rankings));
            }

            var itemsColumns = rankings
                .Select(r => new[]
                {
                    ((long)r.Stage).ToString(),
                    ((long)r.Level).ToString(),
                    r.Date.ToString("yyyy-MM-dd hh:mm:ss"),
                    r.PlayerId.ToString(),
                    r.Time.ToString(),
                    r.Rank.ToString()
                })
                .ToList();

            await BulkInsertInternal(itemsColumns, "ranking");
        }

        /// <inheritdoc />
        public async Task DeletePlayer(long id)
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
        public async Task UpdatePlayerEntries(long currentPlayerId, long newPlayerId)
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
        public async Task UpdatePlayerInformation(PlayerDto player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

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
        public async Task DeleteStageLevelRankingHistory(Stage stage, Level level)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_deleteRankingPsName),
                    new
                    {
                        stage_id = (long)stage,
                        level_id = (long)level
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task DeletePlayerStageEntries(Stage stage, long playerId)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_deletePlayerEntriesPsName),
                    new
                    {
                        stage_id = (long)stage,
                        player_id = playerId
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task InsertWr(WrDto wr)
        {
            if (wr == null)
            {
                throw new ArgumentNullException(nameof(wr));
            }

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_insertStageLevelWrPsName),
                    new
                    {
                        stage_id = (long)wr.Stage,
                        level_id = (long)wr.Level,
                        player_id = wr.PlayerId,
                        date = wr.Date,
                        time = wr.Time,
                        untied = wr.Untied,
                        first_tied = wr.FirstTied
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task DeleteStageLevelWr(Stage stage, Level level)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection.QueryAsync(
                    ToPsName(_deleteStageLevelWrPsName),
                    new
                    {
                        stage_id = (long)stage,
                        level_id = (long)level
                    },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task UpdateEntryDate(long entryId, DateTime date)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                // lazy !
                await connection
                    .QueryAsync(
                        "UPDATE [dbo].[entry] SET [date] = @date WHERE [id] = @entryId",
                        new { date, entryId })
                    .ConfigureAwait(false);
            }
        }

        private async Task<long> InsertAndGetId(string psNameBase, object lambdaParameters)
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

        private async Task BulkInsertInternal(IReadOnlyCollection<string[]> itemsColumns, string tableName)
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

        /// <inheritdoc />
        public async Task UpdateDirtyPlayer(long playerId)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        ToPsName(_updateDirtyPlayerPsName),
                        new { id = playerId },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
        }
    }
}
