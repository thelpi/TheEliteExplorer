using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    public interface IWriteRepository
    {
        Task<long> InsertTimeEntryAsync(EntryDto requestEntry);

        Task<long> InsertPlayerAsync(string urlName, string defaultHexColor);

        Task DeletePlayerStageEntriesAsync(Stage stage, long playerId);

        Task UpdateDirtyPlayerAsync(long playerId);

        Task CleanPlayerAsync(PlayerDto player);
    }
}
