using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// Connection provider.
    /// </summary>
    /// <seealso cref="IConnectionProvider"/>
    public class ConnectionProvider : IConnectionProvider
    {
        private readonly IReadOnlyDictionary<string, string> _connectionStrings;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionStrings">Dictionary of named connectionstrings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionStrings"/> is <c>Null</c>.</exception>
        public ConnectionProvider(IReadOnlyDictionary<string, string> connectionStrings)
        {
            _connectionStrings = connectionStrings ?? throw new ArgumentNullException(nameof(connectionStrings));
        }

        /// <inheritdoc />
        public IDbConnection GetConnection(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_connectionStrings.ContainsKey(name))
            {
                return null;
            }

            return new SqlConnection(_connectionStrings[name]);
        }
    }
}
