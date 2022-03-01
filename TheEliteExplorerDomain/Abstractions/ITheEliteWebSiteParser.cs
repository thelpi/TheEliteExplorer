using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// The-elite website parser interface.
    /// </summary>
    public interface ITheEliteWebSiteParser
    {
        /// <summary>
        /// Extracts every time for a given period from the website "the-elite".
        /// </summary>
        /// <param name="year">The year to scan.</param>
        /// <param name="month">The month to scan.</param>
        /// <param name="withEngine">Gets the engine (from another page) or not.</param>
        /// <returns>List of <see cref="EntryWebDto"/>.</returns>
        Task<IReadOnlyCollection<EntryWebDto>> ExtractTimeEntriesAsync(int year, int month, bool withEngine);

        /// <summary>
        /// Extracts every time for a given stage from the website "the-elite".
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <returns>List of <see cref="EntryWebDto"/>.</returns>
        Task<IReadOnlyCollection<EntryWebDto>> ExtractStageAllTimeEntriesAsync(Stage stage);

        /// <summary>
        /// Gets information about a player.
        /// </summary>
        /// <param name="urlName">Player URL name.</param>
        /// <param name="defaultHexPlayer">Default hexadecimal player color.</param>
        /// <returns>Instance of <see cref="PlayerDto"/>; <c>Null</c> if not found.</returns>
        Task<PlayerDto> GetPlayerInformationAsync(string urlName, string defaultHexPlayer);

        /// <summary>
        /// Gets entries history for a single player.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="playerUrlName">Player URL name.</param>
        /// <returns>Collection of entries; <c>Null</c> if the player history is not accessible.</returns>
        Task<IReadOnlyCollection<EntryWebDto>> GetPlayerEntriesHistoryAsync(Game game, string playerUrlName);
    }
}
