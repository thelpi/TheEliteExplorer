﻿using System;

namespace TheEliteExplorer.Infrastructure
{
    /// <summary>
    /// Clock provider default implementation.
    /// </summary>
    /// <seealso cref="IClockProvider"/>
    public class ClockProvider : IClockProvider
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;
    }
}
