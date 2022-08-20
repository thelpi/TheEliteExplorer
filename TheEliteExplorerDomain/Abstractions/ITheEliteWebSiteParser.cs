using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    public interface ITheEliteWebSiteParser
    {
        Task<IReadOnlyCollection<EntryWebDto>> GetMonthPageTimeEntriesAsync(int year, int month);

        Task<PlayerDto> GetPlayerInformationAsync(string urlName, string defaultHexPlayer);

        Task<IReadOnlyCollection<EntryWebDto>> GetPlayerEntriesAsync(Game game, string playerUrlName);

        Task<Engine> GetTimeEntryEngineAsync(string url);
    }
}
