using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Builds a ranking.
    /// </summary>
    public class RankingBuilder
    {
        private readonly DateTime _rankingDate;
        private readonly Game _game;
        private readonly List<EntryDto> _entries;
        private readonly IReadOnlyDictionary<long, (string, DateTime)> _players;
        private readonly RankingConfiguration _configuration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entries">Collection of <see cref="EntryDto"/>.</param>
        /// <param name="players">Collection of <see cref="PlayerDto"/>.</param>
        /// <param name="configuration">Ranking configuration.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <param name="game">Game; inferred if not specified.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="players"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
        /// <exception cref="ArgumentException">Unables to retrieve the game from <paramref name="entries"/>.</exception>
        public RankingBuilder(IReadOnlyCollection<EntryDto> entries,
            IReadOnlyCollection<PlayerDto> players,
            RankingConfiguration configuration,
            DateTime rankingDate,
            Game? game)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _players = players.ToDictionary(p => p.Id, p =>
                (p.RealName, p.JoinDate.GetValueOrDefault(_rankingDate)));
            _rankingDate = rankingDate.Date;

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Count == 0)
            {
                throw new ArgumentException($"{nameof(entries)} is empty.", nameof(entries));
            }

            IEnumerable<EntryDto> filteredEntries = entries
                .Where(e => e.Time.HasValue)
                .Where(e => _players.ContainsKey(e.PlayerId))
                .Where(e => _players[e.PlayerId].Item2 <= _rankingDate);

            _game = game ?? GetGameFromEntries(filteredEntries);

            _entries = new List<EntryDto>();
            foreach (IGrouping<long, EntryDto> entryGroup in LoopByPlayerStageAndLevel(filteredEntries))
            {
                IEnumerable<EntryDto> dateableEntries = GetDateableEntries(entryGroup);
                if (dateableEntries.Any())
                {
                    _entries.Add(GetBestTimeFromEntries(dateableEntries));
                }
            }
        }

        /// <summary>
        /// Computes and gets the ranking.
        /// </summary>
        /// <returns>
        /// Collection of <see cref="RankingEntry"/>;
        /// sorted by <see cref="RankingEntry.Points"/> descending.
        /// </returns>
        public IReadOnlyCollection<RankingEntry> GetRankingEntries()
        {
            List<RankingEntry> rankingEntries = _entries
                .GroupBy(e => e.PlayerId)
                .Select(e => new RankingEntry(_game, e.Key, _players[e.Key].Item1))
                .ToList();

            var entriesByStageAndLevel = _entries.GroupBy(e => new { e.StageId, e.LevelId });
            foreach (var entry in entriesByStageAndLevel)
            {
                int i = 0;
                foreach (var timesGroup in entry.GroupBy(l => l.Time).OrderBy(l => l.Key))
                {
                    if (i >= 100)
                    {
                        break;
                    }

                    int ranking = i + 1;
                    foreach (EntryDto timeEntry in timesGroup)
                    {
                        rankingEntries
                            .Single(e => e.PlayerId == timeEntry.PlayerId)
                            .AddStageAndLevelDatas(timeEntry, ranking, timesGroup.Count() == 1);
                    }

                    i += timesGroup.Count();
                }
            }

            return rankingEntries
                .OrderByDescending(r => r.Points)
                .ThenBy(r => r.CumuledTime)
                .ToList();
        }

        /// <summary>
        /// Computes the ranking for each stage and each level at the specified date.
        /// </summary>
        /// <param name="actionDelegate">Delegate to process the output <see cref="RankingDto"/>.</param>
        /// <returns>Nothing.</returns>
        public async Task GenerateRankings(Func<RankingDto, Task> actionDelegate)
        {
            foreach (var stageLevelGroup in _entries.GroupBy(e => new { e.StageId, e.LevelId }))
            {
                int i = 0;
                foreach (var timesGroup in stageLevelGroup.GroupBy(l => l.Time).OrderBy(l => l.Key))
                {
                    int ranking = i + 1;
                    foreach (EntryDto timeEntry in timesGroup)
                    {
                        var rkDto = new RankingDto
                        {
                            Date = _rankingDate,
                            LevelId = stageLevelGroup.Key.LevelId,
                            PlayerId = timeEntry.PlayerId,
                            Rank = ranking,
                            StageId = stageLevelGroup.Key.StageId,
                            Time = timesGroup.Key.Value
                        };

                        await actionDelegate(rkDto).ConfigureAwait(false);
                    }

                    i += timesGroup.Count();
                }
            }
        }

        private Game GetGameFromEntries(IEnumerable<EntryDto> entries)
        {
            Game? game = entries.First().Game;

            if (entries.Any(e => e.Game != game))
            {
                game = null;
            }

            return game ?? throw new ArgumentException($"Unables to retrieve the game from {nameof(entries)}.", nameof(entries));
        }

        private IEnumerable<IGrouping<long, EntryDto>> LoopByPlayerStageAndLevel(IEnumerable<EntryDto> entries)
        {
            foreach (IGrouping<long, EntryDto> entryPlayerGroup in entries.GroupBy(e => e.PlayerId))
            {
                foreach (IGrouping<long, EntryDto> entryStageGroup in entryPlayerGroup.GroupBy(e => e.StageId))
                {
                    foreach (IGrouping<long, EntryDto> entryLevelGroup in entryStageGroup.GroupBy(e => e.LevelId))
                    {
                        yield return entryLevelGroup;
                    }
                }
            }
        }

        private IEnumerable<EntryDto> GetDateableEntries(IEnumerable<EntryDto> entries)
        {
            // Entry date must be prior or equal to the ranking date
            // or unknown, but the player doesn't have submit entry past the ranking date with a worst time
            return entries.Where(e =>
                e.Date?.Date <= _rankingDate
                || (
                    !e.Date.HasValue
                    && _configuration.IncludeUnknownDate
                    && entries.Any(eBis => eBis.Time > e.Time && eBis.Date?.Date > _rankingDate)
                )
            );
        }

        private EntryDto GetBestTimeFromEntries(IEnumerable<EntryDto> entries)
        {
            return entries
                .OrderBy(e => e.Time)
                .First();
        }
    }
}
