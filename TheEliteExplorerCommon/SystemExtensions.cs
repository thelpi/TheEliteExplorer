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
        /// Loops between a date and now with the step type and a step value of <c>1</c>.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="stepType">Step type.</param>
        /// <returns>Collection of dates.</returns>
        public static IEnumerable<DateTime> LoopBetweenDates(this DateTime startDate, DateStep stepType)
        {
            return LoopBetweenDates(startDate, ServiceProviderAccessor.ClockProvider.Now, stepType, 1);
        }

        /// <summary>
        /// Loops between two dates with the step type and a step value of <c>1</c>.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <param name="stepType">Step type.</param>
        /// <returns>Collection of dates.</returns>
        public static IEnumerable<DateTime> LoopBetweenDates(this DateTime startDate, DateTime endDate, DateStep stepType)
        {
            return LoopBetweenDates(startDate, endDate, stepType, 1);
        }

        /// <summary>
        /// Loops between two dates with the specified step.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <param name="stepType">Step type.</param>
        /// <param name="stepValue">Increment value between steps.</param>
        /// <returns>Collection of dates.</returns>
        public static IEnumerable<DateTime> LoopBetweenDates(this DateTime startDate, DateTime endDate, DateStep stepType, int stepValue)
        {
            for (DateTime date = startDate; date <= endDate; date = _dateStepDelegates[stepType](date, stepValue))
            {
                yield return date;
            }
        }

        private static readonly IReadOnlyDictionary<DateStep, Func<DateTime, int, DateTime>> _dateStepDelegates =
            new Dictionary<DateStep, Func<DateTime, int, DateTime>>
            {
                { DateStep.Second, (d, i) => d.AddSeconds(i) },
                { DateStep.Minute, (d, i) => d.AddMinutes(i) },
                { DateStep.Hour, (d, i) => d.AddHours(i) },
                { DateStep.Day, (d, i) => d.AddDays(i) },
                { DateStep.Month, (d, i) => d.AddMonths(i) },
                { DateStep.Year, (d, i) => d.AddYears(i) }
            };

        /// <summary>
        /// Computes the standard deviation of a list of integers.
        /// </summary>
        /// <param name="values">List of integers.</param>
        /// <returns>The standard deviation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>Null</c>.</exception>
        public static double ComputeStandardDeviation(this IEnumerable<int> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return values.Select(v => (double)v).ComputeStandardDeviation();
        }

        /// <summary>
        /// Computes the standard deviation of a list of floating numbers.
        /// </summary>
        /// <param name="values">List of floating numbers.</param>
        /// <returns>The standard deviation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>Null</c>.</exception>
        public static double ComputeStandardDeviation(this IEnumerable<double> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!values.Any())
            {
                return 0;
            }

            double sum = values.Sum(d => Math.Pow(d - values.Average(), 2));

            return Math.Sqrt((sum) / (values.Count() - 1));
        }

        /// <summary>
        /// Wraps this object instance into an <see cref="IEnumerable{T}"/> consisting of a single item.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="item">The instance that will be wrapped.</param>
        /// <returns> An <see cref="IEnumerable{T}"/> consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}
