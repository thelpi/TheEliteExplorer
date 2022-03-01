using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Integration provider interface.
    /// </summary>
    public interface IIntegrationProvider
    {
        /// <summary>
        /// Scans and inserts time entries for every known player; previous entries are removed.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        Task ScanAllPlayersEntriesHistoryAsync(Game game);

        /// <summary>
        /// Scans and inserts time entries for a single player; previous entries are removed.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task ScanPlayerEntriesHistoryAsync(Game game, long playerId);

        /// <summary>
        /// Gets dirty players with valid time page.
        /// </summary>
        /// <returns>Collection of players.</returns>
        Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayersAsync();

        /// <summary>
        /// Checks for non-dirty player that might be banned. Proceeds to mark them as dirty (but not banned, a manuel intervention is required for that).
        /// </summary>
        /// <returns>Nothing.</returns>
        Task CheckPotentialBannedPlayersAsync();

        /// <summary>
        /// Loops on every month (from now) of time page to find new players; does not integrate times.
        /// </summary>
        /// <param name="stopAt">Date to stop loop.</param>
        /// <returns>Nothing.</returns>
        Task ScanTimePageForNewPlayersAsync(DateTime? stopAt);

        /// <summary>
        /// Cleans a specified dirty player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns><c>True</c> if success; <c>False</c> otherwise.</returns>
        Task<bool> CleanDirtyPlayerAsync(long playerId);
    }
}
