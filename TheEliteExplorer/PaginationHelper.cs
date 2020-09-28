using System;
using System.Collections.Generic;

namespace TheEliteExplorer
{
    internal static class PaginationHelper
    {
        private const int _maxCount = 1000;

        internal static int EnsurePage(int page)
        {
            return Math.Max(page, 1);
        }

        internal static int EnsureCount(int count)
        {
            return Math.Min(Math.Max(count, 1), _maxCount);
        }
    }
}
