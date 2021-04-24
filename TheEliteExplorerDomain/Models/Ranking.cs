using System;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents the default behavior for model items that can be ranked.
    /// </summary>
    public abstract class Ranking
    {
        /// <summary>
        /// Rank.
        /// </summary>
        public int Rank { get; private set; }
        /// <summary>
        /// Sub rank.
        /// </summary>
        public int SubRank { get; private set; }

        internal virtual void SetRank<T>(
            Ranking previousRankingEntry,
            Func<Ranking, T> getComparedValue)
            where T : IEquatable<T>
        {
            Rank = 1;
            if (previousRankingEntry != null)
            {
                SubRank = previousRankingEntry.SubRank + 1;
                Rank = previousRankingEntry.Rank;
                if (!getComparedValue(previousRankingEntry).Equals(getComparedValue(this)))
                {
                    Rank += SubRank;
                    SubRank = 0;
                }
            }
        }
    }
}
