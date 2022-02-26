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
        public async Task<IReadOnlyCollection<RankingEntryLight>> GetRankingEntriesAsync(
            RankingRequest request)
        {
            request.Players = await GetPlayersInternalAsync().ConfigureAwait(false);

            return await GetFullGameConsolidatedRankingAsync(request)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Sweep>> GetSweepsAsync(
            Game game,
            bool untied,
            DateTime? endDate)
        {
            var sweeps = new ConcurrentBag<Sweep>();

            var wrs = await GetWorldRecordsAsync(game, endDate).ConfigureAwait(false);

            //var tasks = new List<Task>();

            foreach (var stage in game.GetStages())
            {
                var locWrs = wrs.Where(wr => wr.Stage == stage);

                // Makes a list of every date with entries, ordered ascending
                var dates = locWrs
                    .SelectMany(wr => wr.Holders.Select(_ => _.Item2))
                    .Distinct()
                    .OrderBy(d => d);

                Sweep currentSweep = null;
                var currentWrs = new List<Wr> { null, null, null };
                foreach (var date in dates)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        // all untied of the day
                        var untieds = locWrs
                            .Where(wr => wr.Level == (Level)(i + 1) && wr.Date == date)
                            .OrderByDescending(wr => wr.Time);

                        if (untieds.Any())
                        {
                            currentWrs[i] = untieds.Last();
                        }

                        // all not untied of the day
                        var notUntieds = locWrs
                            .Where(wr => wr.Level == (Level)(i + 1) && wr.Holders.Skip(1).Any(_ => _.Item2 == date))
                            .OrderByDescending(wr => wr.Time);

                        if (currentWrs[i] != null && notUntieds.Any(_ => _.Time <= currentWrs[i].Time))
                        {
                            currentWrs[i] = null;
                        }
                    }

                    var isFullUntiedOnePlayer = currentWrs.All(_ => _ != null)
                        && currentWrs.GroupBy(_ => _.Player.Id).Count() == 1;

                    if (currentSweep != null)
                    {
                        if (!isFullUntiedOnePlayer
                            || currentWrs.First().Player.Id != currentSweep.Author.Id)
                        {
                            currentSweep.EndDate = date;
                            currentSweep = null;
                        }
                    }

                    if (currentSweep == null)
                    {
                        if (isFullUntiedOnePlayer)
                        {
                            currentSweep = new Sweep
                            {
                                StartDate = date,
                                Author = currentWrs.First().Player,
                                Stage = stage
                            };
                            sweeps.Add(currentSweep);
                        }
                    }
                }
                /*tasks.Add(Task.Run(() =>
                {
                    
                }));*/
            }

            //await Task.WhenAll(tasks).ConfigureAwait(false);

            var now = ServiceProviderAccessor.ClockProvider.Now;

            return sweeps
                .OrderByDescending(x => x.WithDays(now).Days)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<WrBase>> GetAmbiguousWorldRecordsAsync(
            Game game,
            bool untiedSlayAmbiguous)
        {
            var syncWr = new List<WrBase>();

            var wrs = await GetWorldRecordsAsync(game, null).ConfigureAwait(false);
            foreach (var wr in wrs)
            {
                if ((untiedSlayAmbiguous && wr.CheckAmbiguousHolders(1))
                    || (!untiedSlayAmbiguous && wr.CheckAmbiguousHolders(2)))
                {
                    syncWr.Add(wr.ToBase());
                }
            }

            return syncWr;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Standing>> GetLongestStandingsAsync(
            Game game,
            DateTime? endDate,
            StandingType standingType,
            bool? stillOngoing)
        {
            var standings = new List<Standing>();

            var wrs = await GetWorldRecordsAsync(game, endDate).ConfigureAwait(false);

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    var locWrs = wrs
                        .Where(wr => wr.Stage == stage && wr.Level == level)
                        .OrderBy(wr => wr.Date)
                        .ThenByDescending(wr => wr.Time);

                    Standing currentStanding = null;
                    foreach (var locWr in locWrs)
                    {
                        switch (standingType)
                        {
                            case StandingType.Unslayed:
                                standings.AddRange(locWr.Holders.Select(_ => new Standing(locWr.Time)
                                {
                                    Slayer = locWr.SlayPlayer,
                                    EndDate = locWr.SlayDate,
                                    StartDate = _.Item2,
                                    Author = _.Item1,
                                    Level = level,
                                    Stage = stage
                                }));
                                break;
                            case StandingType.UnslayedExceptSelf:
                                var slayer = locWr.SlayPlayer;
                                var holders = locWr.Holders.ToList();
                                if (currentStanding != null)
                                {
                                    currentStanding.AddTime(locWr.Time);
                                    holders.RemoveAll(_ => _.Item1.Id == currentStanding.Author.Id);
                                    if (slayer == null || slayer.Id != currentStanding.Author.Id)
                                    {
                                        currentStanding.Slayer = slayer;
                                        currentStanding.EndDate = locWr.SlayDate;
                                        currentStanding = null;
                                    }
                                }
                                foreach (var holder in holders)
                                {
                                    var locCurrentStanding = new Standing(locWr.Time)
                                    {
                                        StartDate = holder.Item2,
                                        Author = holder.Item1,
                                        Level = level,
                                        Stage = stage
                                    };
                                    standings.Add(locCurrentStanding);

                                    if (slayer == null || slayer.Id != locCurrentStanding.Author.Id)
                                    {
                                        locCurrentStanding.Slayer = slayer;
                                        locCurrentStanding.EndDate = locWr.SlayDate;
                                    }
                                    else
                                    {
                                        currentStanding = locCurrentStanding;
                                    }
                                }
                                break;
                            case StandingType.Untied:
                                standings.Add(new Standing(locWr.Time)
                                {
                                    Slayer = locWr.UntiedSlayPlayer ?? locWr.SlayPlayer,
                                    EndDate = locWr.UntiedSlayDate ?? locWr.SlayDate,
                                    StartDate = locWr.Date,
                                    Author = locWr.Player,
                                    Level = level,
                                    Stage = stage
                                });
                                break;
                            case StandingType.UntiedExceptSelf:
                                if (currentStanding == null)
                                {
                                    currentStanding = new Standing(locWr.Time)
                                    {
                                        StartDate = locWr.Date,
                                        Author = locWr.Player,
                                        Level = level,
                                        Stage = stage
                                    };
                                    standings.Add(currentStanding);
                                }

                                currentStanding.AddTime(locWr.Time);

                                var untiedSlayer = locWr.UntiedSlayPlayer ?? locWr.SlayPlayer;
                                if (untiedSlayer == null || untiedSlayer.Id != currentStanding.Author.Id)
                                {
                                    currentStanding.Slayer = untiedSlayer;
                                    currentStanding.EndDate = locWr.UntiedSlayDate ?? locWr.SlayDate;
                                    currentStanding = null;
                                }
                                break;
                            case StandingType.BetweenTwoTimes:
                                for (var i = 0; i < locWr.Holders.Count; i++)
                                {
                                    var holder = locWr.Holders.ElementAt(i);
                                    var isLast = i == locWr.Holders.Count - 1;
                                    standings.Add(new Standing(locWr.Time)
                                    {
                                        Slayer = isLast
                                            ? locWr.SlayPlayer
                                            : locWr.Holders.ElementAt(i + 1).Item1,
                                        EndDate = isLast
                                            ? locWr.SlayDate
                                            : locWr.Holders.ElementAt(i + 1).Item2,
                                        StartDate = holder.Item2,
                                        Author = holder.Item1,
                                        Level = level,
                                        Stage = stage
                                    });
                                }
                                break;
                        }
                    }
                }
            }

            var now = ServiceProviderAccessor.ClockProvider.Now;

            return standings
                .Where(x => stillOngoing == true
                    ? !x.EndDate.HasValue
                    : (stillOngoing == false
                        ? x.EndDate.HasValue
                        : true))
                .OrderByDescending(x => x.WithDays(now).Days)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Player>> GetPlayersAsync()
        {
            var players = await GetPlayersInternalAsync().ConfigureAwait(false);

            return players
                .Select(p => new Player(p.Value))
                .ToList();
        }

        #endregion IStatisticsProvider implementation

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

            foreach (var entryGroup in finalEntries.GroupBy(r => new { r.Stage, r.Level }))
            {
                foreach (var timesGroup in entryGroup.GroupBy(l => l.Time).OrderBy(l => l.Key))
                {
                    var rank = timesGroup.First().Rank;
                    bool isUntied = rank == 1 && timesGroup.Count() == 1;

                    foreach (var timeEntry in timesGroup)
                    {
                        rankingEntries
                            .Single(e => e.Player.Id == timeEntry.PlayerId)
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
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        if (!request.SkipStages.Contains(stage))
                        {
                            var stageLevelRankings = await GetStageLevelRankingAsync(request, stage, level)
                                .ConfigureAwait(false);
                            rankingEntries.AddRange(stageLevelRankings);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return rankingEntries.ToList();
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
            var entries = await GetStageLevelEntriesCoreAsync(request.Players, stage, level, request.Entries)
                .ConfigureAwait(false);

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

        private async Task<List<EntryDto>> GetStageLevelEntriesCoreAsync(
            IReadOnlyDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            ConcurrentDictionary<(Stage, Level), IReadOnlyCollection<EntryDto>> entriesCache = null)
        {
            // Gets every entry for the stage and level
            var tmpEntriesSource = entriesCache?.ContainsKey((stage, level)) == true
                ? entriesCache[(stage, level)]
                : await _readRepository
                    .GetEntriesAsync(stage, level, null, null)
                    .ConfigureAwait(false);

            // Entries not related to players are excluded
            var entries = tmpEntriesSource
                .Where(e => players.ContainsKey(e.PlayerId))
                .ToList();

            // Sets date for every entry
            ManageDateLessEntries(stage.GetGame(), entries);

            if (entriesCache?.ContainsKey((stage, level)) == false)
            {
                entriesCache.TryAdd((stage, level), new List<EntryDto>(entries));
            }

            return entries;
        }

        #endregion Ranking private methods

        // Gets a adictionary of every player by identifier (including dirty, but not banned)
        private async Task<IReadOnlyDictionary<long, PlayerDto>> GetPlayersInternalAsync()
        {
            var playersSourceClean = await _readRepository
                .GetPlayersAsync()
                .ConfigureAwait(false);

            var playersSourceDirty = await _readRepository
                .GetDirtyPlayersAsync()
                .ConfigureAwait(false);

            return playersSourceClean.Concat(playersSourceDirty).ToDictionary(p => p.Id, p => p);
        }

        // Sets a fake date on entries without it
        private void ManageDateLessEntries(
            Game game,
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
        
        private IReadOnlyCollection<Wr> GetStageLevelWorldRecords(
            IReadOnlyCollection<EntryDto> allEntries,
            IReadOnlyDictionary<long, PlayerDto> players,
            Stage stage,
            Level level,
            DateTime? endDate)
        {
            endDate = (endDate ?? ServiceProviderAccessor.ClockProvider.Now).Date;

            var wrs = new List<Wr>();

            var entries = allEntries
                .Where(e => e.Date <= endDate)
                .GroupBy(e => e.Date.Value)
                .OrderBy(e => e.Key)
                .ToDictionary(eg => eg.Key, eg => eg.OrderByDescending(e => e.Time).ToList());

            var bestTime = entries[entries.Keys.First()].Select(_ => _.Time).Min();
            Wr currentWr = null;
            foreach (var entryDate in entries.Keys)
            {
                var betterOrEqualEntries = entries[entryDate].Where(e => e.Time <= bestTime);
                foreach (var entry in betterOrEqualEntries)
                {
                    var player = players[entry.PlayerId];

                    if (entry.Time == bestTime && currentWr != null)
                        currentWr.AddHolder(player, entryDate, entry.Engine);
                    else
                    {
                        currentWr?.AddSlayer(player, entryDate);

                        currentWr = new Wr(stage, level, entry.Time, player, entryDate, entry.Engine);
                        wrs.Add(currentWr);
                        bestTime = entry.Time;
                    }
                }
            }

            return wrs;
        }
        
        private async Task<IReadOnlyCollection<Wr>> GetWorldRecordsAsync(
            Game game,
            DateTime? endDate)
        {
            var wrs = new ConcurrentBag<Wr>();

            var players = await GetPlayersInternalAsync().ConfigureAwait(false);

            var tasks = new List<Task>();

            foreach (var stage in game.GetStages())
            {
                foreach (var level in SystemExtensions.Enumerate<Level>())
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var entries = await GetStageLevelEntriesCoreAsync(players, stage, level).ConfigureAwait(false);

                        wrs.AddRange(
                            GetStageLevelWorldRecords(entries, players, stage, level, endDate));
                    }));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return wrs;
        }
    }
}
