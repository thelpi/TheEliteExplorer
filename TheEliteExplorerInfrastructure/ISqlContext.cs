using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerInfrastructure
{
    /// <summary>
    /// SQL context interface.
    /// </summary>
    public interface ISqlContext
    {
        /// <summary>
        /// Gets every players from the database.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync();

        /// <summary>
        /// Gets every entries for a specified stage and level, between two dates.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <param name="levelId">Level identifier.</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <param name="includeUnknownDate"><c>True</c> to include entries without date, regardless of <paramref name="startDate"/> and <paramref name="endDate"/>.</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, long levelId, DateTime? startDate, DateTime? endDate, bool includeUnknownDate);

        /// <summary>
        /// Insert a time entry, or retrieves it if the tuple [playerId / levelId / stageId / time / systemId] already exists.
        /// </summary>
        /// <param name="requestEntry">Entry to insert.</param>
        /// <returns>Time entry identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="requestEntry"/> is <c>Null</c>.</exception>
        Task<long> InsertOrRetrieveTimeEntryAsync(EntryDto requestEntry);

        /// <summary>
        /// Inserts a player, or retrieves him if <see cref="PlayerDto.UrlName"/> already exists.
        /// </summary>
        /// <param name="dto">The player DTO.</param>
        /// <returns>Player identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dto"/> is <c>Null</c>.</exception>
        Task<long> InsertOrRetrievePlayerAsync(PlayerDto dto);

        /// <summary>
        /// Inserts a player, or retrieves him if <paramref name="urlName"/> already exists.
        /// </summary>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="joinDate">Date of joining the elite.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName, DateTime? joinDate);

        /// <summary>
        /// Gets the most recent entry date.
        /// </summary>
        /// <returns>Most recent entry date</returns>
        Task<DateTime> GetLatestEntryDateAsync();
    }
}
