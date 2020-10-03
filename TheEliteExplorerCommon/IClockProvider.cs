using System;

namespace TheEliteExplorerCommon
{
    /// <summary>
    /// Clock provider interface.
    /// </summary>
    public interface IClockProvider
    {
        /// <summary>
        /// Represents the current datetime of the clocK.
        /// </summary>
        DateTime Now { get; }
    }
}
