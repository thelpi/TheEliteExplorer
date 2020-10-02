using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// Connection provider.
    /// </summary>
    /// <seealso cref="IConnectionProvider"/>
    public class ConnectionProvider : IConnectionProvider
    {
        private readonly IConfiguration _configuration;
        private const string _theEliteConfigKey = "TheElite";

        /// <inheritdoc />
        public IDbConnection TheEliteConnection
        {
            get
            {
                return new SqlConnection(_configuration.GetConnectionString(_theEliteConfigKey));
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Dictionary of named connectionstrings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>Null</c>.</exception>
        public ConnectionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}
