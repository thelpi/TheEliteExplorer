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

        /// <summary>
        /// Computes and gets the full ranking at the specified date.
        /// </summary>
        /// <param name="rankingDate">Ranking date.</param>
        /// <param name="game"></param>
        /// <returns>
        /// Collection of <see cref="RankingEntry"/>;
        /// sorted by <see cref="RankingEntry.Points"/> descending.
        /// </returns>
        public async Task<IReadOnlyCollection<RankingEntry>> GetRankingEntries(Game game, DateTime rankingDate)
        {
            var basePlayersList = await _sqlContext.GetPlayersAsync().ConfigureAwait(false);

            var entries = await _sqlContext.GetEntriesAsync((long)game).ConfigureAwait(false);

            var _basePlayersList = basePlayersList?.ToDictionary(p => p.Id, p => p);

            rankingDate = rankingDate.Date;

            List<EntryDto> finalEntries = SetFinalEntriesList(game, entries, _basePlayersList, rankingDate);

            List<RankingEntry> rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => new RankingEntry(game, e.Key, _basePlayersList[e.Key].RealName))
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

        /// <inheritdoc />
        public async Task GenerateRankings(Game game)
        {
            var basePlayersList = await _sqlContext.GetPlayersAsync().ConfigureAwait(false);

            var Entries = await _sqlContext.GetEntriesAsync((long)game).ConfigureAwait(false);

            var _basePlayersList = basePlayersList?.ToDictionary(p => p.Id, p => p);

            var startDate = await _sqlContext
                .GetLatestRankingDateAsync(game)
                .ConfigureAwait(false);

            foreach (var rankingDate in (startDate ?? game.GetEliteFirstDate()).LoopBetweenDates(DateStep.Day))
            {
                await InternalGenerateRankings(game, Entries, rankingDate, _basePlayersList)
                    .ConfigureAwait(false);
            }
        }

        private async Task InternalGenerateRankings(
            Game game,
            IReadOnlyCollection<EntryDto> Entries,
            DateTime rankingDate,
            Dictionary<long, PlayerDto> _basePlayersList)
        {
            rankingDate = rankingDate.Date;

            // Do nothing for date without time entries.
            if (!Entries.Any(e => e.Date?.Date == rankingDate))
            {
                return;
            }

            var finalEntries = SetFinalEntriesList(game, Entries, _basePlayersList, rankingDate);

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

        private static IOrderedEnumerable<IGrouping<long, EntryDto>> GroupAndOrderByTime(IEnumerable<EntryDto> entryGroup)
        {
            return entryGroup.GroupBy(l => l.Time).OrderBy(l => l.Key);
        }

        private List<EntryDto> SetFinalEntriesList(
            Game game,
            IReadOnlyCollection<EntryDto> Entries,
            Dictionary<long, PlayerDto> _basePlayersList,
            DateTime rankingDate)
        {
            var filteredEntries = Entries
                .Where(e => _basePlayersList.ContainsKey(e.PlayerId))
                .Where(e => _basePlayersList[e.PlayerId].JoinDate.GetValueOrDefault(rankingDate) <= rankingDate);

            var finalEntries = new List<EntryDto>();
            foreach (var entryGroup in LoopByPlayerStageAndLevel(filteredEntries))
            {
                var dateableEntries = GetDateableEntries(game, Entries, _basePlayersList, entryGroup, rankingDate);
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

        private IEnumerable<EntryDto> GetDateableEntries(
            Game game,
            IReadOnlyCollection<EntryDto> Entries,
            Dictionary<long, PlayerDto> _basePlayersList,
            IEnumerable<EntryDto> entries,
            DateTime rankingDate)
        {
            // Entry date must be prior or equal to the ranking date
            // If no date on the entry, we try to guess it
            return entries.Where(e =>
                e.Date?.Date <= rankingDate
                || (
                    !e.Date.HasValue
                    && _configuration.IncludeUnknownDate
                    && ComputeNearDate(game, Entries, _basePlayersList, e, rankingDate) <= rankingDate
                )
            );
        }

        private DateTime ComputeNearDate(
            Game game,
            IReadOnlyCollection<EntryDto> Entries,
            Dictionary<long, PlayerDto> _basePlayersList,
            EntryDto entry,
            DateTime rankingDate)
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
                : GetJoinDateForPlayer(game, _basePlayersList, entry);
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

        private DateTime GetJoinDateForPlayer(Game game, Dictionary<long, PlayerDto> players, EntryDto entry)
        {
            return players[entry.PlayerId].JoinDate ?? game.GetEliteFirstDate();
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
