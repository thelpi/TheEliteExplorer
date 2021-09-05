using System;
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
        /// Gets world records for a stage and a level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <returns>Collection of world records.</returns>
        Task<IReadOnlyCollection<WrDto>> GetStageLevelWrs(Stage stage, Level level);

        /// <summary>
        /// Gets every players from the database.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetPlayers();

        /// <summary>
        /// Gets every entries for a specified stage and level, between two dates.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntries(Stage stage, Level level, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Gets every entry for a specified stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntries(Stage stage);

        /// <summary>
        /// Gets entries count for a specified stage and an optional level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level (<c>Null</c> for every level).</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <returns>Entries count.</returns>
        Task<int> GetEntriesCount(Stage stage, Level? level, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Gets the most recent entry date.
        /// </summary>
        /// <returns>Most recent entry date</returns>
        Task<DateTime?> GetLatestEntryDate();

        /// <summary>
        /// Gets duplicate players.
        /// </summary>
        /// <returns>A collection of <see cref="DuplicatePlayerDto"/>.</returns>
        Task<IReadOnlyCollection<DuplicatePlayerDto>> GetDuplicatePlayers();

        /// <summary>
        /// Gets every dirty player.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayers();

        /// <summary>
        /// Gets rankings for a specified stage and level a a specified date.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <param name="date">Date.</param>
        /// <returns>Collection of rankings.</returns>
        Task<IReadOnlyCollection<RankingDto>> GetStageLevelDateRankings(Stage stage, Level level, DateTime date);
    }
}
