namespace TheEliteExplorerInfrastructure.Configuration
{
    /// <summary>
    /// Configuration of "The Elite" website.
    /// </summary>
    public class TheEliteWebsiteConfiguration
    {
        /// <summary>
        /// Base URL.
        /// </summary>
        public string BaseUri { get; set; }
        /// <summary>
        /// History page partial URL.
        /// </summary>
        /// <remarks>
        /// Use <see cref="System.String.Format(string, object, object)"/>: {0} for year and {1} for month
        /// </remarks>
        public string HistoryPage { get; set; }
    }
}
