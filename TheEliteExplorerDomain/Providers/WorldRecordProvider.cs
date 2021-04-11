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
        private readonly ISqlContext _sqlContext;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        public WorldRecordProvider(ISqlContext sqlContext)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
        }

        /// <inheritdoc />
        public async Task GenerateWorldRecords(Game game)
        {
            foreach (var stage in Stage.Get(game))
            {
                var entries = await _sqlContext
                    .GetEntries(stage.Id)
                    .ConfigureAwait(false);

                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    await _sqlContext
                        .DeleteStageLevelWr(stage.Id, level)
                        .ConfigureAwait(false);

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
                            .First();

                        if (!time.HasValue || time > bestTimesAtDate.Key)
                        {
                            time = bestTimesAtDate.Key;

                            currentlyUntied = await AddEntriesAsWorldRecords(
                                    bestTimesAtDate,
                                    stage.Id,
                                    level,
                                    true,
                                    false)
                                .ConfigureAwait(false);
                        }
                        else if (time == bestTimesAtDate.Key)
                        {
                            await AddEntriesAsWorldRecords(
                                    bestTimesAtDate,
                                    stage.Id,
                                    level,
                                    false,
                                    currentlyUntied)
                                .ConfigureAwait(false);
                            currentlyUntied = false;
                        }
                    }
                }
            }
        }

        private async Task<bool> AddEntriesAsWorldRecords(
            IEnumerable<EntryDto> bestTimesAtDate,
            long stageId,
            Level level,
            bool untied,
            bool firstTied)
        {
            foreach (var times in bestTimesAtDate)
            {
                await _sqlContext
                    .InsertWr(stageId, level, times.PlayerId, times.Date.Value, times.Time, untied, firstTied)
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
    }
}
