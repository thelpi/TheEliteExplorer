using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Builds a ranking.
    /// </summary>
    public class RankingBuilder
    {
        private readonly Game _game;
        private readonly List<EntryDto> _entries;
        private readonly IReadOnlyCollection<PlayerDto> _players;
        private readonly RankingConfiguration _configuration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entries">Collection of <see cref="EntryDto"/>.</param>
        /// <param name="players">Collection of <see cref="PlayerDto"/>.</param>
        /// <param name="configuration">Ranking configuration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="players"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
        /// <exception cref="ArgumentException">Unables to retrieve the game from <paramref name="entries"/>.</exception>
        public RankingBuilder(IReadOnlyCollection<EntryDto> entries,
            IReadOnlyCollection<PlayerDto> players,
            RankingConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _players = players ?? throw new ArgumentNullException(nameof(players));

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Count == 0)
            {
                throw new ArgumentException($"{nameof(entries)} is empty.", nameof(entries));
            }
            
            _game = GetGameFromEntries(entries);

            _entries = new List<EntryDto>();
            foreach (var entryPlayerGroup in entries.Where(e => e.Time.HasValue).GroupBy(p => p.PlayerId))
            {
                if (!_players.Any(p => p.Id == entryPlayerGroup.Key))
                {
                    // ignore the entry if the related player is unknown.
                    continue;
                }

                foreach (var entryStageLevelgroup in entryPlayerGroup.GroupBy(e => new { e.StageId, e.LevelId }))
                {
                    _entries.Add(entryStageLevelgroup
                        .OrderBy(e => e.Time.Value)
                        .First());
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
                .Select(e => new RankingEntry(_game, e.Key, _players.First(p => p.Id == e.Key).RealName))
                .ToList();

            var entriesByStageAndLevel = _entries.GroupBy(e => new { e.StageId, e.LevelId });
            foreach (var entry in entriesByStageAndLevel)
            {
                int i = 0;
                foreach (var timesGroup in entry.GroupBy(l => l.Time.Value).OrderBy(l => l.Key))
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
    }
}
