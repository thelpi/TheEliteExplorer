using System;
using System.Data;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// Connection provider interface.
    /// </summary>
    public interface IConnectionProvider
    {
        /// <summary>
        /// Gets a SQL connection by its name.
        /// </summary>
        /// <param name="name">Connection name.</param>
        /// <returns>SQL connection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>Null</c>, empty or white spaces only.</exception>
        IDbConnection GetConnection(string name);
    }
}
