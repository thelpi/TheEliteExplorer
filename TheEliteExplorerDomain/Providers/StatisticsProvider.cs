using System;
using System.Collections.Concurrent;
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
    /// Statistics provider.
    /// </summary>
    /// <seealso cref="IStatisticsProvider"/>
    public sealed class StatisticsProvider : IStatisticsProvider
    {
        private readonly IReadRepository _readRepository;
        private readonly RankingConfiguration _configuration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Ranking configuration.</param>
        /// <param name="readRepository">Instance of <see cref="IReadRepository"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> or inner value is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="readRepository"/> is <c>Null</c>.</exception>
        public StatisticsProvider(
            IReadRepository readRepository,
            IOptions<RankingConfiguration> configuration)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region IStatisticsProvider implementation

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageEntryCount>> GetStagesEntriesCountAsync(
            Game game,
            DateTime startDate,
            DateTime endDate,
            bool levelDetails,
            DateTime? globalStartDate,
            DateTime? globalEndDate)
        {
            var entries = new List<EntryDto>();

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var datas = await _readRepository
                        .GetEntriesAsync(stage, level, startDate, endDate)
                        .ConfigureAwait(false);

                    entries.AddRange(datas);
                }
            }

            var results = new List<StageEntryCount>();

            foreach (var stage in game.GetStages())
            {
                var levels = !levelDetails
                    ? new List<Level?> { null }
                    : SystemExtensions.Enumerate<Level>().Select(l => (Level?)l).ToList();
                foreach (var level in levels)
                {
                    results.Add(new StageEntryCount
                    {
                        EndDate = endDate,
                        StartDate = startDate,
                        AllStagesEntriesCount = entries.Count,
                        Level = level,
                        PeriodEntriesCount = entries.Count(e => e.Stage == stage && (!level.HasValue || e.Level == level)),
                        Stage = stage,
                        TotalEntriesCount = await _readRepository
                            .GetEntriesCountAsync(stage, level, globalStartDate, globalEndDate)
                            .ConfigureAwait(false)
                    });
                }
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(RankingRequest request)
        {
            request.Players = await GetPlayersAsync().ConfigureAwait(false);

            return await GetFullGameConsolidatedRankingAsync(request)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageSweep>> GetSweepsAsync(
            Game game,
            bool untied,
            DateTime? startDate,
            DateTime? endDate,
            Stage? stage)
        {
            if (startDate > endDate)
            {
                throw new ArgumentOutOfRangeException(nameof(startDate), startDate,
                    $"{nameof(startDate)} is greater than {nameof(endDate)}.");
            }

            var fullEntries = new List<EntryDto>();

            foreach (var locStage in game.GetStages())
            {
                var entries = await _readRepository
                    .GetEntriesAsync(locStage)
                    .ConfigureAwait(false);

                fullEntries.AddRange(entries);
            }

            var entriesGroups = fullEntries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.Stage, e.Level))
                .ToDictionary(e => e.Key, e => e.ToList());

            var playerKeys = await GetPlayersAsync().ConfigureAwait(false);

            var sweepsRaw = new ConcurrentBag<(long playerId, DateTime date, Stage stage)>();

            var dates = SystemExtensions
                .LoopBetweenDates(
                    startDate ?? Extensions.GetEliteFirstDate(game),
                    endDate ?? ServiceProviderAccessor.ClockProvider.Now,
                    DateStep.Day)
                .GroupBy(d => d.DayOfWeek)
                .ToDictionary(d => d.Key, d => d);
            Parallel.ForEach(Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>(), dow =>
            {
                foreach (var currentDate in dates[dow])
                {
                    foreach (var stg in game.GetStages())
                    {
                        if (stage == null || stg == stage)
                        {
                            var sweeps = GetPotentialSweeps(untied, entriesGroups, currentDate, stg);
                            foreach (var sw in sweeps)
                                sweepsRaw.Add(sw);
                        }
                    }
                }
            });

            var finalSweeps = new List<StageSweep>();

            foreach (var (playerId, date, locStage) in sweepsRaw.OrderBy(f => f.date))
            {
                var sweepMatch = finalSweeps.SingleOrDefault(s =>
                    s.PlayerId == playerId
                    && s.Stage == locStage
                    && s.EndDate == date);

                if (sweepMatch == null)
                {
                    sweepMatch = new StageSweep(date, locStage, playerKeys[playerId]);
                    finalSweeps.Add(sweepMatch);
                }

                sweepMatch.AddDay();
            }

            return finalSweeps;
        }

        /// <inheritdoc />
        public async Task<Dictionary<Stage, Dictionary<Level, (EntryDto, bool)>>> GetLastTiedWrsAsync(Game game, DateTime date)
        {
            var daysByStage = new ConcurrentDictionary<Stage, Dictionary<Level, (EntryDto, bool)>>();

            var tasks = new List<Task>();
            foreach (var stage in game.GetStages())
                tasks.Add(GetSingleStageGetLastTiedWrsAsync(daysByStage, date, stage));

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            return daysByStage.ToDictionary(_ => _.Key, _ => _.Value);
        }

        /*/// <inheritdoc />
        public async Task GeneratePermanentRankingsBetweenDatesAsync(Game game, DateTime fromDate, DateTime? toDate, long rankingTypeId)
        {
            var startDate = fromDate.Date;
            var endDate = (toDate ?? ServiceProviderAccessor.ClockProvider.Now).Date;

            var request = new RankingRequest
            {
                Entries = new ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>>(),
                Game = game,
                Players = await GetPlayersAsync().ConfigureAwait(false)
            };

            foreach (var date in startDate.LoopBetweenDates(endDate, DateStep.Day))
            {
                request.RankingDate = date;

                var rankings = await GetFullGameRankingAsync(request)
                    .ConfigureAwait(false);

                foreach (var ranking in rankings)
                {
                    await _writeRepository
                        .InsertRankingEntryAsync(ranking)
                        .ConfigureAwait(false);
                }
            }
        }*/

        #endregion IStatisticsProvider implementation

        #region Generic private methods

        // Gets a adictionary of every player by identifier (including dirty, but not banned)
        private async Task<IReadOnlyDictionary<long, PlayerDto>> GetPlayersAsync()
        {
            var playersSourceClean = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var playersSourceDirty = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            return playersSourceClean.Concat(playersSourceDirty).ToDictionary(p => p.Id, p => p);
        }

        // Sets a fake date on emtries without it
        private void ManageDateLessEntries(
            Game game,
            IReadOnlyDictionary<long, PlayerDto> players,
            List<EntryDto> entries)
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
                        var dateMin = entries
                            .Where(e => e.PlayerId == entry.PlayerId && e.Date.HasValue)
                            .Select(e => e.Date.Value)
                            .Concat(game.GetEliteFirstDate().Yield())
                            .Min();
                        var dateMax = entries.Where(e => e.PlayerId == entry.PlayerId).Max(e => e.Date ?? Player.LastEmptyDate);
                        dateMinMaxPlayer.Add(entry.PlayerId, (dateMin, dateMax, entries.Where(e => e.PlayerId == entry.PlayerId).ToList()));
                    }

                    // Same time with a known date (possible for another engine/system)
                    var sameEntry = dateMinMaxPlayer[entry.PlayerId].Entries.FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time == entry.Time && e.Date.HasValue);
                    // Better time (closest to current) with a known date
                    var betterEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderBy(e => e.Time).FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time < entry.Time && e.Date.HasValue);
                    // Worse time (closest to current) with a known date
                    var worseEntry = dateMinMaxPlayer[entry.PlayerId].Entries.OrderByDescending(e => e.Time).FirstOrDefault(e => e.Stage == entry.Stage && e.Level == entry.Level && e.Time < entry.Time && e.Date.HasValue);

                    if (sameEntry != null)
                    {
                        // use the another engine/system date as the current date
                        entry.Date = sameEntry.Date;
                        entry.IsSimulatedDate = true;
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
                                entry.IsSimulatedDate = true;
                                break;
                            case NoDateEntryRankingRule.Max:
                                entry.Date = realMax;
                                entry.IsSimulatedDate = true;
                                break;
                            case NoDateEntryRankingRule.Min:
                                entry.Date = realMin;
                                entry.IsSimulatedDate = true;
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
                                entry.IsSimulatedDate = true;
                                break;
                        }
                    }
                }
            }
        }

        // Loops on every stages and levels of a list of entries
        private static IEnumerable<(Stage, Level, IEnumerable<RankingDto>)> LoopByStageAndLevel(
            IEnumerable<RankingDto> rankings)
        {
            foreach (var group in rankings.GroupBy(r => new { r.Stage, r.Level }))
            {
                yield return (group.Key.Stage, group.Key.Level, group);
            }
        }

        #endregion Generic private methods

        #region Ranking private methods

        // Gets the full game ranking
        private async Task<List<RankingEntryLight>> GetFullGameConsolidatedRankingAsync(RankingRequest request)
        {
            // Gets ranking
            var finalEntries = await GetFullGameRankingAsync(request)
                .ConfigureAwait(false);

            if (request.ExcludePlayer.HasValue)
            {
                // Computes WR holders (untied or not)
                var wrHolders = finalEntries
                    .Where(e => e.Rank == 1 && (
                        request.ExcludePlayer == RankingRequest.ExcludePlayerType.HasWorldRecord
                        || !finalEntries.Any(eBis =>
                            eBis.Rank == 1
                            && eBis.PlayerId != e.PlayerId
                            && eBis.Stage == e.Stage
                            && eBis.Level == e.Level)))
                    .Select(e => e.PlayerId)
                    .ToList();

                // Remove WR holders from players list
                request.Players = request.Players
                    .Where(p => !wrHolders.Contains(p.Key))
                    .ToDictionary(p => p.Key, p => p.Value);

                // Gets ranking without WR holders
                finalEntries = await GetFullGameRankingAsync(request)
                    .ConfigureAwait(false);
            }

            var rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => request.FullDetails
                    ? new RankingEntry(request.Game, request.Players[e.Key])
                    : new RankingEntryLight(request.Game, request.Players[e.Key]))
                .ToList();

            foreach (var entryGroup in LoopByStageAndLevel(finalEntries))
            {
                foreach (var timesGroup in entryGroup.Item3.GroupBy(l => l.Time).OrderBy(l => l.Key))
                {
                    var rank = timesGroup.First().Rank;
                    bool isUntied = rank == 1 && timesGroup.Count() == 1;

                    foreach (var timeEntry in timesGroup)
                    {
                        rankingEntries
                            .Single(e => e.PlayerId == timeEntry.PlayerId)
                            .AddStageAndLevelDatas(timeEntry, isUntied);
                    }
                }
            }

            return rankingEntries
                .OrderByDescending(r => r.Points)
                .ToList()
                .WithRanks(r => r.Points);
        }

        // Gets the full game ranking entries
        private async Task<List<RankingDto>> GetFullGameRankingAsync(RankingRequest request)
        {
            var rankingEntries = new ConcurrentBag<RankingDto>();

            var tasks = new List<Task>();
            foreach (var stage in request.Game.GetStages())
            {
                tasks.Add(GetStageAllLevelRankingAsync(request, rankingEntries, stage));
            }

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            return rankingEntries.ToList();
        }

        // Gets the ranking entries for every level for a single stage
        private async Task GetStageAllLevelRankingAsync(
            RankingRequest request,
            ConcurrentBag<RankingDto> rankingEntries,
            Stage stage)
        {
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                if (!request.SkipStages.Contains(stage))
                {
                    var stageLevelRankings = await GetStageLevelRankingAsync(request, stage, level)
                        .ConfigureAwait(false);
                    foreach (var slr in stageLevelRankings)
                        rankingEntries.Add(slr);
                }
            }
        }

        // Gets ranking entries for a stage and level
        private async Task<List<RankingDto>> GetStageLevelRankingAsync(
            RankingRequest request,
            Stage stage,
            Level level)
        {
            var entries = await GetStageLevelEntriesAsync(request, stage, level)
                .ConfigureAwait(false);

            // Groups and sorts by date
            var entriesDateGroup = new SortedList<DateTime, List<EntryDto>>(
                entries
                    .GroupBy(e => e.Date.Value.Date)
                    .ToDictionary(
                        eGroup => eGroup.Key,
                        eGroup => eGroup.ToList()));

            var rankingsToInsert = new List<RankingDto>();

            // For the current date + previous days
            // Gets the min time entry for each player
            // Then orders by entry time overall (ascending)
            var selectedEntries = entriesDateGroup
                .Where(kvp => kvp.Key <= request.RankingDate)
                .SelectMany(kvp => kvp.Value)
                .GroupBy(e => e.PlayerId)
                .Select(eGroup => eGroup.First(e => e.Time == eGroup.Min(et => et.Time)))
                .OrderBy(e => e.Time)
                .ThenBy(e => e.Date.Value)
                .ToList();

            var pos = 1;
            var posAgg = 1;
            long? currentTime = null;
            foreach (var entry in selectedEntries)
            {
                if (!currentTime.HasValue)
                {
                    currentTime = entry.Time;
                }
                else if (currentTime == entry.Time)
                {
                    posAgg++;
                }
                else
                {
                    pos += posAgg;
                    posAgg = 1;
                    currentTime = entry.Time;
                }

                var ranking = new RankingDto
                {
                    Date = request.RankingDate,
                    Level = entry.Level,
                    PlayerId = entry.PlayerId,
                    Rank = pos,
                    Stage = entry.Stage,
                    Time = entry.Time,
                    EntryDate = entry.Date.Value,
                    IsSimulatedDate = entry.IsSimulatedDate
                };

                rankingsToInsert.Add(ranking);
            }

            return rankingsToInsert;
        }

        // Gets entries for a stage and level
        private async Task<List<EntryDto>> GetStageLevelEntriesAsync(
            RankingRequest request,
            Stage stage,
            Level level)
        {
            // Gets every entry for the stage and level
            var tmpEntriesSource = request.Entries.ContainsKey((stage, level))
                ? request.Entries[(stage, level)]
                : await _readRepository
                    .GetEntriesAsync(stage, level, null, null)
                    .ConfigureAwait(false);

            // Entries not related to players are excluded
            var entries = tmpEntriesSource
                .Where(e => request.Players.ContainsKey(e.PlayerId))
                .ToList();

            // Sets date for every entry
            ManageDateLessEntries(request.Game, request.Players, entries);

            if (!request.Entries.ContainsKey((stage, level)))
            {
                request.Entries.TryAdd((stage, level), new List<EntryDto>(entries));
            }

            if (request.Engine.HasValue)
            {
                entries.RemoveAll(_ => (_.Engine.HasValue && _.Engine.Value != request.Engine.Value)
                    || (!request.IncludeUnknownEngine && !_.Engine.HasValue));
            }

            if (request.RankingStartDate.HasValue)
            {
                entries.RemoveAll(_ => _.Date < request.RankingStartDate.Value);
            }

            if (request.PlayerVsLegacy.HasValue)
            {
                entries.RemoveAll(_ => _.Date > request.PlayerVsLegacy.Value.Item2
                    && _.PlayerId != request.PlayerVsLegacy.Value.Item1);
            }

            return entries;
        }

        #endregion Ranking private methods

        private async Task GetSingleStageGetLastTiedWrsAsync(
            ConcurrentDictionary<Stage, Dictionary<Level, (EntryDto, bool)>> daysByStage,
            DateTime date,
            Stage stage)
        {
            var stageDatas = new Dictionary<Level, (EntryDto, bool)>();
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var entries = await _readRepository.GetEntriesAsync(stage, level, null, date).ConfigureAwait(false);
                var datedEntries = entries.Where(_ => _.Date.HasValue);
                if (datedEntries.Any())
                {
                    var entriesAt = datedEntries.GroupBy(_ => _.Time).OrderBy(_ => _.Key).First();
                    var lastEntry = entriesAt.OrderByDescending(_ => _.Date).First();
                    stageDatas.Add(level, (lastEntry, entriesAt.Count() == 1));
                }
                else
                {
                    stageDatas.Add(level, (null, false));
                }
            }

            daysByStage.TryAdd(stage, stageDatas);
        }

        private static IEnumerable<(long, DateTime, Stage)> GetPotentialSweeps(
            bool untied,
            Dictionary<(Stage, Level), List<EntryDto>> entriesGroups,
            DateTime currentDate,
            Stage stage)
        {
            var tiedSweepPlayerIds = new List<long>();
            long? untiedSweepPlayerId = null;
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var stageLevelDateWrs = entriesGroups[(stage, level)]
                    .Where(e => e.Date.Value.Date <= currentDate.Date)
                    .GroupBy(e => e.Time)
                    .OrderBy(e => e.Key)
                    .FirstOrDefault();

                bool isPotentialSweep = untied
                    ? stageLevelDateWrs?.Count() == 1
                    : stageLevelDateWrs?.Count() > 0;

                if (isPotentialSweep)
                {
                    if (untied)
                    {
                        var currentPId = stageLevelDateWrs.First().PlayerId;
                        if (!untiedSweepPlayerId.HasValue)
                        {
                            untiedSweepPlayerId = currentPId;
                        }

                        isPotentialSweep = untiedSweepPlayerId.Value == currentPId;
                    }
                    else
                    {
                        tiedSweepPlayerIds = tiedSweepPlayerIds.IntersectOrConcat(stageLevelDateWrs.Select(_ => _.PlayerId));

                        isPotentialSweep = tiedSweepPlayerIds.Count > 0;
                    }
                }

                if (!isPotentialSweep)
                {
                    tiedSweepPlayerIds.Clear();
                    untiedSweepPlayerId = null;
                    break;
                }
            }

            if (!untied)
            {
                return tiedSweepPlayerIds.Select(_ => (_, currentDate, stage));
            }

            return untiedSweepPlayerId.HasValue
                ? (untiedSweepPlayerId.Value, currentDate.Date, stage).Yield()
                : Enumerable.Empty<(long, DateTime, Stage)>();
        }
    }
}
