using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TheEliteExplorerInfrastructure
{
    public class ConnectionProvider : IConnectionProvider
    {
        private readonly IConfiguration _configuration;
        private const string _theEliteConfigKey = "TheElite";

        public IDbConnection TheEliteConnection
        {
            get
            {
                return new SqlConnection(_configuration.GetConnectionString(_theEliteConfigKey));
            }
        }

        public ConnectionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}
