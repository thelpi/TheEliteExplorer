using System;

namespace TheEliteExplorerCommon
{
    public interface IClockProvider
    {
        DateTime Now { get; }
        DateTime Tomorrow { get; }
    }
}
