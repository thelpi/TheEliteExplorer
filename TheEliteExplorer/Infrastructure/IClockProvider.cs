using System;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// Clock provider interface.
    /// </summary>
    public interface IClockProvider
    {
        /// <summary>
        /// Represents the current date of the clocK.
        /// </summary>
        DateTime Now { get; }
    }
}
