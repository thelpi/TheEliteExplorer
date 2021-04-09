using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Providers
{
    /// <summary>
    /// Ranking builder.
    /// </summary>
    /// <seealso cref="IRankingProvider"/>
    public sealed class RankingProvider : IRankingProvider
    {
        private readonly RankingConfiguration _configuration;
        private readonly ISqlContext _sqlContext;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Ranking configuration.</param>
        /// <param name="sqlContext">Players base list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> or inner value is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        public RankingProvider(IOptions<RankingConfiguration> configuration,
            ISqlContext sqlContext)
        {
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<RankingEntry>> GetRankingEntries(Game game, DateTime rankingDate)
        {
            var players = await GetPlayers().ConfigureAwait(false);

            var entries = await GetEntries(game, players).ConfigureAwait(false);

            rankingDate = rankingDate.Date;

            var finalEntries = SetFinalEntriesList(game, entries, players, rankingDate);

            var rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => new RankingEntry(game, e.Key, players[e.Key].RealName))
                .ToList();

            foreach (var entryGroup in LoopByStageAndLevel(finalEntries))
            {
                int rank = 1;
                foreach (var timesGroup in GroupAndOrderByTime(entryGroup.Item3))
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

        /// <inheritdoc />
        public async Task GenerateRankings(Game game)
        {
            var players = await GetPlayers().ConfigureAwait(false);

            var entries = await GetEntries(game, players).ConfigureAwait(false);

            var startDate = await _sqlContext
                .GetLatestRankingDateAsync(game)
                .ConfigureAwait(false);

            foreach (var rankingDate in (startDate ?? game.GetEliteFirstDate()).LoopBetweenDates(DateStep.Day))
            {
                await InternalGenerateRankings(game, entries, rankingDate, players)
                    .ConfigureAwait(false);
            }
        }

        private async Task<Dictionary<long, PlayerDto>> GetPlayers()
        {
            var basePlayersList = await _sqlContext.GetPlayersAsync().ConfigureAwait(false);

            var players = basePlayersList.ToDictionary(p => p.Id, p => p);
            return players;
        }

        private async Task<IReadOnlyCollection<EntryDto>> GetEntries(
            Game game,
            Dictionary<long, PlayerDto> players)
        {
            var entries = await _sqlContext.GetEntriesAsync((long)game).ConfigureAwait(false);

            // useless ?
            var entriesList = entries
                .Where(e => players.ContainsKey(e.PlayerId))
                .ToList();

            ManageDateLessEntries(game, players, entriesList);

            return entriesList;
        }

        private IReadOnlyCollection<EntryDto> ManageDateLessEntries(Game game, Dictionary<long, PlayerDto> players, List<EntryDto> entries)
        {
            if (_configuration.NoDateEntryRankingRule == NoDateEntryRankingRule.Ignore)
            {
                entries.RemoveAll(e => !e.Date.HasValue);
            }
            else
            {
                var dateMinMaxPlayer = new Dictionary<long, (DateTime Min, DateTime Max, IReadOnlyCollection<EntryDto> Entries)>();

                var dateLessEntries = entries.Where(e => !e.Date.HasValue).ToList();
                foreach (var entry in dateLessEntries)
                {
                    if (!dateMinMaxPlayer.ContainsKey(entry.PlayerId))
                    {
                        var dateMin = players[entry.PlayerId].JoinDate ?? game.GetEliteFirstDate();
                        var dateMax = entries.Where(e => e.PlayerId == entry.PlayerId).Max(e => e.Date ?? Player.LastEmptyDate);
                        dateMinMaxPlayer.Add(entry.PlayerId, (dateMin, dateMax, entries.Where(e => e.PlayerId == entry.PlayerId).ToList()));
                    }

                    // Same time with a known date (possible for another engine/system)
                    var sameEntry = dateMinMaxPlayer[entry.PlayerId].Entries.FirstOrDefault(e => e.StageId == entry.StageId && e.LevelId == entry.LevelId && e.Time == entry.Time && e.Date.HasValue);
                    // Better time (closest to current) with a known date
                    var betterEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderBy(e => e.Time).FirstOrDefault(e => e.StageId == entry.StageId && e.LevelId == entry.LevelId && e.Time < entry.Time && e.Date.HasValue);
                    // Worse time (closest to current) with a known date
                    var worseEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderByDescending(e => e.Time).FirstOrDefault(e => e.StageId == entry.StageId && e.LevelId == entry.LevelId && e.Time < entry.Time && e.Date.HasValue);

                    if (sameEntry != null)
                    {
                        // use the another engine/system date as the current date
                        entry.Date = sameEntry.Date;
                    }
                    else
                    {
                        var realMin = dateMinMaxPlayer[entry.PlayerId].Min;
                        if (worseEntry != null && worseEntry.Date > realMin)
                        {
                            realMin = worseEntry.Date.Value;
                        }

                        var realMax = dateMinMaxPlayer[entry.PlayerId].Max;
                        if (betterEntry != null && betterEntry.Date < realMax)
                        {
                            realMax = betterEntry.Date.Value;
                        }

                        switch (_configuration.NoDateEntryRankingRule)
                        {
                            case NoDateEntryRankingRule.Average:
                                entry.Date = realMin.AddDays((realMax - realMin).TotalDays / 2).Date;
                                break;
                            case NoDateEntryRankingRule.Max:
                                entry.Date = realMax;
                                break;
                            case NoDateEntryRankingRule.Min:
                                entry.Date = realMin;
                                break;
                            case NoDateEntryRankingRule.PlayerHabit:
                                var entriesBetween = dateMinMaxPlayer[entry.PlayerId].Entries
                                    .Where(e => e.Date < realMax && e.Date > realMin)
                                    .Select(e => Convert.ToInt32((ServiceProviderAccessor.ClockProvider.Now - e.Date.Value).TotalDays))
                                    .ToList();
                                if (entriesBetween.Count == 0)
                                {
                                    entry.Date = realMin.AddDays((realMax - realMin).TotalDays / 2).Date;
                                }
                                else
                                {
                                    var avgDays = entriesBetween.Average();
                                    entry.Date = ServiceProviderAccessor.ClockProvider.Now.AddDays(-avgDays).Date;
                                }
                                break;
                        }
                    }
                }
            }

            return entries;
        }

        private async Task InternalGenerateRankings(
            Game game,
            IReadOnlyCollection<EntryDto> entries,
            DateTime rankingDate,
            Dictionary<long, PlayerDto> players)
        {
            rankingDate = rankingDate.Date;

            // Do nothing for date without time entries.
            if (!entries.Any(e => e.Date?.Date == rankingDate))
            {
                return;
            }

            var finalEntries = SetFinalEntriesList(game, entries, players, rankingDate);

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

                        await _sqlContext.InsertRankingAsync(rkDto).ConfigureAwait(false);
                    }
                    rank += timesGroup.Count();
                }
            }
        }

        private static IOrderedEnumerable<IGrouping<long, EntryDto>> GroupAndOrderByTime(
            IEnumerable<EntryDto> entryGroup)
        {
            return entryGroup.GroupBy(l => l.Time).OrderBy(l => l.Key);
        }

        private List<EntryDto> SetFinalEntriesList(
            Game game,
            IReadOnlyCollection<EntryDto> entries,
            Dictionary<long, PlayerDto> players,
            DateTime rankingDate)
        {
            var filteredEntries = entries
                .Where(e => players[e.PlayerId].JoinDate.GetValueOrDefault(rankingDate) <= rankingDate)
                .GroupBy(e => (e.PlayerId, e.StageId, e.LevelId))
                .ToList();

            var finalEntries = new List<EntryDto>();
            foreach (var entryGroup in filteredEntries)
            {
                var dateableEntries = entries.Where(e => e.Date.Value.Date <= rankingDate).ToList();
                if (dateableEntries.Count > 0)
                {
                    finalEntries.Add(GetBestTimeFromEntries(dateableEntries));
                }
            }

            return finalEntries;
        }

        private static IEnumerable<(long, long, IEnumerable<EntryDto>)> LoopByStageAndLevel(
            IEnumerable<EntryDto> entries)
        {
            foreach (var group in entries.GroupBy(e => new { e.StageId, e.LevelId }))
            {
                yield return (group.Key.StageId, group.Key.LevelId, group);
            }
        }

        private static EntryDto GetBestTimeFromEntries(
            IReadOnlyCollection<EntryDto> entries)
        {
            return entries
                .OrderBy(e => e.Time)
                .First();
        }
    }
}
