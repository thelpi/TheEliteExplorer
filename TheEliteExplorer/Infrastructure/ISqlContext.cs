using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorer.Infrastructure.Dtos;

namespace TheEliteExplorer.Infrastructure
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
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, long levelId, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Insert a time entry, or retrieves it if the tuple [playerId / levelId / stageId / time / systemId] already exists.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="levelId">Level identifier.</param>
        /// <param name="stageId">Stage identifier.</param>
        /// <param name="date">Date.</param>
        /// <param name="time">Time.</param>
        /// <param name="systemId">System (engine) identifier.</param>
        /// <returns>Time entry identifier.</returns>
        Task<long> InsertOrRetrieveTimeEntryAsync(long playerId, long levelId, long stageId, DateTime? date, long? time, long? systemId);

        /// <summary>
        /// Inserts a player, or retrieves him if <paramref name="urlName"/> already exists.
        /// </summary>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="realName">Player real name.</param>
        /// <param name="surname">Player surname.</param>
        /// <param name="color">Player color.</param>
        /// <param name="controlStyle">Player control style.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertOrRetrievePlayerAsync(string urlName, string realName, string surname, string color, string controlStyle);

        /// <summary>
        /// Inserts a player, or retrieves him if <paramref name="urlName"/> already exists.
        /// </summary>
        /// <param name="urlName">Player URL name.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName);

        /// <summary>
        /// Gets the most recent entry date.
        /// </summary>
        /// <returns>Most recent entry date</returns>
        Task<DateTime> GetLatestEntryDateAsync();
    }
}
