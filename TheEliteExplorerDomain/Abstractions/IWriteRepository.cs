using System;
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
        Task UpdateEntryDateAsync(long entryId, DateTime date);

        /// <summary>
        /// Insert a time entry.
        /// </summary>
        /// <param name="requestEntry">Entry to insert.</param>
        /// <param name="game">Game.</param>
        /// <returns>Time entry identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="requestEntry"/> is <c>Null</c>.</exception>
        Task<long> InsertTimeEntryAsync(EntryDto requestEntry, Game game);

        /// <summary>
        /// Inserts a player; player will be flagged dirty.
        /// </summary>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="defaultHexColor">Default hexadecimal color.</param>
        /// <returns>Player identifier.</returns>
        Task<long> InsertPlayerAsync(string urlName, string defaultHexColor);

        /// <summary>
        /// Deletes every entry for a player for a stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task DeletePlayerStageEntriesAsync(Stage stage, long playerId);

        /// <summary>
        /// Updates a plyer to dirty.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Nothing.</returns>
        Task UpdateDirtyPlayerAsync(long playerId);

        /// <summary>
        /// Cleans a dirty player.
        /// </summary>
        /// <param name="player">Player information.</param>
        /// <returns>Nothing</returns>
        Task CleanPlayerAsync(PlayerDto player);

        /// <summary>
        /// Inserts a ranking entry.
        /// </summary>
        /// <param name="ranking">Ranking information.</param>
        /// <returns>Nothing.</returns>
        Task InsertRankingEntryAsync(RankingDto ranking);
    }
}
