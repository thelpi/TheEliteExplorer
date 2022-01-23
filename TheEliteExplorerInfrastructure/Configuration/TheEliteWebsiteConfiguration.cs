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
        /// Use <see cref="string.Format(string, object, object)"/>: {0} for year and {1} for month
        /// </remarks>
        public string HistoryPage { get; set; }
        /// <summary>
        /// Ajax URL key.
        /// </summary>
        public string AjaxKey { get; set; }
        /// <summary>
        /// Attemps of getting a page content.
        /// </summary>
        public int PageAttemps { get; set; }
    }
}
