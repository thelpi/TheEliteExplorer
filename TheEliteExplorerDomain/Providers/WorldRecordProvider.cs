using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Providers
{
    /// <summary>
    /// World records provider.
    /// </summary>
    /// <seealso cref="IWorldRecordProvider"/>
    public sealed class WorldRecordProvider : IWorldRecordProvider
    {
        private readonly IReadRepository _readRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="readRepository">Instance of <see cref="IReadRepository"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="readRepository"/> is <c>Null</c>.</exception>
        public WorldRecordProvider(IReadRepository readRepository)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageSweep>> GetSweeps(
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
            var entriesGroups = await GetEntriesGroupByStageAndLevel(game).ConfigureAwait(false);

            var playerKeys = await GetPlayersDictionary().ConfigureAwait(false);

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
        public async Task<IReadOnlyCollection<StageWrStanding>> GetLongestStandingWrs(
            Game game,
            bool untied,
            bool stillStanding,
            DateTime? atDate)
        {
            var players = await GetPlayersDictionary().ConfigureAwait(false);

            var allWrDto = await GetAllWrs(game, atDate).ConfigureAwait(false);

            var wrs = GetEveryStageWrStanding(untied, players, allWrDto, atDate);

            return wrs
                .OrderByDescending(wr => wr.Days)
                .Where(wr => !stillStanding || wr.StillWr)
                .ToList()
                .WithRanks(r => r.Days);
        }

        /// <inheritdoc />
        public async Task<StageAllTimeLeaderboard> GetStageAllTimeLeaderboard(
            Stage stage,
            int limit)
        {
            var players = await GetPlayersDictionary()
                .ConfigureAwait(false);

            var leaderboard = new StageAllTimeLeaderboard(limit);

            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var wrs = await _readRepository
                    .GetStageLevelWrs(stage, level)
                    .ConfigureAwait(false);

                wrs = wrs.OrderBy(wr => wr.Date).ThenByDescending(wr => wr.Untied).ToList();

                foreach (var wr in wrs)
                {
                    leaderboard.SetEntry(wr, players);
                }

                leaderboard.SetTodayForEntries();
            }

            return leaderboard;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageWrStanding>> GetCurrentLongestStandingWrsHistory(Game game, bool untied)
        {
            var players = await GetPlayersDictionary().ConfigureAwait(false);

            var allWrDto = await GetAllWrs(game, null).ConfigureAwait(false);

            var wrs = GetEveryStageWrStanding(untied, players, allWrDto, null)
                .OrderBy(wr => wr.StartDate)
                .ToList();

            var finalStandingList = new List<StageWrStanding>();
            var finalStandingListDateWrs = new List<DateTime>();

            // gets every wr the first day
            var firstDayWrs = wrs
                .Where(wr => wr.StartDate == wrs.First().StartDate)
                .ToList();

            // gets the best wr of the first day
            var dayWrPick = firstDayWrs.First(wr => wr.Days == firstDayWrs.Max(wrd => wrd.Days));
            var dayWrPickDate = dayWrPick.StartDate;
            finalStandingList.Add(dayWrPick);

            while (dayWrPick.EndDate.HasValue)
            {
                // gets wr that started before (or the same day) the current standing is over
                // and that finished after the current standing (or still active)
                var wrsAtDate = wrs
                    .Where(wr =>
                        wr.StartDate > dayWrPickDate
                        && wr.StartDate <= dayWrPick.EndDate
                        && (wr.EndDate > dayWrPick.EndDate || !wr.EndDate.HasValue))
                    .ToList();

                if (wrsAtDate.Count > 0)
                {
                    // for those wr, gets the longest
                    var bestWrAtDate = wrsAtDate.FirstOrDefault(wr => wr.Days == wrsAtDate.Max(wrd => wrd.Days));
                    finalStandingListDateWrs.Add(dayWrPick.EndDate.Value);
                    finalStandingList.Add(bestWrAtDate);
                    dayWrPick = bestWrAtDate;
                    dayWrPickDate = dayWrPick.StartDate;
                }
                else
                {
                    // this weird case may happen for untieds
                    // when there are no untied at all at some point
                    dayWrPickDate.AddDays(1);
                }
            }

            // sets the date when the wr has started as standing
            for (int i = 0; i < finalStandingListDateWrs.Count; i++)
            {
                finalStandingList[i].StandingStartDate = finalStandingListDateWrs[i];
            }

            return finalStandingList;
        }

        /// <inheritdoc />
        public async Task<Dictionary<Stage, Dictionary<Level, (EntryDto, bool)>>> GetLastTiedWrs(Game game, DateTime date)
        {
            var daysByStage = new ConcurrentDictionary<Stage, Dictionary<Level, (EntryDto, bool)>>();

            var tasks = new List<Task>();
            foreach (var stage in game.GetStages())
                tasks.Add(GetSingleStageGetLastTiedWrs(daysByStage, date, stage));

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            return daysByStage.ToDictionary(_ => _.Key, _ => _.Value);
        }

        private async Task GetSingleStageGetLastTiedWrs(
            ConcurrentDictionary<Stage, Dictionary<Level, (EntryDto, bool)>> daysByStage,
            DateTime date,
            Stage stage)
        {
            var stageDatas = new Dictionary<Level, (EntryDto, bool)>();
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var entries = await _readRepository.GetEntries(stage, level, null, date).ConfigureAwait(false);
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

        private static List<StageWrStanding> GetEveryStageWrStanding(
            bool untied,
            Dictionary<long, PlayerDto> players,
            List<WrDto> allWrDto,
            DateTime? atDate)
        {
            var wrs = new List<StageWrStanding>();

            foreach (var wrDto in allWrDto)
            {
                if (wrDto.Untied)
                {
                    StopStanding(players, wrs, wrDto);
                    wrs.Add(new StageWrStanding(wrDto, players, atDate));
                }
                else if (untied)
                {
                    StopStanding(players, wrs, wrDto);
                }
            }

            return wrs;
        }

        private static void StopStanding(Dictionary<long, PlayerDto> players, List<StageWrStanding> wrs, WrDto wrDto)
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

        private async Task<List<WrDto>> GetAllWrs(Game game, DateTime? atDate)
        {
            var wrs = new List<WrDto>();

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var stageLevelWrs = await _readRepository
                        .GetStageLevelWrs(stage, level)
                        .ConfigureAwait(false);
                    wrs.AddRange(stageLevelWrs);
                }
            }

            return wrs
                .Where(wr => !atDate.HasValue || wr.Date <= atDate)
                .OrderBy(wr => wr.Date)
                .ThenByDescending(wr => wr.Untied)
                .ToList();
        }

        private bool AddEntriesAsWorldRecords(
            IEnumerable<EntryDto> bestTimesAtDate,
            Stage stage,
            Level level,
            bool untied,
            bool firstTied,
            List<(long, long)> entriesDone)
        {
            foreach (var times in bestTimesAtDate)
            {
                // Avoid duplicates on several engines
                if (entriesDone.Contains((times.PlayerId, times.Time)))
                {
                    continue;
                }
                entriesDone.Add((times.PlayerId, times.Time));

                var dto = new WrDto
                {
                    Stage = stage,
                    Date = times.Date.Value,
                    FirstTied = firstTied,
                    Level = level,
                    PlayerId = times.PlayerId,
                    Time = times.Time,
                    Untied = untied
                };

                if (untied)
                {
                    untied = false;
                    firstTied = true;
                }
                else if (firstTied)
                {
                    firstTied = false;
                }
            }

            return firstTied;
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

        private async Task<Dictionary<(Stage Stage, Level Level), List<EntryDto>>> GetEntriesGroupByStageAndLevel(Game game)
        {
            var fullEntries = new List<EntryDto>();

            foreach (var stage in game.GetStages())
            {
                var entries = await _readRepository
                    .GetEntries(stage)
                    .ConfigureAwait(false);

                fullEntries.AddRange(entries);
            }

            var groupEntries = fullEntries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.Stage, e.Level))
                .ToDictionary(e => e.Key, e => e.ToList());
            return groupEntries;
        }

        private async Task<Dictionary<long, PlayerDto>> GetPlayersDictionary()
        {
            var players = await _readRepository
                .GetPlayers()
                .ConfigureAwait(false);
            var dirtyPlayers = await _readRepository
                .GetDirtyPlayers()
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

        public async Task<IReadOnlyCollection<DateCountWr>> GetDateCountWrs(Game game)
        {
            var result = new List<DateCountWr>();

            var players = await GetPlayersDictionary().ConfigureAwait(false);

            var entriesGroups = await GetEntriesGroupByStageAndLevel(game).ConfigureAwait(false);

            foreach (var currentDate in SystemExtensions.LoopBetweenDates(
                Extensions.GetEliteFirstDate(game),
                ServiceProviderAccessor.ClockProvider.Now,
                DateStep.Day))
            {
                if (currentDate.DayOfWeek != DayOfWeek.Sunday)
                    continue;

                var currentResult = new DateCountWr
                {
                    Date = currentDate,
                    TiedPlayers = new List<long>(),
                    UntiedPlayers = new List<long>()
                };
                foreach (var stage in game.GetStages())
                {
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        var wrs = entriesGroups[(stage, level)]
                            .Where(e => e.Date <= currentDate)
                            .GroupBy(e => e.Time)
                            .OrderBy(e => e.Key)
                            .FirstOrDefault()?
                            .ToList()
                            ?? new List<EntryDto>();

                        var untied = wrs.Count == 1;
                        if (untied)
                        {
                            currentResult.UntiedsCount++;
                            currentResult.UntiedPlayers.AddRange(wrs.Select(e => e.PlayerId).Where(pId => !currentResult.UntiedPlayers.Contains(pId)));
                        }

                        currentResult.TiedsCount += wrs.Count;
                        currentResult.TiedPlayers.AddRange(wrs.Select(e => e.PlayerId).Where(pId => !currentResult.TiedPlayers.Contains(pId)));
                    }
                }
                result.Add(currentResult);
            }

            return result;
        }
    }
}
