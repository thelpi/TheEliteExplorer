﻿using System;
using System.Data;
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
        private const string _insertPlayerPsName = "insert_player";
        private const string _insertEntryPsName = "insert_entry";
        private const string _updateDirtyPlayerPsName = "update_dirty_player";
        private const string _updateCleanPlayerPsName = "update_player";
        private const string _updatePlayersJoinDatePsName = "update_player_join_date";
        private const string _deletePlayerEntriesPsName = "delete_player_entry";
        private const string _updateEntryDate = "update_entry_date";

        private readonly IConnectionProvider _connectionProvider;

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
        public async Task<long> InsertTimeEntryAsync(EntryDto requestEntry, Game game)
        {
            if (requestEntry == null)
            {
                throw new ArgumentNullException(nameof(requestEntry));
            }

            long entryid = await InsertAndGetIdAsync(
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
        public async Task<long> InsertPlayerAsync(string urlName, DateTime? joinDate, string defaultHexColor)
        {
            return await InsertAndGetIdAsync(
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
        public async Task UpdateEntryDateAsync(long entryId, DateTime date)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        ToPsName(_updateEntryDate),
                        new { date, entryId },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task UpdateDirtyPlayerAsync(long playerId)
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

        /// <inheritdoc />
        public async Task DeletePlayerStageEntriesAsync(Stage stage, long playerId)
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
        public async Task CleanPlayerAsync(PlayerDto player)
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        ToPsName(_updateCleanPlayerPsName),
                        new
                        {
                            id = player.Id,
                            real_name = player.RealName,
                            surname = player.SurName,
                            color = player.Color,
                            control_style = player.ControlStyle
                        },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task ResetPlayersJoinDateAsync()
        {
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        ToPsName(_updatePlayersJoinDatePsName),
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
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
