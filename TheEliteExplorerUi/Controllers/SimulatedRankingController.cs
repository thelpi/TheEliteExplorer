using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;
using TheEliteExplorerUi.Models;

namespace TheEliteExplorerUi.Controllers
{
    public class SimulatedRankingController : Controller
    {
        private readonly IRankingProvider _rankingProvider;

        public SimulatedRankingController(IRankingProvider rankingProvider)
        {
            _rankingProvider = rankingProvider;
        }

        public async Task<IActionResult> Index(
            Game game,
            DateTime? date,
            bool full,
            long? simulatedPlayerId)
        {
            var rankingEntries = await _rankingProvider
                .GetRankingEntries(game, date ?? ServiceProviderAccessor.ClockProvider.Now, full, simulatedPlayerId)
                .ConfigureAwait(false);

            // TODO : build the view data
            var pointsRankingEntries = new System.Collections.Generic.List<PointsRankingItemData>();
            var timeRankingEntries = new System.Collections.Generic.List<TimeRankingItemData>();
            
            foreach (var entry in rankingEntries)
            {
                if (entry.Rank > 50) break;
                var fullEntry = entry as RankingEntry;
                pointsRankingEntries.Add(new PointsRankingItemData
                {
                    EasyPoints = fullEntry.LevelPoints[Level.Easy],
                    HardPoints = fullEntry.LevelPoints[Level.Hard],
                    MediumPoints = fullEntry.LevelPoints[Level.Medium],
                    PlayerColor = fullEntry.PlayerColor,
                    PlayerName = fullEntry.PlayerName,
                    Rank = fullEntry.Rank,
                    TotalPoints = fullEntry.Points
                });
            }

            // doto : gérer les égalités
            int rank = 1;
            foreach (var entry in rankingEntries.OrderBy(x => x.CumuledTime))
            {
                if (rank > 50) break;
                var fullEntry = entry as RankingEntry;
                timeRankingEntries.Add(new TimeRankingItemData
                {
                    EasyTime = new TimeSpan(0, 0, (int)fullEntry.LevelCumuledTime[Level.Easy]),
                    HardTime = new TimeSpan(0, 0, (int)fullEntry.LevelCumuledTime[Level.Hard]),
                    MediumTime = new TimeSpan(0, 0, (int)fullEntry.LevelCumuledTime[Level.Medium]),
                    PlayerColor = fullEntry.PlayerColor,
                    PlayerName = fullEntry.PlayerName,
                    Rank = rank,
                    TotalTime = new TimeSpan(0, 0, (int)fullEntry.CumuledTime)
                });
                rank++;
            }

            return View("Views/SimulatedRanking.cshtml", new SimulatedRankingViewData
            {
                CombinedTime = new TimeSpan(1, 12, 45),
                PointsRankingEntries = pointsRankingEntries,
                TimeRankingEntries = timeRankingEntries
            });
        }
    }
}