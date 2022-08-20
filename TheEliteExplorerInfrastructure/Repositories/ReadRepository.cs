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
    public sealed class ReadRepository : BaseRepository, IReadRepository
    {
        private readonly IConnectionProvider _connectionProvider;

        public ReadRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                return await GetEntriesByCriteriaInternalAsync(stage, level, startDate, endDate).ConfigureAwait(false);
            }

            return await GetEntriesByCriteriaInternalAsync(stage, level, null, null).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage stage)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection
                    .QueryAsync<EntryDto>(
                       "SELECT id, date, level_id AS Level, player_id, " +
                       "stage_id AS Stage, system_id AS Engine, time " +
                       "FROM entry " +
                       "WHERE stage_id = @stage_id",
                       new
                       {
                           stage_id = (long)stage
                       },
                        commandType: CommandType.Text)
                    .ConfigureAwait(false);

                return results.ToList();
            }
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync()
        {
            return await GetPlayersInternalAsync(false, false).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync(bool withBanned)
        {
            var players = await GetPlayersInternalAsync(true, false).ConfigureAwait(false);
            if (withBanned)
            {
                players.AddRange(await GetPlayersInternalAsync(true, true).ConfigureAwait(false));
            }
            return players;
        }

        private async Task<List<PlayerDto>> GetPlayersInternalAsync(bool isDirty, bool isBanned)
        {
            using (var connection = _connectionProvider.TheEliteConnection)
            {
                var players = await connection
                    .QueryAsync<PlayerDto>(
                       "SELECT id, url_name, real_name, surname,  color, " +
                       "control_style, is_dirty, is_banned " +
                       "FROM player " +
                       "WHERE is_dirty = @is_dirty AND is_banned = @is_banned",
                       new
                       {
                           is_dirty = isDirty,
                           is_banned = isBanned
                       },
                        commandType: CommandType.Text)
                    .ConfigureAwait(false);

                return players.ToList();
            }
        }

        private async Task<List<EntryDto>> GetEntriesByCriteriaInternalAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<EntryDto>();

            using (var connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection
                    .QueryAsync<EntryDto>(
                       "SELECT id, date, level_id AS Level, player_id, " +
                       "stage_id AS Stage, system_id AS Engine, time " +
                       "FROM entry " +
                       "WHERE (@start_date IS NULL OR date >= @start_date) " +
                       "AND (@end_date IS NULL OR date < @end_date) " +
                       "AND (@stage_id IS NULL OR stage_id = @stage_id) " +
                       "AND (@level_id IS NULL OR level_id = @level_id)",
                       new
                       {
                           stage_id = (long?)stage,
                           level_id = (int?)level,
                           start_date = startDate,
                           end_date = endDate
                       },
                        commandType: CommandType.Text)
                    .ConfigureAwait(false);

                if (results != null)
                {
                    entries.AddRange(results);
                }
            }

            return entries;
        }
    }
}
