using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a ranking entry.
    /// </summary>
    public class RankingEntry
    {
        /// <summary>
        /// When a time is unknown, the value used is <c>20</c> minutes.
        /// </summary>
        public static readonly long UnsetTimeValueSeconds = 20 * 60;

        private readonly Dictionary<Level, int> _levelPoints;
        private readonly Dictionary<Level, int> _levelUntiedRecordsCount;
        private readonly Dictionary<Level, int> _levelRecordsCount;
        private readonly Dictionary<Level, long> _levelCumuledTime;
        private readonly Dictionary<Stage, Dictionary<Level, (int, int, long?)>> _details;

        /// <summary>
        /// Game.
        /// </summary>
        public Game Game { get; }
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; }
        /// <summary>
        /// Player name (surname).
        /// </summary>
        public string PlayerName { get; }
        /// <summary>
        /// Points.
        /// </summary>
        public int Points { get; private set; }
        /// <summary>
        /// Time cumuled on every level/stage.
        /// </summary>
        public long CumuledTime { get; private set; }
        /// <summary>
        /// Count of untied world records.
        /// </summary>
        public int UntiedRecordsCount { get; private set; }
        /// <summary>
        /// Count of world records.
        /// </summary>
        public int RecordsCount { get; private set; }
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
        /// Detail of [ranking/points/time] for each level of each stage.
        /// </summary>
        public IReadOnlyDictionary<Stage, IReadOnlyDictionary<Level, (int, int, long?)>> Details
        {
            get
            {
                return _details.ToDictionary(d => d.Key, d => (IReadOnlyDictionary<Level, (int, int, long?)>)d.Value);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join('\t', new object[]
            {
                PlayerName,
                LevelPoints[Level.Easy],
                LevelPoints[Level.Medium],
                LevelPoints[Level.Hard],
                Points,
                string.Join("§", _details.ToList().SelectMany(kvp =>
                    kvp.Value.Select(k2 => string.Join("¤", new object[]
                    {
                        kvp.Key.Name,
                        k2.Key,
                        k2.Value.Item1,
                        k2.Value.Item2,
                        k2.Value.Item3
                    })).ToList()))
            });
        }

        internal RankingEntry(Game game, PlayerDto player)
        {
            Game = game;
            PlayerId = player.Id;
            PlayerName = player.RealName;
            _details = new Dictionary<Stage, Dictionary<Level, (int, int, long?)>>();

            Points = 0;
            UntiedRecordsCount = 0;
            RecordsCount = 0;

            _levelPoints = ToLevelDictionary(0);
            _levelUntiedRecordsCount = ToLevelDictionary(0);
            _levelRecordsCount = ToLevelDictionary(0);

            long allStagesMaxTime = UnsetTimeValueSeconds * Stage.Get(Game).Count;

            CumuledTime = allStagesMaxTime * SystemExtensions.Count<Level>();

            _levelCumuledTime = ToLevelDictionary(allStagesMaxTime);
        }

        internal void AddStageAndLevelDatas(RankingDto ranking, bool untied)
        {
            var stage = Stage.Get(Game).FirstOrDefault(s => s.Id == ranking.StageId);

            Level level = (Level)ranking.LevelId;

            int points = (100 - ranking.Rank) - 2;
            if (points < 0)
            {
                points = 0;
            }
            if (ranking.Rank == 1)
            {
                points = 100;
                RecordsCount++;
                _levelRecordsCount[level]++;
                if (untied)
                {
                    UntiedRecordsCount++;
                    _levelUntiedRecordsCount[level]++;
                }
            }
            else if (ranking.Rank == 2)
            {
                points = 97;
            }

            Points += points;
            _levelPoints[level] += points;
            
            GetDetailsByLevel(stage).Add(level, (ranking.Rank, points, ranking.Time));

            if (ranking.Time < UnsetTimeValueSeconds)
            {
                CumuledTime -= UnsetTimeValueSeconds - ranking.Time;
                _levelCumuledTime[level] -= UnsetTimeValueSeconds - ranking.Time;
            }
        }

        private Dictionary<Level, (int, int, long?)> GetDetailsByLevel(Stage stage)
        {
            Dictionary<Level, (int, int, long?)> detailsByLevel;
            if (!_details.ContainsKey(stage))
            {
                detailsByLevel = new Dictionary<Level, (int, int, long?)>();
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
