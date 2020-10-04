﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain;

namespace TheEliteExplorerInfrastructure
{
    /// <summary>
    /// The-elite website parser interface.
    /// </summary>
    public interface ITheEliteWebSiteParser
    {
        /// <summary>
        /// Extracts every time for a given period from the website "the-elite".
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="year">The year to scan.</param>
        /// <param name="month">The month to scan.</param>
        /// <param name="minimalDateToScan">
        /// Optionnal date where to stop scan;
        /// if <c>Null</c>, the full page will be scanned.
        /// </param>
        /// <returns>A tuple with the list of <see cref="EntryRequest"/> and list of error logs.</returns>
        Task<(IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>)> ExtractTimeEntriesAsync(Game game, int year, int month, DateTime? minimalDateToScan);

        /// <summary>
        /// Extracts every time for a given stage from the website "the-elite".
        /// </summary>
        /// <param name="stageId">Stage identifier.</param>
        /// <returns>A tuple with the list of <see cref="EntryRequest"/> and list of error logs.</returns>
        Task<(IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>)> ExtractStageAllTimeEntriesAsync(long stageId);
    }
}