using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Abstractions
{
    /// <summary>
    /// World records provider interface.
    /// </summary>
    public interface IWorldRecordProvider
    {
        /// <summary>
        /// Generates world records for a whole game; clears previous world records already registered.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Nothing.</returns>
        Task GenerateWorldRecords(Game game);

        /// <summary>
        /// Gets sweeps.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied">Untied y/n.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>Collection of sweeps</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startDate"/> is greater than <paramref name="endDate"/>.</exception>
        Task<IReadOnlyCollection<StageSweep>> GetSweeps(
            Game game,
            bool untied,
            DateTime? startDate,
            DateTime? endDate);

        /// <summary>
        /// Gets longest standing world records.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="untied"><c>True</c> to only get untied world records.</param>
        /// <param name="stillStanding"><c>True</c> to get only world records currently standing.</param>
        /// <returns>Collection of longest standing world records; sorted by the most days.</returns>
        Task<IReadOnlyCollection<StageWrStanding>> GetLongestStandingWrs(
            Game game,
            bool untied,
            bool stillStanding);
    }
}
