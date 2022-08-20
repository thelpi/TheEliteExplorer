using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

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
                return new MySqlConnection(_configuration.GetConnectionString(_theEliteConfigKey));
            }
        }

        public ConnectionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}
