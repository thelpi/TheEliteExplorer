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
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, long levelId, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Insert a time entry, or retrieves it if the tuple [playerId / levelId / stageId / time / systemId] already exists.
        /// </summary>
        /// <param name="requestEntry">Entry to insert.</param>
        /// <returns>Time entry identifier.</returns>
        Task<long> InsertOrRetrieveTimeEntryAsync(EntryDto requestEntry);

        /// <summary>
        /// Inserts a player, or retrieves him if <see cref="PlayerDto.UrlName"/> already exists.
        /// </summary>
        /// <remarks>If <see cref="PlayerDto.JoinDate"/> is specified, it will be rounded without the time part.</remarks>
        /// <param name="dto">The player DTO.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertOrRetrievePlayerAsync(PlayerDto dto);

        /// <summary>
        /// Inserts a player, or retrieves him if <paramref name="urlName"/> already exists.
        /// </summary>
        /// <remarks>If <paramref name="joinDate"/> is specified, it will be rounded without the time part.</remarks>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="joinDate">Date of joining the elite.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName, DateTime? joinDate);

        /// <summary>
        /// Gets the most recent entry date.
        /// </summary>
        /// <returns>Most recent entry date</returns>
        Task<DateTime> GetLatestEntryDateAsync();

        /// <summary>
        /// Gets time entries for every stage and level of the specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>Collection of <see cref="EntryDto"/>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesForEachStageAndLevelAsync(TheEliteExplorerDomain.Game game);

        /// <summary>
        /// Inserts a ranking into the database.
        /// </summary>
        /// <param name="ranking">Ranking data.</param>
        /// <returns>Nothing.</returns>
        Task InsertRankingAsync(RankingDto ranking);

        /// <summary>
        /// Gets the date of the latest ranking.
        /// </summary>
        /// <param name="gameId">Game identifier.</param>
        /// <returns>Date of the latest ranking; <c>Null</c> if no ranking.</returns>
        Task<DateTime?> GetLatestRankingDateAsync(long gameId);

        /// <summary>
        /// Gets duplicate players.
        /// </summary>
        /// <returns>A collection of <see cref="DuplicatePlayerDto"/>.</returns>
        Task<IReadOnlyCollection<DuplicatePlayerDto>> GetDuplicatePlayersAsync();

        /// <summary>
        /// Deletes a player by its identifier.
        /// </summary>
        /// <param name="id">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task DeletePlayerAsync(long id);

        /// <summary>
        /// Updates the player entries.
        /// </summary>
        /// <param name="currentPlayerId">Current player identifier.</param>
        /// <param name="newPlayerId">New player identifier.</param>
        /// <returns>Nothing.</returns>
        Task UpdatePlayerEntriesAsync(long currentPlayerId, long newPlayerId);

        /// <summary>
        /// Updates player information.
        /// </summary>
        /// <param name="player">Player information.</param>
        /// <returns>Nothing.</returns>
        Task UpdatePlayerInformationAsync(PlayerDto player);

        /// <summary>
        /// Gets every dirty player.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync();
    }
}
