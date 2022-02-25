namespace TheEliteExplorerDomain.Enums
{
    /// <summary>
    /// Possible rules for longest standind world records.
    /// </summary>
    public enum StandingType
    {
        /// <summary>
        /// Longest standing untied.
        /// </summary>
        Untied,
        /// <summary>
        /// Longest standing untied, excluding slay by the same player while being untied.
        /// </summary>
        UntiedExceptSelf,
        /// <summary>
        /// Longest standing unslayed.
        /// </summary>
        Unslayed,
        /// <summary>
        /// Longest standing unslayed, excluding slay by the same player.
        /// </summary>
        UnslayedExceptSelf,
        /// <summary>
        /// Longest standing without being tied or slayed.
        /// </summary>
        BetweenTwoTimes
    }
}
