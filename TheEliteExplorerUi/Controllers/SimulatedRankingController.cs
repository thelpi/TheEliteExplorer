using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;
using TheEliteExplorerUi.Models;

namespace TheEliteExplorerUi.Controllers
{
    public class SimulatedRankingController : Controller
    {
        private const int MaxRankDisplay = 50;
        private const string StageImagePath = @"D:\Ma programmation\csharp\Projects\TheEliteUI\TheEliteUI\Resources\Stages\{0}.jpg";
        private const string ViewName = "SimulatedRanking";

        private readonly IRankingProvider _rankingProvider;

        public SimulatedRankingController(IRankingProvider rankingProvider)
        {
            _rankingProvider = rankingProvider;
        }

        public async Task<IActionResult> Index(
            Game game,
            DateTime? date,
            long? simulatedPlayerId)
        {
            var rankingEntriesBase = await _rankingProvider
                .GetRankingEntries(game, date ?? ServiceProviderAccessor.ClockProvider.Now, true, simulatedPlayerId)
                .ConfigureAwait(false);

            var rankingEntries = rankingEntriesBase.Select(r => r as RankingEntry).ToList();

            var pointsRankingEntries = rankingEntries
                .Where(r => r.Rank <= MaxRankDisplay)
                .Select(r => r.ToPointsRankingItemData())
                .ToList();

            // this does not manage equality between two global times
            // ie one player will be ranked above/below the other one
            int rank = 1;
            var timeRankingEntries = rankingEntries
                .OrderBy(x => x.CumuledTime)
                .Take(MaxRankDisplay)
                .Select(r => r.ToTimeRankingItemData( rank++))
                .ToList();

            var secondsLevel = SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => 0);
            var stageWorldRecordEntries = game.GetStages()
                .Select(s => s.ToStageWorldRecordItemData(rankingEntries, secondsLevel, StageImagePath))
                .ToList();

            return View($"Views/{ViewName}.cshtml", new SimulatedRankingViewData
            {
                CombinedTime = new TimeSpan(0, 0, secondsLevel.Values.Sum()),
                EasyCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Easy]),
                MediumCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Medium]),
                HardCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Hard]),
                PointsRankingEntries = pointsRankingEntries,
                TimeRankingEntries = timeRankingEntries,
                StageWorldRecordEntries = stageWorldRecordEntries
            });
        }
    }
}