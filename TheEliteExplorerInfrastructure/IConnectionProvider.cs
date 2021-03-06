﻿using System.Data;

namespace TheEliteExplorerInfrastructure
{
    /// <summary>
    /// Connection provider interface.
    /// </summary>
    public interface IConnectionProvider
    {
        /// <summary>
        /// Gets the connection to "TheElite" database.
        /// </summary>
        IDbConnection TheEliteConnection { get; }
    }
}
