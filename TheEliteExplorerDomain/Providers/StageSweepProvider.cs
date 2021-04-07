using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Providers
{
    /// <summary>
    /// Stage sweep provider implementation.
    /// </summary>
    /// <seealso cref="IStageSweepProvider"/>
    public sealed class StageSweepProvider : IStageSweepProvider
    {
        private readonly ISqlContext _sqlContext;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="sqlContext">SQL context.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        public StageSweepProvider(ISqlContext sqlContext)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageSweep>> GetSweepsAsync(Game game, bool untied, DateTime? startDate, DateTime? endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentOutOfRangeException(nameof(startDate), startDate,
                    $"{nameof(startDate)} is greater than {nameof(endDate)}.");
            }

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
            var entries = await _sqlContext
                            .GetEntriesAsync((long)game)
                            .ConfigureAwait(false);

            var groupEntries = entries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.StageId, e.LevelId))
                .ToDictionary(e => e.Key, e => e.ToList());
            return groupEntries;
        }

        private async Task<Dictionary<long, Dtos.PlayerDto>> GetPlayersDictionary()
        {
            var players = await _sqlContext
                            .GetPlayersAsync()
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
