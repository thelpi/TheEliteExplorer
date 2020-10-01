using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorer.Infrastructure.Dtos;

namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Builds a ranking.
    /// </summary>
    public class RankingBuilder
    {
        private readonly Game _game;
        private readonly List<EntryDto> _entries;
        private readonly IReadOnlyCollection<PlayerDto> _players;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="entries">Collection of <see cref="EntryDto"/>.</param>
        /// <param name="players">Collection of <see cref="PlayerDto"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="players"/> is <c>Null</c>.</exception>
        public RankingBuilder(Game game, IReadOnlyCollection<EntryDto> entries, IReadOnlyCollection<PlayerDto> players)
        {
            // TODO: check the consistency of entries with game
            // or stop passing the game and infers it from entries
            _game = game;
            _players = players ?? throw new ArgumentNullException(nameof(players));

            var baseEntries = new List<EntryDto>(entries ?? throw new ArgumentNullException(nameof(entries)));

            _entries = new List<EntryDto>();
            foreach (var entryPlayerGroup in baseEntries.Where(e => e.Time.HasValue).GroupBy(p => p.PlayerId))
            {
                if (!_players.Any(p => p.Id == entryPlayerGroup.Key))
                {
                    // ignore the entry if the related player is not known
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
