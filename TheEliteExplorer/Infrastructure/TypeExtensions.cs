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

        internal static int Count<T>() where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The targeted type is not an enum.");
            }

            return Enum.GetValues(typeof(T)).Length;
        }

        internal static DateTime? ToDateTime(this string dateTime)
        {
            return !string.IsNullOrWhiteSpace(dateTime) && DateTime.TryParse(dateTime, out DateTime dt) ?
                dt : default(DateTime?);
        }

        internal static IEnumerable<(int, int)> LoopMonthAndYear(this DateTime startDate, DateTime endDate)
        {
            for (int year = startDate.Year; year <= endDate.Year; year++)
            {
                int month = startDate.Month;
                do
                {
                    yield return (month, year);
                    month = month == 12 ? 1 : month + 1;
                }
                while (year != endDate.Year && month != endDate.Month);
            }
        }

    }
}
