﻿using System;
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
        Task<IReadOnlyCollection<Dtos.PlayerDto>> GetCleanableDirtyPlayersAsync();

        /// <summary>
        /// Checks players for dirt
        /// </summary>
        /// <returns>Nothing.</returns>
        Task CheckDirtyPlayersAsync();

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

        /// <summary>
        /// Scans every time entry of a stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns>Nothing.</returns>
        Task ScanStageTimesAsync(Stage stage);

        /// <summary>
        /// Cleans a specified dirty player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns><c>True</c> if success; <c>False</c> otherwise.</returns>
        Task<bool> CleanDirtyPlayerAsync(long playerId);
    }
}
