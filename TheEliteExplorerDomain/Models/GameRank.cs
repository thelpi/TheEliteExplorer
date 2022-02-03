using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a ranking entry for a game.
    /// </summary>
    public class GameRank
    {
        private const long UnsetTime = 1200;

        private int _timesCount;
        private readonly Dictionary<Level, int> _levelTimesCount;
        private readonly Dictionary<Level, int> _levelPoints;
        private readonly Dictionary<Level, TimeSpan> _levelTime;

        /// <summary>
        /// Time rank.
        /// </summary>
        public int TimeRank { get; internal set; }

        /// <summary>
        /// Points rank.
        /// </summary>
        public int PointsRank { get; internal set; }

        /// <summary>
        /// Player details.
        /// </summary>
        public Dtos.PlayerDto Player { get; }

        /// <summary>
        /// Points.
        /// </summary>
        public int Points { get; private set; }

        /// <summary>
        /// Time (in seconds).
        /// </summary>
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// Points by level
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelPoints { get { return _levelPoints; } }

        /// <summary>
        /// Time by level
        /// </summary>
        public IReadOnlyDictionary<Level, TimeSpan> LevelTime { get { return _levelTime; } }

        internal GameRank(Dtos.PlayerDto p)
        {
            Player = p;
            Time = TimeSpan.Zero;
            _levelPoints = SystemExtensions.Enumerate<Level>().ToDictionary(_ => _, _ => 0);
            _levelTime = SystemExtensions.Enumerate<Level>().ToDictionary(_ => _, _ => TimeSpan.Zero);
            _levelTimesCount = SystemExtensions.Enumerate<Level>().ToDictionary(_ => _, _ => 0);
        }

        internal void AddEntry(Dtos.RankingBaseDto ranking, int points, Level level)
        {
            var localTime = new TimeSpan(0, 0, (int)ranking.Time);

            Points += points;
            Time = Time.Add(localTime);
            _timesCount++;

            _levelPoints[level] += points;
            _levelTime[level] += _levelTime[level].Add(localTime);
            _levelTimesCount[level]++;
        }

        internal void FillMissingTimes(int expectedTimesCount)
        {
            var timeToAdd = UnsetTime * (expectedTimesCount - _timesCount);
            Time = Time.Add(new TimeSpan(0, 0, (int)timeToAdd));

            foreach (var key in _levelTimesCount.Keys)
            {
                timeToAdd = UnsetTime * (expectedTimesCount / 3 - _levelTimesCount[key]);
                _levelTime[key] = _levelTime[key].Add(new TimeSpan(0, 0, (int)timeToAdd));
            }
        }
    }
}
