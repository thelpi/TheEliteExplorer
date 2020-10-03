using System;
using System.Collections.Generic;
using System.Linq;

namespace TheEliteExplorerCommon
{
    /// <summary>
    /// System extension methods.
    /// </summary>
    public static class SystemExtensions
    {
        /// <summary>
        /// Enumerates values of an enumeration.
        /// </summary>
        /// <typeparam name="T">Enumeration type.</typeparam>
        /// <returns>Values.</returns>
        /// <exception cref="InvalidOperationException">The targeted type is not an enum.</exception>
        public static IEnumerable<T> Enumerate<T>() where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The targeted type is not an enum.");
            }

            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Counts the number of values of an enumeration.
        /// </summary>
        /// <typeparam name="T">Enumeration type.</typeparam>
        /// <returns>Values count.</returns>
        /// <exception cref="InvalidOperationException">The targeted type is not an enum.</exception>
        public static int Count<T>() where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The targeted type is not an enum.");
            }

            return Enum.GetValues(typeof(T)).Length;
        }

        /// <summary>
        /// Tries to parse a string representing date (and optionnaly time) into a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The string date.</param>
        /// <returns>Instance of <see cref="DateTime"/> or <c>Null</c>.</returns>
        public static DateTime? ToDateTime(this string dateTime)
        {
            return !string.IsNullOrWhiteSpace(dateTime) && DateTime.TryParse(dateTime, out DateTime dt) ?
                dt : default(DateTime?);
        }

        /// <summary>
        /// Enumerates every month of every year between two dates.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>A list of (month / year) tuples, from the beginning to the end.</returns>
        public static IEnumerable<(int, int)> LoopMonthAndYear(this DateTime startDate, DateTime endDate)
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
