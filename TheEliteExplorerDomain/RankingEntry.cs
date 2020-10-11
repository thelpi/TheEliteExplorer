using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">The <see cref="Game"/> value.</param>
        /// <param name="playerId">The <see cref="PlayerId"/> value.</param>
        /// <param name="playerName">The <see cref="PlayerName"/> value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="game"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerId"/> is not a valid identifier.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="playerName"/> is <c>Null</c>, empty or white spaces only.</exception>
        public RankingEntry(Game game, long playerId, string playerName)
        {
            if (playerId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), playerId, $"{playerId} is not a valid identifier.");
            }

            Game = game;
            PlayerId = playerId;
            PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
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

        /// <summary>
        /// Integrates a time entry to the instance statistics.
        /// </summary>
        /// <param name="entry">The time entry.</param>
        /// <param name="position">The position (ranking) of the entry for this stage and level.</param>
        /// <param name="untied">Indicates if the entry is an untied world record; ignored if <paramref name="position"/> is not <c>1</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entry"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="entry"/> is not related to <see cref="PlayerId"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entry"/> is not related to <see cref="Game"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is below <c>1</c>.</exception>
        public void AddStageAndLevelDatas(EntryDto entry, int position, bool untied)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (!entry.IsValid())
            {
                throw new ArgumentException($"{entry} is invalid.", nameof(entry));
            }

            if (entry.PlayerId != PlayerId)
            {
                throw new ArgumentException($"{entry} is not related to the player {PlayerId}.", nameof(entry));
            }

            Stage stage = Stage.Get(Game).FirstOrDefault(s => s.Id == entry.StageId);
            if (stage == null)
            {
                throw new ArgumentException($"{entry} is not related to the game {Game}.", nameof(entry));
            }

            if (position < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, $"{nameof(position)} is below 1.");
            }

            Level level = (Level)entry.LevelId;

            int points = (100 - position) - 2;
            if (points < 0)
            {
                points = 0;
            }
            if (position == 1)
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
            else if (position == 2)
            {
                points = 97;
            }

            Points += points;
            _levelPoints[level] += points;
            
            GetDetailsByLevel(stage).Add(level, (position, points, entry.Time));

            if (entry.Time < UnsetTimeValueSeconds)
            {
                CumuledTime -= UnsetTimeValueSeconds - entry.Time;
                _levelCumuledTime[level] -= UnsetTimeValueSeconds - entry.Time;
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
