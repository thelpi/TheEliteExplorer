using System;
using System.Collections.Generic;
using System.Linq;

namespace TheEliteExplorer.Infrastructure
{
    internal static class TypeExtensions
    {
        internal static IEnumerable<T> Enumerate<T>() where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The targeted type is not an enum.");
            }

            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        internal static DateTime? ToDateTime(this string dateTime)
        {
            return !string.IsNullOrWhiteSpace(dateTime) && DateTime.TryParse(dateTime, out DateTime dt) ?
                dt : default(DateTime?);
        }
    }
}
