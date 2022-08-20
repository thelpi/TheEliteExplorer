using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerInfrastructure.Repositories
{
    public sealed class WriteRepository : BaseRepository, IWriteRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public WriteRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        public async Task<long> InsertTimeEntryAsync(EntryDto requestEntry)
        {
            if (requestEntry == null)
            {
                throw new ArgumentNullException(nameof(requestEntry));
            }

            try
            {
                long entryid = await InsertAndGetIdAsync(
                        "INSERT INTO entry " +
                        "(player_id, level_id, stage_id, date, time, system_id, creation_date) " +
                        "VALUES " +
                        "(@player_id, @level_id, @stage_id, @date, @time, @system_id, NOW())",
                        new
                        {
                            player_id = requestEntry.PlayerId,
                            level_id = (long)requestEntry.Level,
                            stage_id = (long)requestEntry.Stage,
                            requestEntry.Date,
                            requestEntry.Time,
                            system_id = (long)requestEntry.Engine
                        })
                    .ConfigureAwait(false);

                return entryid;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return 0;
            }
        }

        public async Task<long> InsertPlayerAsync(string urlName, string defaultHexColor)
        {
            return await InsertAndGetIdAsync(
                    "INSERT INTO player " +
                    "(url_name, real_name, surname, color, control_style, is_dirty, creation_date) " +
                    "VALUES " +
                    "(@url_name, @real_name, @surname, @color, @control_style, @is_dirty, NOW())",
                    new
                    {
                        url_name = urlName,
                        real_name = urlName,
                        surname = urlName,
                        color = defaultHexColor,
                        control_style = default(string),
                        is_dirty = 1
                    })
                .ConfigureAwait(false);
        }

        public async Task UpdateDirtyPlayerAsync(long playerId)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        "UPDATE player SET is_dirty = 1 WHERE id = @id",
                        new { id = playerId },
                        commandType: CommandType.Text)
                    .ConfigureAwait(false);
            }
        }

        public async Task DeletePlayerStageEntriesAsync(Stage stage, long playerId)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        "DELETE FROM entry " +
                        "WHERE player_id = @player_id AND stage_id = @stage_id",
                        new
                        {
                            stage_id = (long)stage,
                            player_id = playerId
                        },
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
        }

        public async Task CleanPlayerAsync(PlayerDto player)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                        "UPDATE player " +
                        "SET real_name = @real_name, surname = @surname, color = @color, " +
                        "control_style = @control_style, is_dirty = 0 " +
                        "WHERE id = @id",
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

        private async Task<long> InsertAndGetIdAsync(string sql, object lambdaParameters)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                await connection
                    .QueryAsync(
                       sql, lambdaParameters, commandType: CommandType.Text)
                    .ConfigureAwait(false);

                var results = await connection
                    .QueryAsync<long>(
                        "SELECT LAST_INSERT_ID()", commandType: CommandType.Text)
                    .ConfigureAwait(false);

                return results.FirstOrDefault();
            }
        }
    }
}
