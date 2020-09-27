using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEliteExplorer.Infrastructure.Dtos;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// SQL context.
    /// </summary>
    /// <seealso cref="ISqlContext"/>
    public class SqlContext : ISqlContext
    {
        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PlayerDto>> GetPlayers()
        {
            throw new NotImplementedException();
        }
    }
}
