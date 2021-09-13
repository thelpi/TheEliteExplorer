using System;
using System.Collections.Generic;
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
            long? simulatedPlayerId)
        {
            var rankingEntriesBase = await _rankingProvider
                .GetRankingEntries(game, date ?? ServiceProviderAccessor.ClockProvider.Now, true, simulatedPlayerId)
                .ConfigureAwait(false);

            var rankingEntries = rankingEntriesBase.Select(r => r as RankingEntry).ToList();

            var pointsRankingEntries = new List<PointsRankingItemData>();
            foreach (var entry in rankingEntries)
            {
                if (entry.Rank > 50) break;
                pointsRankingEntries.Add(new PointsRankingItemData
                {
                    EasyPoints = entry.LevelPoints[Level.Easy],
                    HardPoints = entry.LevelPoints[Level.Hard],
                    MediumPoints = entry.LevelPoints[Level.Medium],
                    PlayerColor = entry.PlayerColor,
                    PlayerName = entry.PlayerName,
                    Rank = entry.Rank,
                    TotalPoints = entry.Points
                });
            }

            // TODO: manage equality between times (rare)
            int rank = 1;
            var timeRankingEntries = new List<TimeRankingItemData>();
            foreach (var entry in rankingEntries.OrderBy(x => x.CumuledTime))
            {
                if (rank > 50) break;
                timeRankingEntries.Add(new TimeRankingItemData
                {
                    EasyTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Easy]),
                    HardTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Hard]),
                    MediumTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Medium]),
                    PlayerColor = entry.PlayerColor,
                    PlayerName = entry.PlayerName,
                    Rank = rank,
                    TotalTime = new TimeSpan(0, 0, (int)entry.CumuledTime)
                });
                rank++;
            }

            var easySeconds = 0;
            var mediumSeconds = 0;
            var hardSeconds = 0;
            var stageWorldRecordEntries = new List<StageWorldRecordItemData>();
            foreach (var stage in SystemExtensions.Enumerate<Stage>().Where(s => TheEliteExplorerDomain.Extensions.GetGame(s) == game))
            {
                var easyEntryRef = rankingEntries.Where(x => x.Details.ContainsKey(stage) && x.Details[stage].ContainsKey(Level.Easy)).OrderBy(x => x.Details[stage][Level.Easy].Item3).First();
                var mediumEntryRef = rankingEntries.Where(x => x.Details.ContainsKey(stage) && x.Details[stage].ContainsKey(Level.Medium)).OrderBy(x => x.Details[stage][Level.Medium].Item3).First();
                var hardEntryRef = rankingEntries.Where(x => x.Details.ContainsKey(stage) && x.Details[stage].ContainsKey(Level.Hard)).OrderBy(x => x.Details[stage][Level.Hard].Item3).First();

                var easyTime = (int)easyEntryRef.Details[stage][Level.Easy].Item3;
                var mediumTime = (int)mediumEntryRef.Details[stage][Level.Medium].Item3;
                var hardTime = (int)hardEntryRef.Details[stage][Level.Hard].Item3;

                var easyEntries = rankingEntries.Where(x => x.Details.ContainsKey(stage) && x.Details[stage].ContainsKey(Level.Easy) && x.Details[stage][Level.Easy].Item3 == easyTime).ToList();
                var mediumEntries = rankingEntries.Where(x => x.Details.ContainsKey(stage) && x.Details[stage].ContainsKey(Level.Medium) && x.Details[stage][Level.Medium].Item3 == mediumTime).ToList();
                var hardEntries = rankingEntries.Where(x => x.Details.ContainsKey(stage) && x.Details[stage].ContainsKey(Level.Hard) && x.Details[stage][Level.Hard].Item3 == hardTime).ToList();

                stageWorldRecordEntries.Add(new StageWorldRecordItemData
                {
                    EasyColoredInitials = easyEntries.OrderBy(x => x.Details[stage][Level.Easy].Item4).Select(x => (ToInitials(x.PlayerName), x.PlayerColor)).ToList(),
                    EasyTime = new TimeSpan(0, 0, easyTime),
                    HardColoredInitials = hardEntries.OrderBy(x => x.Details[stage][Level.Hard].Item4).Select(x => (ToInitials(x.PlayerName), x.PlayerColor)).ToList(),
                    HardTime = new TimeSpan(0, 0, hardTime),
                    Image = $@"D:\Ma programmation\csharp\Projects\TheEliteUI\TheEliteUI\Resources\Stages\{(int)stage}.jpg",
                    MediumColoredInitials = mediumEntries.OrderBy(x => x.Details[stage][Level.Medium].Item4).Select(x => (ToInitials(x.PlayerName), x.PlayerColor)).ToList(),
                    MediumTime = new TimeSpan(0, 0, mediumTime),
                    Name = stage.ToString(),
                    Code = $"s{(int)stage}"
                });
                easySeconds += easyTime;
                mediumSeconds += mediumTime;
                hardSeconds += hardTime;
            }

            return View("Views/SimulatedRanking.cshtml", new SimulatedRankingViewData
            {
                CombinedTime = new TimeSpan(0, 0, easySeconds + mediumSeconds + hardSeconds),
                EasyCombinedTime = new TimeSpan(0, 0, easySeconds),
                MediumCombinedTime = new TimeSpan(0, 0, mediumSeconds),
                HardCombinedTime = new TimeSpan(0, 0, hardSeconds),
                PointsRankingEntries = pointsRankingEntries,
                TimeRankingEntries = timeRankingEntries,
                StageWorldRecordEntries = stageWorldRecordEntries
            });
        }

        private string ToInitials(string playerName)
        {
            var splitter = playerName.Split(' ');

            if (splitter.Length < 2)
                return splitter[0].Substring(0, 2).ToUpperInvariant();
            else if (splitter.Length == 2)
                return string.Concat(splitter[0].Substring(0, 1), splitter[1].Substring(0, 1)).ToUpperInvariant();
            else
                return string.Concat(splitter[0].Substring(0, 1), splitter[2].Substring(0, 1)).ToUpperInvariant();
        }
    }
}