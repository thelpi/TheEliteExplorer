using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Ranking builder.
    /// </summary>
    public class RankingBuilder
    {
        private readonly RankingConfiguration _configuration;
        private readonly IReadOnlyCollection<PlayerDto> _basePlayersList;
        private readonly Game _game;

        /// <summary>
        /// Collection of every time entry.
        /// </summary>
        public IReadOnlyCollection<EntryDto> Entries { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Ranking configuration.</param>
        /// <param name="basePlayersList">Players base list.</param>
        /// <param name="game">Game.</param>
        /// <param name="entries">Every time entries.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="basePlayersList"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        public RankingBuilder(RankingConfiguration configuration,
            IReadOnlyCollection<PlayerDto> basePlayersList,
            Game game,
            IReadOnlyCollection<EntryDto> entries)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _basePlayersList = basePlayersList ?? throw new ArgumentNullException(nameof(basePlayersList));
            _game = game;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>
        /// Computes and gets the full ranking at the specified date.
        /// </summary>
        /// <param name="rankingDate">Ranking date.</param>
        /// <returns>
        /// Collection of <see cref="RankingEntry"/>;
        /// sorted by <see cref="RankingEntry.Points"/> descending.
        /// </returns>
        public IReadOnlyCollection<RankingEntry> GetRankingEntries(DateTime rankingDate)
        {
            rankingDate = rankingDate.Date;

            Dictionary<long, (string, DateTime)> playersDict = GetPlayersDictionary(rankingDate);

            List<EntryDto> finalEntries = SetFinalEntriesList(rankingDate, playersDict);

            List<RankingEntry> rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => new RankingEntry(_game, e.Key, playersDict[e.Key].Item1))
                .ToList();

            foreach ((long, long, IEnumerable<EntryDto>) entryGroup in LoopByStageAndLevel(finalEntries))
            {
                int rank = 1;
                foreach (IGrouping<long, EntryDto> timesGroup in GroupAndOrderByTime(entryGroup.Item3))
                {
                    if (rank > 100)
                    {
                        break;
                    }
                    int countAtRank = timesGroup.Count();
                    bool isUntied = rank == 1 && countAtRank == 1;

                    foreach (EntryDto timeEntry in timesGroup)
                    {
                        rankingEntries
                            .Single(e => e.PlayerId == timeEntry.PlayerId)
                            .AddStageAndLevelDatas(timeEntry, rank, isUntied);
                    }
                    rank += countAtRank;
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
        /// <remarks>
        /// Does not compute ranking for a day without any submited time.
        /// Does not compute ranking for (stage,level) tuples without any submited time for the day.
        /// </remarks>
        /// <param name="actionDelegateAsync">Delegate to process the output <see cref="RankingDto"/>.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <returns>Nothing.</returns>
        public async Task GenerateRankings(DateTime rankingDate, Func<RankingDto, Task> actionDelegateAsync)
        {
            rankingDate = rankingDate.Date;

            // Do nothing for date without time entries.
            if (!Entries.Any(e => e.Date?.Date == rankingDate))
            {
                return;
            }

            var playersDict = GetPlayersDictionary(rankingDate);

            var finalEntries = SetFinalEntriesList(rankingDate, playersDict);

            foreach ((long, long, IEnumerable<EntryDto>) entryGroup in LoopByStageAndLevel(finalEntries))
            {
                // Do nothing for date without time entries for (stage,level) tuple.
                if (!finalEntries.Any(e =>
                    e.Date?.Date == rankingDate
                    && entryGroup.Item1 == e.StageId
                    && entryGroup.Item2 == e.LevelId))
                {
                    continue;
                }

                var rank = 1;
                foreach (IGrouping<long, EntryDto> timesGroup in GroupAndOrderByTime(entryGroup.Item3))
                {
                    foreach (var timeEntry in timesGroup)
                    {
                        var rkDto = new RankingDto
                        {
                            Date = rankingDate,
                            LevelId = entryGroup.Item2,
                            PlayerId = timeEntry.PlayerId,
                            Rank = rank,
                            StageId = entryGroup.Item1,
                            Time = timesGroup.Key
                        };

                        await actionDelegateAsync(rkDto).ConfigureAwait(false);
                    }
                    rank += timesGroup.Count();
                }
            }
        }

        private static IOrderedEnumerable<IGrouping<long, EntryDto>> GroupAndOrderByTime(IEnumerable<EntryDto> entryGroup)
        {
            return entryGroup.GroupBy(l => l.Time).OrderBy(l => l.Key);
        }

        private List<EntryDto> SetFinalEntriesList(DateTime rankingDate, Dictionary<long, (string, DateTime)> playersDict)
        {
            var filteredEntries = Entries
                .Where(e => playersDict.ContainsKey(e.PlayerId))
                .Where(e => playersDict[e.PlayerId].Item2 <= rankingDate);

            var finalEntries = new List<EntryDto>();
            foreach (var entryGroup in LoopByPlayerStageAndLevel(filteredEntries))
            {
                var dateableEntries = GetDateableEntries(entryGroup, rankingDate);
                if (dateableEntries.Any())
                {
                    finalEntries.Add(GetBestTimeFromEntries(dateableEntries));
                }
            }

            return finalEntries;
        }

        private static IEnumerable<IEnumerable<EntryDto>> LoopByPlayerStageAndLevel(IEnumerable<EntryDto> entries)
        {
            foreach (var group in entries.GroupBy(e => new { e.PlayerId, e.StageId, e.LevelId }))
            {
                yield return group;
            }
        }

        private static IEnumerable<(long, long, IEnumerable<EntryDto>)> LoopByStageAndLevel(IEnumerable<EntryDto> entries)
        {
            foreach (var group in entries.GroupBy(e => new { e.StageId, e.LevelId }))
            {
                yield return (group.Key.StageId, group.Key.LevelId, group);
            }
        }

        private IEnumerable<EntryDto> GetDateableEntries(IEnumerable<EntryDto> entries, DateTime rankingDate)
        {
            // Entry date must be prior or equal to the ranking date
            // or unknown, but the player doesn't have submit entry past the ranking date with a worst time
            return entries.Where(e =>
                e.Date?.Date <= rankingDate
                || (
                    !e.Date.HasValue
                    && _configuration.IncludeUnknownDate
                    && !HasWorstTimeLater(entries, rankingDate, e)
                )
            );
        }

        private static bool HasWorstTimeLater(IEnumerable<EntryDto> entries, DateTime rankingDate, EntryDto entry)
        {
            return entries.Any(e => e.Time > entry.Time && e.Date?.Date > rankingDate);
        }

        private static EntryDto GetBestTimeFromEntries(IEnumerable<EntryDto> entries)
        {
            return entries
                .OrderBy(e => e.Time)
                .First();
        }

        private Dictionary<long, (string RealName, DateTime)> GetPlayersDictionary(DateTime rankingDate)
        {
            return _basePlayersList.ToDictionary(p => p.Id, p =>
                (p.RealName, p.JoinDate.GetValueOrDefault(rankingDate)));
        }
    }
}
