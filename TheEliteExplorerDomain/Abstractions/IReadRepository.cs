using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    public interface IReadRepository
    {
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync();
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage? stage, Level? level, DateTime? startDate, DateTime? endDate);
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage stage);
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync(bool withBanned);
    }
}
