namespace TheEliteExplorerDomain.Enums
{
    /// <summary>
    /// Possible rules for longest standing world records.
    /// </summary>
    public enum StandingType
    {
        /// <summary>
        /// Longest standing untied.
        /// </summary>
        Untied,
        /// <summary>
        /// Same as <see cref="Untied"/> with merge when "super/ultra-untied".
        /// </summary>
        UntiedExceptSelf,
        /// <summary>
        /// Longest standing unslayed; includes tieds.
        /// </summary>
        Unslayed,
        /// <summary>
        /// Same as <see cref="Unslayed"/> with merge for self-slay.
        /// </summary>
        UnslayedExceptSelf,
        /// <summary>
        /// Longest standing without being tied or slayed.
        /// </summary>
        BetweenTwoTimes,
        /// <summary>
        /// Same as <see cref="Unslayed"/> without tieds.
        /// </summary>
        FirstUnslayed
    }
}
