using System;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a ranking entry for a game.
    /// </summary>
    public class GameRank
    {
        /// <summary>
        /// Time simulated, in seconds, when no entry found.
        /// </summary>
        public const long UnsetTime = 1200;

        /// <summary>
        /// Rank.
        /// </summary>
        public int Rank { get; private set; }

        /// <summary>
        /// Player details.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Points.
        /// </summary>
        public int Points { get; private set; }

        /// <summary>
        /// Time (in seconds).
        /// </summary>
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// Count of stage/level tuples with time.
        /// </summary>
        public int Times { get; private set; }

        internal GameRank(Player p)
        {
            Player = p;
            Time = TimeSpan.Zero;
        }

        internal void AddEntry(Dtos.RankingBaseDto ranking, int points)
        {
            Points += points;
            Time = Time.Add(new TimeSpan(0, 0, (int)ranking.Time));
            Times++;
        }

        internal GameRank WithRank(int i, int rankOfPreviousItem, bool isCurrentItemEqualPreviousItem)
        {
            Rank = isCurrentItemEqualPreviousItem
                ? rankOfPreviousItem
                : i + 1;
            return this;
        }

        internal void FillMissingTimes(int expectedTimesCount)
        {
            var timeToAdd = UnsetTime * (expectedTimesCount - Times);
            Time = Time.Add(new TimeSpan(0, 0, (int)timeToAdd));
        }
    }
}
