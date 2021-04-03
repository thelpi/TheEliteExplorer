using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Providers
{
    /// <summary>
    /// Ranking builder.
    /// </summary>
    public class RankingBuilder
    {
        private readonly RankingConfiguration _configuration;
        private readonly IReadOnlyDictionary<long, PlayerDto> _basePlayersList;
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
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            Entries = entries ??
                throw new ArgumentNullException(nameof(entries));
            _basePlayersList = basePlayersList?.ToDictionary(p => p.Id, p => p) ??
                throw new ArgumentNullException(nameof(basePlayersList));
            _game = game;
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

            List<EntryDto> finalEntries = SetFinalEntriesList(rankingDate);

            List<RankingEntry> rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => new RankingEntry(_game, e.Key, _basePlayersList[e.Key].RealName))
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

            var finalEntries = SetFinalEntriesList(rankingDate);

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

        private List<EntryDto> SetFinalEntriesList(DateTime rankingDate)
        {
            var filteredEntries = Entries
                .Where(e => _basePlayersList.ContainsKey(e.PlayerId))
                .Where(e => _basePlayersList[e.PlayerId].JoinDate.GetValueOrDefault(rankingDate) <= rankingDate);

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
            // If no date on the entry, we try to guess it
            return entries.Where(e =>
                e.Date?.Date <= rankingDate
                || (
                    !e.Date.HasValue
                    && _configuration.IncludeUnknownDate
                    && ComputeNearDate(e, rankingDate) <= rankingDate
                )
            );
        }

        private DateTime ComputeNearDate(EntryDto entry, DateTime rankingDate)
        {
            var playerEntries = Entries.Where(e => e.Id != entry.Id && e.PlayerId == entry.PlayerId);
            var playerStageLevelEntries = playerEntries.Where(e => e.StageId == entry.StageId && e.LevelId == entry.LevelId);

            if (HasWorstTimeLater(playerStageLevelEntries, rankingDate, entry))
            {
                // Any date after the ranking date works here
                return rankingDate.AddDays(1);
            }

            var betweenMin = playerStageLevelEntries.Any(e => e.Date < rankingDate)
                ? playerStageLevelEntries.Where(e => e.Date < rankingDate).Max(e => e.Date.Value)
                : GetJoinDateForPlayer(entry);
            var betweenMax = playerStageLevelEntries.Any(e => e.Date > rankingDate && e.Date < Player.LastEmptyDate)
                ? playerStageLevelEntries.Where(e => e.Date > rankingDate).Min(e => e.Date.Value)
                : GetExitDateForPlayer(playerEntries);

            // This case might happen for newcomers
            if (betweenMax < betweenMin)
            {
                betweenMax = betweenMin;
            }

            // TODO: find a better method to compute the posting pattern of the player
            var entriesInDateRange = playerEntries.Where(e => e.Date >= betweenMin && e.Date <= betweenMax);
            if (entriesInDateRange.Count() > 0)
            {
                // Takes the year where the player has submitted the most times
                var selectedYear = entriesInDateRange
                    .GroupBy(e => e.Date.Value.Year)
                    .OrderByDescending(grp => grp.Count())
                    .First()
                    .Key;
                betweenMin = new DateTime(selectedYear, 1, 1);
                betweenMax = new DateTime(selectedYear + 1, 1, 1);
            }

            return betweenMin.AddDays((betweenMax - betweenMin).TotalDays / 2).Date;
        }

        private DateTime GetJoinDateForPlayer(EntryDto entry)
        {
            return _basePlayersList[entry.PlayerId].JoinDate ?? _game.GetEliteFirstDate();
        }

        private static DateTime GetExitDateForPlayer(IEnumerable<EntryDto> playerEntries)
        {
            // times post-"Player.LastEmptyDate" are ignored
            var exitDate = playerEntries.Any(e => e.Date.HasValue)
                ? playerEntries.Max(e => e.Date).Value
                : Player.LastEmptyDate;
            return exitDate > Player.LastEmptyDate ? Player.LastEmptyDate : exitDate;
        }

        private static bool HasWorstTimeLater(IEnumerable<EntryDto> entries, DateTime rankingDate, EntryDto entry)
        {
            return entries.Any(e => e.Time > entry.Time
                && e.Date?.Date > rankingDate);
            // We could take in consideration the engine, as shown below:
            // && (!entry.SystemId.HasValue || !e.SystemId.HasValue || entry.SystemId == e.SystemId)
        }

        private static EntryDto GetBestTimeFromEntries(IEnumerable<EntryDto> entries)
        {
            return entries
                .OrderBy(e => e.Time)
                .First();
        }
    }
}
