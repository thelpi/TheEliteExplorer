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
        private readonly IReadRepository _readRepository;
        private readonly IWriteRepository _writeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Ranking configuration.</param>
        /// <param name="readRepository">Instance of <see cref="IReadRepository"/>.</param>
        /// <param name="writeRepository">Instance of <see cref="IWriteRepository"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> or inner value is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="readRepository"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="writeRepository"/> is <c>Null</c>.</exception>
        public RankingProvider(
            IOptions<RankingConfiguration> configuration,
            IReadRepository readRepository,
            IWriteRepository writeRepository)
        {
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntries(
            Game game,
            DateTime rankingDate,
            bool full)
        {
            rankingDate = rankingDate.Date;

            var players = await GetPlayers().ConfigureAwait(false);

            var finalEntries = new List<RankingDto>();
            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var stageLevelRankings = await _readRepository
                        .GetStageLevelDateRankings((long)stage, level, rankingDate)
                        .ConfigureAwait(false);
                    finalEntries.AddRange(stageLevelRankings);
                }
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
                        break;
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
                .ThenBy(r => r.CumuledTime)
                .ToList();
        }

        /// <inheritdoc />
        public async Task RebuildRankingHistory(
            Game game)
        {
            var players = await GetPlayers()
                .ConfigureAwait(false);

            var entries = await GetEntriesInternal(game, null, players)
                .ConfigureAwait(false);

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var filteredEntries = entries
                        .Where(e => e.StageId == (long)stage && e.LevelId == (long)level)
                        .ToList();

                    await RebuildRankingHistoryInternal(filteredEntries, players, stage, level)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task RebuildRankingHistory(
            Stage stage,
            Level level)
        {
            var players = await GetPlayers()
                .ConfigureAwait(false);

            var entries = await GetEntriesInternal(null, (stage, level), players)
                .ConfigureAwait(false);

            await RebuildRankingHistoryInternal(entries, players, stage, level)
                .ConfigureAwait(false);
        }

        // Gets entries according to parameters (full game, or one stage and level)
        private async Task<List<EntryDto>> GetEntriesInternal(
            Game? game,
            (Stage Stage, Level Level)? stageAndLevel,
            IDictionary<long, PlayerDto> players)
        {
            var entriesSource = new List<EntryDto>();

            if (stageAndLevel.HasValue)
            {
                // Gets every entry for the stage and level
                var tmpEntriesSource = await _readRepository
                    .GetEntries((long)stageAndLevel.Value.Stage, stageAndLevel.Value.Level, null, null)
                    .ConfigureAwait(false);

                entriesSource.AddRange(tmpEntriesSource);
            }
            else
            {
                // Gets every entry for the game
                foreach (var stage in game.Value.GetStages())
                {
                    var entriesStageSource = await _readRepository
                        .GetEntries((long)stage)
                        .ConfigureAwait(false);

                    entriesSource.AddRange(entriesStageSource);
                }
            }

            // Entries not related to players are excluded
            var entries = entriesSource.Where(e => players.ContainsKey(e.PlayerId)).ToList();

            // Sets time for every entry
            ManageDateLessEntries(game.Value, players, entries);

            return entries;
        }

        // Rebuilds ranking for a stage and a level
        private async Task RebuildRankingHistoryInternal(
            List<EntryDto> entries,
            IDictionary<long, PlayerDto> players,
            Stage stage,
            Level level)
        {
            // Removes previous ranking history
            await _writeRepository
                .DeleteStageLevelRankingHistory((long)stage, level)
                .ConfigureAwait(false);

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
                        LevelId = entry.LevelId,
                        PlayerId = entry.PlayerId,
                        Rank = pos,
                        StageId = entry.StageId,
                        Time = entry.Time
                    };

                    rankingsToInsert.Add(ranking);
                }
            }

            await _writeRepository
                .BulkInsertRankings(rankingsToInsert)
                .ConfigureAwait(false);
        }

        // Gets every player keyed by ID
        private async Task<IDictionary<long, PlayerDto>> GetPlayers()
        {
            // TODO: gets also dirty players
            var playersSource = await _readRepository
                .GetPlayers()
                .ConfigureAwait(false);
            
            return playersSource.ToDictionary(p => p.Id, p => p);
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
        }

        // Loops on every stages and levels of a list of entries
        private static IEnumerable<(long, long, IEnumerable<RankingDto>)> LoopByStageAndLevel(
            IEnumerable<RankingDto> rankings)
        {
            foreach (var group in rankings.GroupBy(r => new { r.StageId, r.LevelId }))
            {
                yield return (group.Key.StageId, group.Key.LevelId, group);
            }
        }
    }
}
