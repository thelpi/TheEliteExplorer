namespace TheEliteExplorerDomain.Enums
{
    /// <summary>
    /// Ranking computing rules available to apply when an entry has no date.
    /// </summary>
    public enum NoDateEntryRankingRule
    {
        /// <summary>
        /// The entry is ignored.
        /// </summary>
        Ignore,
        /// <summary>
        /// The lowest date is considered.
        /// </summary>
        Min,
        /// <summary>
        /// The greatest date is considered.
        /// </summary>
        Max,
        /// <summary>
        /// The average date is considered.
        /// </summary>
        Average,
        /// <summary>
        /// An arbitrary date based on player habits is considered.
        /// </summary>
        PlayerHabit
    }
}
