﻿using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a ranking entry.
    /// </summary>
    public class RankingEntry : RankingEntryLight
    {
        private readonly Dictionary<Level, int> _levelPoints;
        private readonly Dictionary<Level, int> _levelUntiedRecordsCount;
        private readonly Dictionary<Level, int> _levelRecordsCount;
        private readonly Dictionary<Level, long> _levelCumuledTime;
        private readonly Dictionary<Stage, Dictionary<Level, (int, int, long?, System.DateTime?)>> _details;

        /// <summary>
        /// Points by <see cref="Level"/>.
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelPoints { get { return _levelPoints; } }
        /// <summary>
        /// Count of untied world records by <see cref="Level"/>.
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelUntiedRecordsCount { get { return _levelUntiedRecordsCount; } }
        /// <summary>
        /// Count of world records by <see cref="Level"/>.
        /// </summary>
        public IReadOnlyDictionary<Level, int> LevelRecordsCount { get { return _levelRecordsCount; } }
        /// <summary>
        /// Time cumuled on every stage, by level.
        /// </summary>
        public IReadOnlyDictionary<Level, long> LevelCumuledTime { get { return _levelCumuledTime; } }
        /// <summary>
        /// Detail of [ranking/points/time/date] for each level of each stage.
        /// </summary>
        public IReadOnlyDictionary<Stage, IReadOnlyDictionary<Level, (int, int, long?, System.DateTime?)>> Details
        {
            get
            {
                return _details.ToDictionary(d => d.Key, d => (IReadOnlyDictionary<Level, (int, int, long?, System.DateTime?)>)d.Value);
            }
        }

        internal RankingEntry(Game game, PlayerDto player) : base(game, player)
        {
            _details = new Dictionary<Stage, Dictionary<Level, (int, int, long?, System.DateTime?)>>();

            _levelPoints = ToLevelDictionary(0);
            _levelUntiedRecordsCount = ToLevelDictionary(0);
            _levelRecordsCount = ToLevelDictionary(0);

            _levelCumuledTime = ToLevelDictionary(UnsetTimeValueSeconds * Game.GetStages().Count);
        }

        internal override int AddStageAndLevelDatas(RankingDto ranking, bool untied)
        {
            var points = base.AddStageAndLevelDatas(ranking, untied);

            if (ranking.Rank == 1)
            {
                _levelRecordsCount[ranking.Level]++;
                if (untied)
                {
                    _levelUntiedRecordsCount[ranking.Level]++;
                }
            }
            
            _levelPoints[ranking.Level] += points;
            
            GetDetailsByLevel(ranking.Stage).Add(ranking.Level, (ranking.Rank, points, ranking.Time, ranking.EntryDate));

            if (ranking.Time < UnsetTimeValueSeconds)
            {
                _levelCumuledTime[ranking.Level] -= UnsetTimeValueSeconds - ranking.Time;
            }

            return points;
        }

        private Dictionary<Level, (int, int, long?, System.DateTime?)> GetDetailsByLevel(Stage stage)
        {
            Dictionary<Level, (int, int, long?, System.DateTime?)> detailsByLevel;
            if (!_details.ContainsKey(stage))
            {
                detailsByLevel = new Dictionary<Level, (int, int, long?, System.DateTime?)>();
                _details.Add(stage, detailsByLevel);
            }
            else
            {
                detailsByLevel = _details[stage];
            }

            return detailsByLevel;
        }

        private static Dictionary<Level, T> ToLevelDictionary<T>(T value)
        {
            return SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => value);
        }
    }
}
