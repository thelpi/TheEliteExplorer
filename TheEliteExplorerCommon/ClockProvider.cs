using System;

namespace TheEliteExplorerCommon
{
    public class ClockProvider : IClockProvider
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;

        public DateTime Tomorrow => DateTime.Now.AddDays(1).Date;
    }
}
