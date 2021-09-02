using System;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Integration provider interface.
    /// </summary>
    public interface IIntegrationProvider
    {
        /// <summary>
        /// Scans and inserts time entries for every known player (non dirty); previous entries are removed.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        Task ScanAllPlayersEntriesHistory(Game game);

        /// <summary>
        /// Scans and inserts time entries for a single player; previous entries are removed.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task ScanPlayerEntriesHistory(Game game, long playerId);

        /// <summary>
        /// Cleans players flagged as dirty.
        /// </summary>
        /// <returns>Nothing.</returns>
        Task CleanDirtyPlayers();

        /// <summary>
        /// Scans and inserts time entries
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="startDate">
        /// Start date; if <c>Null</c>, most recent date is used.
        /// Date is rounded to the full month.
        /// </param>
        /// <returns>Nothing.</returns>
        Task ScanTimePage(Game game, DateTime? startDate);
    }
}
