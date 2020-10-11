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
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Ranking configuration.</param>
        /// <param name="basePlayersList">Players base list.</param>
        /// <param name="game">Game.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="basePlayersList"/> is <c>Null</c>.</exception>
        public RankingBuilder(RankingConfiguration configuration,
            IReadOnlyCollection<PlayerDto> basePlayersList,
            Game game)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _basePlayersList = basePlayersList ?? throw new ArgumentNullException(nameof(basePlayersList));
            _game = game;
        }

        /// <summary>
        /// Computes and gets the full ranking at the specified date.
        /// </summary>
        /// <returns>
        /// Collection of <see cref="RankingEntry"/>;
        /// sorted by <see cref="RankingEntry.Points"/> descending.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
        public async Task<IReadOnlyCollection<RankingEntry>> GetRankingEntriesAsync(
            IReadOnlyCollection<EntryDto> entries,
            DateTime rankingDate)
        {
            CheckEntriesParameter(entries);

            rankingDate = rankingDate.Date;

            Dictionary<long, (string, DateTime)> playersDict = GetPlayersDictionary(rankingDate);

            List<EntryDto> finalEntries = SetFinalEntriesList(entries, rankingDate, playersDict);

            List<RankingEntry> rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => new RankingEntry(_game, e.Key, playersDict[e.Key].Item1))
                .ToList();

            await LoopTimesAndActAsync(finalEntries, rankingDate, 100,
                async (ranking, stageId, levelId, time, timeEntry, timesCount) =>
                {
                    // The task is overkill here but it prevents a compiler warning,
                    // as the expected parameters is an asynchronous delegate
                    await Task.Run(() =>
                    {
                        rankingEntries
                            .Single(e => e.PlayerId == timeEntry.PlayerId)
                            .AddStageAndLevelDatas(timeEntry, ranking, timesCount == 1);
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

            return rankingEntries
                .OrderByDescending(r => r.Points)
                .ThenBy(r => r.CumuledTime)
                .ToList();
        }

        /// <summary>
        /// Computes the ranking for each stage and each level at the specified date.
        /// </summary>
        /// <remarks>Does not compute ranking for a day without any submited time.</remarks>
        /// <param name="entries">Base list of entries.</param>
        /// <param name="actionDelegateAsync">Delegate to process the output <see cref="RankingDto"/>.</param>
        /// <param name="rankingDate">Ranking date.</param>
        /// <returns>Nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
        public async Task GenerateRankings(IReadOnlyCollection<EntryDto> entries,
            DateTime rankingDate, Func<RankingDto, Task> actionDelegateAsync)
        {
            CheckEntriesParameter(entries);

            rankingDate = rankingDate.Date;

            // nothing to do
            if (!entries.Any(e => e.Date?.Date == rankingDate))
            {
                return;
            }

            Dictionary<long, (string, DateTime)> playersDict = GetPlayersDictionary(rankingDate);

            List<EntryDto> finalEntries = SetFinalEntriesList(entries, rankingDate, playersDict);

            await LoopTimesAndActAsync(finalEntries, rankingDate, null,
                async (ranking, stageId, levelId, time, timeEntry, timesCount) =>
                {
                    var rkDto = new RankingDto
                    {
                        Date = rankingDate,
                        LevelId = levelId,
                        PlayerId = timeEntry.PlayerId,
                        Rank = ranking,
                        StageId = stageId,
                        Time = time
                    };

                    await actionDelegateAsync(rkDto).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        private async Task LoopTimesAndActAsync(IEnumerable<EntryDto> entries,
            DateTime rankingDate,
            int? breakAt,
            Func<int, long, long, long, EntryDto, int, Task> actDelegateAsync)
        {
            foreach (var stageGroup in entries.GroupBy(e => e.StageId))
            {
                foreach (var levelGroup in stageGroup.GroupBy(e => e.LevelId))
                {
                    int i = 0;
                    foreach (var timesGroup in levelGroup.GroupBy(l => l.Time).OrderBy(l => l.Key))
                    {
                        if (breakAt.HasValue && i >= breakAt)
                        {
                            break;
                        }

                        foreach (EntryDto timeEntry in timesGroup)
                        {
                            await actDelegateAsync(i + 1,
                                stageGroup.Key, levelGroup.Key, timesGroup.Key.Value, timeEntry, timesGroup.Count()
                            ).ConfigureAwait(false);
                        }
                        i += timesGroup.Count();
                    }
                }
            }
        }

        private List<EntryDto> SetFinalEntriesList(IReadOnlyCollection<EntryDto> entries, DateTime rankingDate,
            Dictionary<long, (string, DateTime)> playersDict)
        {
            IEnumerable<EntryDto> filteredEntries = entries
                .Where(e => e.Time.HasValue)
                .Where(e => playersDict.ContainsKey(e.PlayerId))
                .Where(e => playersDict[e.PlayerId].Item2 <= rankingDate);

            var finalEntries = new List<EntryDto>();
            foreach (IGrouping<long, EntryDto> entryGroup in LoopByPlayerStageAndLevel(filteredEntries))
            {
                IEnumerable<EntryDto> dateableEntries = GetDateableEntries(entryGroup, rankingDate);
                if (dateableEntries.Any())
                {
                    finalEntries.Add(GetBestTimeFromEntries(dateableEntries));
                }
            }

            return finalEntries;
        }

        private void CheckEntriesParameter(IReadOnlyCollection<EntryDto> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Count == 0)
            {
                throw new ArgumentException($"{nameof(entries)} is empty.", nameof(entries));
            }
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

        private IEnumerable<EntryDto> GetDateableEntries(IEnumerable<EntryDto> entries, DateTime rankingDate)
        {
            // Entry date must be prior or equal to the ranking date
            // or unknown, but the player doesn't have submit entry past the ranking date with a worst time
            return entries.Where(e =>
                e.Date?.Date <= rankingDate
                || (
                    !e.Date.HasValue
                    && _configuration.IncludeUnknownDate
                    && !entries.Any(eBis => eBis.Time > e.Time && eBis.Date?.Date > rankingDate)
                )
            );
        }

        private EntryDto GetBestTimeFromEntries(IEnumerable<EntryDto> entries)
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
