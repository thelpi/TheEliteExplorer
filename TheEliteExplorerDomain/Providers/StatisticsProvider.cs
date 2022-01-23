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
    /// Stage statistics provider.
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
        public StatisticsProvider(IReadRepository readRepository,
            IOptions<RankingConfiguration> configuration)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

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
        public async Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            Game game,
            DateTime rankingDate,
            bool full,
            long? simulatedPlayerId = null,
            int? monthsOfFreshTimes = null,
            Stage[] skipStages = null,
            bool? excludeWinners = false)
        {
            var players = await GetPlayersAsync().ConfigureAwait(false);

            return await GetRankingEntriesPrivateAsync(players, game, rankingDate, full, simulatedPlayerId, monthsOfFreshTimes, skipStages, excludeWinners).ConfigureAwait(false);
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

            // TODO: use world records table
            var entriesGroups = await GetEntriesGroupByStageAndLevelAsync(game).ConfigureAwait(false);

            var playerKeys = await GetPlayersDictionaryAsync().ConfigureAwait(false);

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
                            var sweeps = GetPotentialSweep(untied, entriesGroups, currentDate, stg);
                            foreach (var sw in sweeps)
                                sweepsRaw.Add(sw);
                        }
                    }
                }
            });

            return ConsolidateSweeps(playerKeys, sweepsRaw);
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

        private async Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesPrivateAsync(
            IDictionary<long, PlayerDto> players,
            Game game,
            DateTime rankingDate,
            bool full,
            long? simulatedPlayerId = null,
            int? monthsOfFreshTimes = null,
            Stage[] skipStages = null,
            bool? excludeWinners = false,
            Dictionary<(Stage, Level), List<EntryDto>> cache = null)
        {
            rankingDate = rankingDate.Date;

            var finalEntries = new List<RankingDto>();
            foreach (var stage in game.GetStages())
            {
                if (skipStages?.Contains(stage) == true) continue;

                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var stageLevelRankings = await RebuildRankingHistoryInternalAsync(players, stage, level, cache,
                            new Tuple<long?, DateTime, int?>(simulatedPlayerId, rankingDate, monthsOfFreshTimes))
                        .ConfigureAwait(false);
                    finalEntries.AddRange(stageLevelRankings);
                }
            }

            if (excludeWinners != false)
            {
                var wrHolders = finalEntries
                    .Where(e => e.Rank == 1 && (excludeWinners == true || !finalEntries.Any(eBis =>
                        eBis.Rank == 1 && eBis.PlayerId != e.PlayerId && eBis.Stage == e.Stage && eBis.Level == e.Level)))
                    .Select(e => e.PlayerId)
                    .ToList();

                players = players.Where(p => !wrHolders.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);

                finalEntries = new List<RankingDto>();
                foreach (var stage in game.GetStages())
                {
                    if (skipStages?.Contains(stage) == true) continue;

                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        var stageLevelRankings = await RebuildRankingHistoryInternalAsync(players, stage, level, cache,
                                new Tuple<long?, DateTime, int?>(simulatedPlayerId, rankingDate, monthsOfFreshTimes))
                            .ConfigureAwait(false);
                        finalEntries.AddRange(stageLevelRankings);
                    }
                }
                //finalEntries.RemoveAll(e => wrHolders.Contains(e.PlayerId));
            }

            var rankingEntries = finalEntries
                .GroupBy(e => e.PlayerId)
                .Select(e => full ? new RankingEntry(game, players[e.Key]) : new RankingEntryLight(game, players[e.Key]))
                .ToList();

            foreach (var entryGroup in LoopByStageAndLevel(finalEntries))
            {
                foreach (var timesGroup in entryGroup.Item3.GroupBy(l => l.Time).OrderBy(l => l.Key))
                {
                    var rank = timesGroup.First().Rank;
                    if (rank > 100)
                    {
                        //break;
                    }
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

        // internal logic or ranking building (simulated or not)
        private async Task<List<RankingDto>> RebuildRankingHistoryInternalAsync(
            IDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            Dictionary<(Stage, Level), List<EntryDto>> cache,
            Tuple<long?, DateTime, int?> playerAtSpecificDate = null)
        {
            players = players ?? await GetPlayersAsync()
                .ConfigureAwait(false);

            var entries = await GetEntriesInternalAsync(null, (stage, level), cache, players, playerAtSpecificDate).ConfigureAwait(false);

            return RebuildRankingHistoryInternalAsync(entries, players, stage, level,
                playerAtSpecificDate == null
                    ? (DateTime?)null
                    : playerAtSpecificDate.Item2);
        }

        // Gets entries according to parameters (full game, or one stage and level)
        private async Task<List<EntryDto>> GetEntriesInternalAsync(
            Game? game,
            (Stage Stage, Level Level)? stageAndLevel,
            Dictionary<(Stage Stage, Level Level), List<EntryDto>> cache,
            IDictionary<long, PlayerDto> players,
            Tuple<long?, DateTime, int?> playerAtSpecificDate = null)
        {
            var entriesSource = new List<EntryDto>();

            if (stageAndLevel.HasValue)
            {
                game = stageAndLevel.Value.Stage.GetGame();

                // Gets every entry for the stage and level
                var tmpEntriesSource = cache != null && cache.ContainsKey(stageAndLevel.Value)
                    ? cache[stageAndLevel.Value]
                    : await _readRepository
                        .GetEntriesAsync(stageAndLevel.Value.Stage, stageAndLevel.Value.Level, null, null)
                        .ConfigureAwait(false);

                entriesSource.AddRange(tmpEntriesSource);
            }
            else
            {
                // Gets every entry for the game
                foreach (var stage in game.Value.GetStages())
                {
                    var entriesStageSource = await _readRepository
                        .GetEntriesAsync(stage)
                        .ConfigureAwait(false);

                    entriesSource.AddRange(entriesStageSource);
                }
            }

            // Entries not related to players are excluded
            var entries = entriesSource.Where(e => players.ContainsKey(e.PlayerId)).ToList();

            // Sets date for every entry
            ManageDateLessEntries(game.Value, players, entries);

            if (stageAndLevel.HasValue && cache != null && !cache.ContainsKey(stageAndLevel.Value))
            {
                cache.Add(stageAndLevel.Value, new List<EntryDto>(entries));
            }

            if (playerAtSpecificDate != null)
            {
                var monthsMin = playerAtSpecificDate.Item3;
                var dateRank = playerAtSpecificDate.Item2;
                var playerSim = playerAtSpecificDate.Item1;

                if (monthsMin.HasValue)
                {
                    entries.RemoveAll(_ => _.Date < dateRank.AddMonths(-monthsMin.Value));
                }

                entries.RemoveAll(_ => _.Date > dateRank && (!playerSim.HasValue || _.PlayerId != playerSim));
            }

            return entries;
        }

        // Rebuilds ranking for a stage and a level
        private List<RankingDto> RebuildRankingHistoryInternalAsync(
            List<EntryDto> entries,
            IDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            DateTime? oneShotAtDate = null)
        {
            // Groups and sorts by date
            var entriesDateGroup = new SortedList<DateTime, List<EntryDto>>(
                entries
                    .GroupBy(e => e.Date.Value.Date)
                    .ToDictionary(
                        eGroup => eGroup.Key,
                        eGroup => eGroup.ToList()));

            // Ranking is generated every day
            // if the current day of the loop has at least one new time
            var eligiblesDates = stage.GetGame().GetEliteFirstDate()
                .LoopBetweenDates(DateStep.Day)
                .Where(d => entriesDateGroup.ContainsKey(d))
                .ToList();

            // if we have only one date to build
            // it's the last from the base list
            if (oneShotAtDate.HasValue)
            {
                eligiblesDates = eligiblesDates.Skip(eligiblesDates.Count - 1).ToList();
            }

            var rankingsToInsert = new List<RankingDto>();

            foreach (var rankingDate in eligiblesDates)
            {
                // For the current date + previous days
                // Gets the min time entry for each player
                // Then orders by entry time overall (ascending)
                var selectedEntries = entriesDateGroup
                    .Where(kvp => kvp.Key <= rankingDate)
                    .SelectMany(kvp => kvp.Value)
                    .GroupBy(e => e.PlayerId)
                    .Select(eGroup => eGroup.First(e => e.Time == eGroup.Min(et => et.Time)))
                    .OrderBy(e => e.Time)
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
                        Date = rankingDate,
                        Level = entry.Level,
                        PlayerId = entry.PlayerId,
                        Rank = pos,
                        Stage = entry.Stage,
                        Time = entry.Time,
                        EntryDate = entry.Date
                    };

                    rankingsToInsert.Add(ranking);
                }
            }

            return rankingsToInsert;
        }

        // Gets every player keyed by ID
        private async Task<IDictionary<long, PlayerDto>> GetPlayersAsync()
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
            IDictionary<long, PlayerDto> players,
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
                        var dateMin = players[entry.PlayerId].JoinDate ?? game.GetEliteFirstDate();
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

        private static void StopStanding(
            Dictionary<long, PlayerDto> players,
            List<StageWrStanding> wrs,
            WrDto wrDto)
        {
            var currentWr = wrs.LastOrDefault(wr =>
                wr.Stage == wrDto.Stage
                && wr.Level == wrDto.Level
                && wr.StillWr);

            if (currentWr != null)
            {
                currentWr.StopStanding(wrDto, players);
            }
        }

        private static IEnumerable<(long, DateTime, Stage)> GetPotentialSweep(
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

        private async Task<Dictionary<(Stage Stage, Level Level), List<EntryDto>>> GetEntriesGroupByStageAndLevelAsync(Game game)
        {
            var fullEntries = new List<EntryDto>();

            foreach (var stage in game.GetStages())
            {
                var entries = await _readRepository
                    .GetEntriesAsync(stage)
                    .ConfigureAwait(false);

                fullEntries.AddRange(entries);
            }

            var groupEntries = fullEntries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.Stage, e.Level))
                .ToDictionary(e => e.Key, e => e.ToList());
            return groupEntries;
        }

        private async Task<Dictionary<long, PlayerDto>> GetPlayersDictionaryAsync()
        {
            var players = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);
            var dirtyPlayers = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            return players.Concat(dirtyPlayers).ToDictionary(p => p.Id, p => p);
        }

        private static IReadOnlyCollection<StageSweep> ConsolidateSweeps(
            Dictionary<long, PlayerDto> playerKeys,
            ConcurrentBag<(long playerId, DateTime date, Stage stage)> sweepsRaw)
        {
            var sweeps = new List<StageSweep>();

            foreach (var (playerId, date, stage) in sweepsRaw.OrderBy(f => f.date))
            {
                var sweepMatch = sweeps.SingleOrDefault(s =>
                    s.PlayerId == playerId
                    && s.Stage == stage
                    && s.EndDate == date);

                if (sweepMatch == null)
                {
                    sweepMatch = new StageSweep(date, stage, playerKeys[playerId]);
                    sweeps.Add(sweepMatch);
                }

                sweepMatch.AddDay();
            }

            return sweeps;
        }
    }
}
