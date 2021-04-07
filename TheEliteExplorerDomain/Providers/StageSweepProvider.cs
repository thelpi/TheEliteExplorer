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

            var entries = await _sqlContext
                .GetEntriesAsync((long)game)
                .ConfigureAwait(false);

            var players = await _sqlContext
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var fullList = new List<(long playerId, DateTime date, Stage stage)>();

            var groupEntries = entries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.StageId, e.LevelId))
                .ToDictionary(e => e.Key, e => e.ToList());

            foreach (var currentDate in SystemExtensions.LoopBetweenDates(
                startDate ?? Extensions.GetEliteFirstDate(game),
                endDate ?? ServiceProviderAccessor.ClockProvider.Now,
                DateStep.Day))
            {
                foreach (var stage in Stage.Get(game))
                {
                    var playersWithWr = new List<long>();
                    long? pId = null;
                    bool isUntiedSweep = true;
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        var currentWr = groupEntries[(stage.Id, (int)level)]
                            .Where(e => e.Date.Value.Date <= currentDate.Date)
                            .GroupBy(e => e.Time)
                            .OrderBy(e => e.Key)
                            .FirstOrDefault();
                        if (untied)
                        {
                            if (currentWr?.Count() == 1)
                            {
                                var currentPId = currentWr.First().PlayerId;
                                if (!pId.HasValue)
                                {
                                    pId = currentPId;
                                }
                                else if (pId.Value != currentPId)
                                {
                                    isUntiedSweep = false;
                                    break;
                                }
                            }
                            else
                            {
                                isUntiedSweep = false;
                                break;
                            }
                        }
                        else
                        {
                            if (currentWr?.Count() > 0)
                            {
                                if (playersWithWr.Count == 0)
                                {
                                    playersWithWr.AddRange(currentWr.Select(_ => _.PlayerId));
                                }
                                else
                                {
                                    playersWithWr = playersWithWr.Intersect(currentWr.Select(_ => _.PlayerId)).ToList();
                                }
                                if (playersWithWr.Count == 0)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                playersWithWr.Clear();
                                break;
                            }
                        }
                    }
                    if (untied)
                    {
                        if (isUntiedSweep)
                        {
                            fullList.Add((pId.Value, currentDate.Date, stage));
                        }
                    }
                    else
                    {
                        fullList.AddRange(playersWithWr.Select(_ => (_, currentDate, stage)));
                    }
                }
            }

            fullList = fullList.OrderBy(f => f.date).ToList();

            var refinedList = new List<StageSweep>();

            foreach (var (playerId, date, stage) in fullList)
            {
                var yesterdayEntry = refinedList.FirstOrDefault(e =>
                    e.PlayerId == playerId
                    && e.Stage == stage
                    && e.EndDate == date.AddDays(-1));
                if (yesterdayEntry == null)
                {
                    refinedList.Add(new StageSweep
                    {
                        EndDate = date,
                        StartDate = date,
                        Player = new Player(players.Single(p => p.Id == playerId)),
                        PlayerId = playerId,
                        Stage = stage
                    });
                }
                else
                {
                    yesterdayEntry.EndDate = yesterdayEntry.EndDate.AddDays(1);
                }
            }

            return refinedList.Select(e =>
            {
                e.EndDate = e.EndDate.AddDays(1);
                return e;
            }).ToList();
        }
    }
}
