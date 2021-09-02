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
    /// Stage statistics provider.
    /// </summary>
    /// <seealso cref="IStageStatisticsProvider"/>
    public sealed class StageStatisticsProvider : IStageStatisticsProvider
    {
        private readonly IReadRepository _readRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="readRepository">Instance of <see cref="IReadRepository"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="readRepository"/> is <c>Null</c>.</exception>
        public StageStatisticsProvider(IReadRepository readRepository)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<StageEntryCount>> GetStagesEntriesCount(
            Game game,
            DateTime startDate,
            DateTime endDate,
            bool levelDetails)
        {
            var entries = new List<EntryDto>();

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var datas = await _readRepository
                        .GetEntries(stage, level, startDate, endDate)
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
                        TotalEntriesCount = await _readRepository.GetEntriesCount(stage, level).ConfigureAwait(false)
                    });
                }
            }

            return results;
        }
    }
}
