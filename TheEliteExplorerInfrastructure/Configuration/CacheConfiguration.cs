namespace TheEliteExplorerInfrastructure.Configuration
{
    /// <summary>
    /// Cache configuration.
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// Indicates if the cache is enabled.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Minutes before cache expiration.
        /// </summary>
        public int MinutesBeforeExpiration { get; set; }
    }
}
