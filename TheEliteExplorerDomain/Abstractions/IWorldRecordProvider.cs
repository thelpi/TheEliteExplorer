using System.Threading.Tasks;
using TheEliteExplorerDomain.Enums;

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
    }
}
