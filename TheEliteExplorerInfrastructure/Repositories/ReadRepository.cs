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
        private const string _getEntriesByGamePsName = "select_all_entry";
        private const string _getEntriesByCriteriaPsName = "select_entry";
        private const string _getEveryPlayersPsName = "select_player";

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
            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var players = await connection.QueryAsync<PlayerDto>(
                   ToPsName(_getEveryPlayersPsName),
                   new
                   {
                       is_dirty = isDirty,
                       is_banned = isBanned
                   },
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

                return players.ToList();
            }
        }

        private async Task<List<EntryDto>> GetEntriesByCriteriaInternalAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate)
        {
            var entries = new List<EntryDto>();

            using (IDbConnection connection = _connectionProvider.TheEliteConnection)
            {
                var results = await connection.QueryAsync<EntryDto>(
                   ToPsName(_getEntriesByCriteriaPsName),
                   new
                   {
                       stage_id = (long?)stage,
                       level_id = (int?)level,
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
