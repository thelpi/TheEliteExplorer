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
    }
}
