using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// Write operations in repository (interface).
    /// </summary>
    public interface IWriteRepository
    {
        /// <summary>
        /// Updates the date of an entry.
        /// </summary>
        /// <param name="entryId">Entry identifier.</param>
        /// <param name="date">Date of the entry.</param>
        /// <returns>Nothing</returns>
        Task UpdateEntryDate(long entryId, DateTime date);

        /// <summary>
        /// Insert a time entry.
        /// </summary>
        /// <param name="requestEntry">Entry to insert.</param>
        /// <param name="game">Game.</param>
        /// <returns>Time entry identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="requestEntry"/> is <c>Null</c>.</exception>
        Task<long> InsertTimeEntry(EntryDto requestEntry, Game game);

        /// <summary>
        /// Inserts a player; player will be flagged dirty.
        /// </summary>
        /// <remarks>If <paramref name="joinDate"/> is specified, it will be rounded without the time part.</remarks>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="joinDate">Date of joining the elite.</param>
        /// <param name="defaultHexColor">Default hexadecimal color.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertPlayer(string urlName, DateTime? joinDate, string defaultHexColor);

        /// <summary>
        /// Inserts a ranking into the database.
        /// </summary>
        /// <param name="ranking">Ranking data.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ranking"/> is <c>Null</c>.</exception>
        Task InsertRanking(RankingDto ranking);

        /// <summary>
        /// Inserts in bulk a collection of <see cref="RankingDto"/>.
        /// </summary>
        /// <param name="rankings">Collection of <see cref="RankingDto"/>.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="rankings"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="rankings"/> is empty.</exception>
        Task BulkInsertRankings(IReadOnlyCollection<RankingDto> rankings);

        /// <summary>
        /// Deletes a player by its identifier.
        /// </summary>
        /// <param name="id">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task DeletePlayer(long id);

        /// <summary>
        /// Updates the player entries.
        /// </summary>
        /// <param name="currentPlayerId">Current player identifier.</param>
        /// <param name="newPlayerId">New player identifier.</param>
        /// <returns>Nothing.</returns>
        Task UpdatePlayerEntries(long currentPlayerId, long newPlayerId);

        /// <summary>
        /// Updates player information.
        /// </summary>
        /// <param name="player">Player information.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player"/> is <c>Null</c>.</exception>
        Task UpdatePlayerInformation(PlayerDto player);

        /// <summary>
        /// Deletes every entry for a player for a stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task DeletePlayerStageEntries(Stage stage, long playerId);

        /// <summary>
        /// Deletes ranking history for a specific stage and level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <returns>Nothing.</returns>
        Task DeleteStageLevelRankingHistory(Stage stage, Level level);

        /// <summary>
        /// Inserts a WR for a stage and level.
        /// </summary>
        /// <param name="dto">WR DTO.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dto"/> is <c>Null</c>.</exception>
        Task InsertWr(WrDto dto);

        /// <summary>
        /// Deletes every WR for a stage and level.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <returns>Nothing.</returns>
        Task DeleteStageLevelWr(Stage stage, Level level);

        /// <summary>
        /// Updates a plyer to dirty.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task UpdateDirtyPlayer(long playerId);
    }
}
