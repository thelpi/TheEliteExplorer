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
        /// Gets dirty players with valid time page.
        /// </summary>
        /// <returns>Collection of players.</returns>
        Task<IReadOnlyCollection<Player>> GetCleanableDirtyPlayers();

        /// <summary>
        /// Checks players for dirt
        /// </summary>
        /// <returns>Nothing.</returns>
        Task CheckDirtyPlayers();

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

        /// <summary>
        /// Scans every time entry of a stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns>Nothing.</returns>
        Task ScanStageTimes(Stage stage);
    }
}
