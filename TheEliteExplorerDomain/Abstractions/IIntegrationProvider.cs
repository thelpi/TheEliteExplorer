using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    public interface IIntegrationProvider
    {
        Task ScanAllPlayersEntriesHistoryAsync(Game game);

        Task ScanPlayerEntriesHistoryAsync(Game game, long playerId);

        Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync();

        Task CheckPotentialBannedPlayersAsync();

        Task ScanTimePageForNewPlayersAsync(DateTime? stopAt, bool addEntries);

        Task<bool> CleanDirtyPlayerAsync(long playerId);
    }
}
