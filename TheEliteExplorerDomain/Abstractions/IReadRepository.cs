﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Read operations in repository (interface).
    /// </summary>
    public interface IReadRepository
    {
        /// <summary>
        /// Gets every players from the database.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync();

        /// <summary>
        /// Gets every entries for a specified stage and level, between two dates.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage stage, Level level, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Gets every entry for a specified stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(Stage stage);

        /// <summary>
        /// Gets entries count for a specified stage and an optional level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level (<c>Null</c> for every level).</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <returns>Entries count.</returns>
        Task<int> GetEntriesCountAsync(Stage stage, Level? level, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Gets the most recent entry date.
        /// </summary>
        /// <returns>Most recent entry date</returns>
        Task<DateTime?> GetLatestEntryDateAsync();

        /// <summary>
        /// Gets every dirty player.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync();

        /// <summary>
        /// Gets the ranking for a stage and level at a specified date.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <param name="noDateRule">Rule to apply for empty dates.</param>
        /// <returns>Collection of <see cref="RankingBaseDto"/>.</returns>
        Task<IReadOnlyCollection<RankingBaseDto>> GetStageLevelRankingAsync(Stage stage, Level level, DateTime rankingDate, NoDateEntryRankingRule noDateRule);
    }
}
