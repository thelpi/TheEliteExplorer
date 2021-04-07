using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Configuration
{
    /// <summary>
    /// Ranking configuration.
    /// </summary>
    public class RankingConfiguration
    {
        /// <summary>
        /// Rule to apply for entry without date.
        /// </summary>
        public NoDateEntryRankingRule NoDateEntryRankingRule { get; set; }
    }
}
