﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
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
        /// <param name="level">Level.</param>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (exclusive).</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long stageId, Level level, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Gets every entry for a specified game.
        /// </summary>
        /// <param name="gameId">Game identifier.</param>
        /// <returns>Collection of <see cref="EntryDto"/>; can't be <c>Null</c>.</returns>
        Task<IReadOnlyCollection<EntryDto>> GetEntriesAsync(long gameId);

        /// <summary>
        /// Insert a time entry, or retrieves it if the tuple [playerId / levelId / stageId / time / systemId] already exists.
        /// </summary>
        /// <param name="requestEntry">Entry to insert.</param>
        /// <param name="gameId">Game identifier.</param>
        /// <returns>Time entry identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="requestEntry"/> is <c>Null</c>.</exception>
        Task<long> InsertOrRetrieveTimeEntryAsync(EntryDto requestEntry, long gameId);

        /// <summary>
        /// Inserts a player, or retrieves him if <see cref="PlayerDto.UrlName"/> already exists.
        /// </summary>
        /// <remarks>If <see cref="PlayerDto.JoinDate"/> is specified, it will be rounded without the time part.</remarks>
        /// <param name="dto">The player DTO.</param>
        /// <returns>Player identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dto"/> is <c>Null</c>.</exception>
        Task<long> InsertOrRetrievePlayerAsync(PlayerDto dto);

        /// <summary>
        /// Inserts a player, or retrieves him if <paramref name="urlName"/> already exists.
        /// </summary>
        /// <remarks>If <paramref name="joinDate"/> is specified, it will be rounded without the time part.</remarks>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="joinDate">Date of joining the elite.</param>
        /// <param name="defaultHexColor">Default hexadecimal color.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertOrRetrievePlayerDirtyAsync(string urlName, DateTime? joinDate, string defaultHexColor);

        /// <summary>
        /// Gets the most recent entry date.
        /// </summary>
        /// <returns>Most recent entry date</returns>
        Task<DateTime?> GetLatestEntryDateAsync();

        /// <summary>
        /// Inserts a ranking into the database.
        /// </summary>
        /// <param name="ranking">Ranking data.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ranking"/> is <c>Null</c>.</exception>
        Task InsertRankingAsync(RankingDto ranking);

        /// <summary>
        /// Inserts in bulk a collection of <see cref="RankingDto"/>.
        /// </summary>
        /// <param name="rankings">Collection of <see cref="RankingDto"/>.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="rankings"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="rankings"/> is empty.</exception>
        Task BulkInsertRankingsAsync(IReadOnlyCollection<RankingDto> rankings);

        /// <summary>
        /// Gets the date of the latest ranking.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Date of the latest ranking; <c>Null</c> if no ranking.</returns>
        Task<DateTime?> GetLatestRankingDateAsync(Game game);

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
        /// <exception cref="ArgumentNullException"><paramref name="player"/> is <c>Null</c>.</exception>
        Task UpdatePlayerInformationAsync(PlayerDto player);

        /// <summary>
        /// Gets every dirty player.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerDto"/>.</returns>
        Task<IReadOnlyCollection<PlayerDto>> GetDirtyPlayersAsync();

        /// <summary>
        /// Deletes every entry for a player for a stage.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task DeletePlayerStageEntries(long stageId, long playerId);

        /// <summary>
        /// Deletes ranking history for a specific stage and level.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <param name="level">Level.</param>
        /// <returns>Nothing.</returns>
        Task DeleteStageLevelRankingHistory(long stageId, Level level);

        /// <summary>
        /// Gets rankings for a specified stage and level a a specified date.
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <param name="level">Level.</param>
        /// <param name="date">Date.</param>
        /// <returns>Collection of rankings.</returns>
        Task<IReadOnlyCollection<RankingDto>> GetStageLevelDateRankings(long stageId, Level level, DateTime date);
    }
}
