using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TheEliteExplorer
{
    /// <summary>
    /// Represents a read-only paginated collection of items.
    /// </summary>
    /// <typeparam name="T">Targeted type.</typeparam>
    /// <seealso cref="IReadOnlyCollection{T}"/>
    public class PaginatedCollection<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Max count allowed for a paginated collection.
        /// </summary>
        public static readonly int MaxCount = 1000;

        private readonly List<T> _sourceList;

        /// <inheritdoc />
        public int Count { get { return _sourceList.Count; } }

        /// <summary>
        /// Total items count.
        /// </summary>
        public int TotalItemsCount { get; }

        /// <summary>
        /// Static constructor.
        /// </summary>
        /// <param name="source">Source list.</param>
        /// <param name="page">Page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <returns>Instance of <see cref="PaginatedCollection{T}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>Null</c>.</exception>
        public static PaginatedCollection<T> CreateInstance(IReadOnlyCollection<T> source,
            int page, int count)
        {
            return new PaginatedCollection<T>(
                PaginateSourceList(source, page, count).ToList(),
                source.Count
            );
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        /// <typeparam name="TSource">Type of items in <paramref name="source"/>.</typeparam>
        /// <param name="source">Source list.</param>
        /// <param name="transformationCallback">Transformation callback from <typeparamref name="TSource"/> to <typeparamref name="T"/>.</param>
        /// <param name="page">Page index (starts at <c>1</c>).</param>
        /// <param name="count">Items count by page.</param>
        /// <returns>Instance of <see cref="PaginatedCollection{T}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="transformationCallback"/> is <c>Null</c>.</exception>
        public static PaginatedCollection<T> CreateInstance<TSource>(IReadOnlyCollection<TSource> source,
            Func<TSource, T> transformationCallback, int page, int count)
        {
            if (transformationCallback == null)
            {
                throw new ArgumentNullException(nameof(transformationCallback));
            }

            return new PaginatedCollection<T>(
                PaginateSourceList(source, page, count).Select(item => transformationCallback(item)).ToList(),
                source.Count
            );
        }

        private static IEnumerable<TSource> PaginateSourceList<TSource>(IReadOnlyCollection<TSource> source, int page, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            page = EnsurePage(page);
            count = EnsureCount(count);
            
            return source.Skip(count * (page - 1)).Take(count);
        }

        private PaginatedCollection(List<T> sourceList, int totalItemsCount)
        {
            _sourceList = sourceList;
            TotalItemsCount = totalItemsCount;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _sourceList.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _sourceList.GetEnumerator();
        }

        internal static int EnsurePage(int page)
        {
            return Math.Max(page, 1);
        }

        internal static int EnsureCount(int count)
        {
            return Math.Min(Math.Max(count, 1), MaxCount);
        }
    }
}
