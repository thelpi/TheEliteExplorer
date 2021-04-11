using System;
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
        /// Scans and inserts time entries for a single player; previous entries are removed.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task ScanPlayerEntriesHistory(Game game, long playerId);

        /// <summary>
        /// Scans and inserts time entries for a stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="clear">Clears previous entries y/n.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stage"/> is <c>Null</c>.</exception>
        Task ScanStageTimesAsync(Stage stage, bool clear);

        /// <summary>
        /// Cleans players flagged as dirty.
        /// </summary>
        /// <returns>Nothing.</returns>
        Task CleanDirtyPlayersAsync();

        /// <summary>
        /// Scans and inserts time entries
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="startDate">
        /// Start date; if <c>Null</c>, most recent date is used.
        /// Date is rounded to the full month.
        /// </param>
        /// <returns>Nothing.</returns>
        Task ScanTimePageAsync(Game game, DateTime? startDate);
    }
}
