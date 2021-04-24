using System;
using System.Collections.Generic;

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

        internal virtual void SetRank<T, TValue>(
            T previousRankingEntry,
            Func<T, TValue> getComparedValue)
            where T : Ranking
            where TValue : IEquatable<TValue>
        {
            Rank = 1;
            if (previousRankingEntry != null)
            {
                SubRank = previousRankingEntry.SubRank + 1;
                Rank = previousRankingEntry.Rank;
                if (!getComparedValue(previousRankingEntry).Equals(getComparedValue((T)this)))
                {
                    Rank += SubRank;
                    SubRank = 0;
                }
            }
        }
    }

    internal static class RankingExtensions
    {
        internal static List<T> WithRanks<T, TValue>(
            this List<T> rankings,
            Func<T, TValue> getComparedValue)
            where T : Ranking
            where TValue : IEquatable<TValue>
        {
            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].SetRank(
                    i == 0 ? null : rankings[i - 1],
                    getComparedValue);
            }
            return rankings;
        }
    }
}
