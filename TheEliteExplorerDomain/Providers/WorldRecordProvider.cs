using System;
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
        private readonly IWriteRepository _writeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="readRepository">Instance of <see cref="IReadRepository"/>.</param>
        /// <param name="writeRepository">Instance of <see cref="IWriteRepository"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="readRepository"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="writeRepository"/> is <c>Null</c>.</exception>
        public WorldRecordProvider(
            IReadRepository readRepository,
            IWriteRepository writeRepository)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
        }

        /// <inheritdoc />
        public async Task GenerateWorldRecords(Game game)
        {
            foreach (var stage in Stage.Get(game))
            {
                var entries = await _readRepository
                    .GetEntries(stage.Id)
                    .ConfigureAwait(false);

                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    await _writeRepository
                        .DeleteStageLevelWr(stage.Id, level)
                        .ConfigureAwait(false);

                    var entriesDone = new List<(long, long)>();

                    var levelEntries = entries
                        .Where(e => e.LevelId == (long)level && e.Date.HasValue)
                        .GroupBy(e => e.Date.Value)
                        .OrderBy(e => e.Key)
                        .ToDictionary(e => e.Key, e => e.ToList());

                    long? time = null;
                    var currentlyUntied = false;
                    foreach (var date in levelEntries.Keys)
                    {
                        // TODO: manage several dates in one day
                        var bestTimesAtDate = levelEntries[date]
                            .GroupBy(e => e.Time)
                            .OrderBy(e => e.Key)
                            .First();

                        if (!time.HasValue || time > bestTimesAtDate.Key)
                        {
                            time = bestTimesAtDate.Key;

                            currentlyUntied = await AddEntriesAsWorldRecords(
                                    bestTimesAtDate,
                                    stage.Id,
                                    level,
                                    true,
                                    false,
                                    entriesDone)
                                .ConfigureAwait(false);
                        }
                        else if (time == bestTimesAtDate.Key)
                        {
                            await AddEntriesAsWorldRecords(
                                    bestTimesAtDate,
                                    stage.Id,
                                    level,
                                    false,
                                    currentlyUntied,
                                    entriesDone)
                                .ConfigureAwait(false);
                            currentlyUntied = false;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageSweep>> GetSweeps(
            Game game,
            bool untied,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentOutOfRangeException(nameof(startDate), startDate,
                    $"{nameof(startDate)} is greater than {nameof(endDate)}.");
            }

            // TODO: use world records table
            var entriesGroups = await GetEntriesGroupByStageAndLevel(game).ConfigureAwait(false);

            var playerKeys = await GetPlayersDictionary().ConfigureAwait(false);

            var sweepsRaw = new List<(long playerId, DateTime date, Stage stage)>();

            foreach (var currentDate in SystemExtensions.LoopBetweenDates(
                startDate ?? Extensions.GetEliteFirstDate(game),
                endDate ?? ServiceProviderAccessor.ClockProvider.Now,
                DateStep.Day))
            {
                foreach (var stage in Stage.Get(game))
                {
                    var sweeps = GetPotentialSweep(untied, entriesGroups, currentDate, stage);
                    sweepsRaw.AddRange(sweeps);
                }
            }

            return ConsolidateSweeps(playerKeys, sweepsRaw);
        }

        private async Task<bool> AddEntriesAsWorldRecords(
            IEnumerable<EntryDto> bestTimesAtDate,
            long stageId,
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
                    StageId = stageId,
                    Date = times.Date.Value,
                    FirstTied = firstTied,
                    LevelId = (long)level,
                    PlayerId = times.PlayerId,
                    Time = times.Time,
                    Untied = untied
                };

                await _writeRepository
                    .InsertWr(dto)
                    .ConfigureAwait(false);
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
            Dictionary<(long, long), List<Dtos.EntryDto>> entriesGroups,
            DateTime currentDate,
            Stage stage)
        {
            var tiedSweepPlayerIds = new List<long>();
            long? untiedSweepPlayerId = null;
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var stageLevelDateWrs = entriesGroups[(stage.Id, (int)level)]
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

        private async Task<Dictionary<(long StageId, long LevelId), List<Dtos.EntryDto>>> GetEntriesGroupByStageAndLevel(Game game)
        {
            var fullEntries = new List<Dtos.EntryDto>();

            foreach (var stage in Stage.Get(game))
            {
                var entries = await _readRepository
                    .GetEntries(stage.Id)
                    .ConfigureAwait(false);

                fullEntries.AddRange(entries);
            }

            var groupEntries = fullEntries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.StageId, e.LevelId))
                .ToDictionary(e => e.Key, e => e.ToList());
            return groupEntries;
        }

        private async Task<Dictionary<long, Dtos.PlayerDto>> GetPlayersDictionary()
        {
            var players = await _readRepository
                            .GetPlayers()
                            .ConfigureAwait(false);

            var playerKeys = players.ToDictionary(p => p.Id, p => p);
            return playerKeys;
        }

        private static IReadOnlyCollection<StageSweep> ConsolidateSweeps(
            Dictionary<long, Dtos.PlayerDto> playerKeys,
            List<(long playerId, DateTime date, Stage stage)> sweepsRaw)
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
