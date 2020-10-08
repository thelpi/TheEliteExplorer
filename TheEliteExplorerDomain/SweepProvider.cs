using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Sweep provider.
    /// </summary>
    public class SweepProvider
    {
        private readonly List<IGrouping<EntryGroup, EntryDto>> _entries;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entries">Entries collection.</param>
        public SweepProvider(IReadOnlyCollection<EntryDto> entries)
        {
            _entries = entries
                .Where(e => e.Time.HasValue && e.Date.HasValue)
                .GroupBy(e => new EntryGroup(e))
                .ToList();
        }

        /// <summary>
        /// Computes a list of <see cref="Sweep"/>.
        /// </summary>
        /// <returns>List of <see cref="Sweep"/>.</returns>
        public IReadOnlyCollection<Sweep> GetSweeps()
        {
            var sweeps = new List<Sweep>();

            var currentWrInfos = new Dictionary<(long, long), (long, List<long>)>();

            foreach (IGrouping<EntryGroup, EntryDto> groupEntries in _entries)
            {
                (long, long) key = groupEntries.Key.Key;

                if (!currentWrInfos.ContainsKey(key))
                {
                    currentWrInfos.Add(key, (0, new List<long>()));
                }

                (long, List<long>) currentWr = currentWrInfos[key];
                IGrouping<long, EntryDto> bestTime = groupEntries.GroupBy(e => e.Time.Value).OrderBy(e => e.Key).First();

                bool isNewUntied = currentWr.Item1 > bestTime.Key || currentWr.Item1 == 0;
                bool isTied = currentWr.Item1 == bestTime.Key;
                // distinct because the same player can get the best time on several engines the same day
                List<long> wrPlayersToday = bestTime.Select(e => e.PlayerId).Distinct().ToList();

                if (isNewUntied)
                {
                    currentWrInfos[key] = (bestTime.Key, wrPlayersToday);
                }
                else if (isTied)
                {
                    // "where" statement to exclude known players who got a WR on a new engine
                    IEnumerable<long> newPlayers = wrPlayersToday.Where(ep => !currentWr.Item2.Contains(ep));
                    currentWr.Item2.AddRange(newPlayers);
                }
            }

            return sweeps;
        }

        private struct EntryGroup
        {
            public (long, long) Key { get; set; }
            public DateTime Date { get; set; }
            public long StageId { get { return Key.Item1; } }
            public long LevelId { get { return Key.Item2; } }

            public EntryGroup(EntryDto entry)
            {
                Date = entry.Date.Value.Date;
                Key = (entry.StageId, entry.LevelId);
            }
        }
    }
}
