using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private const string LogsFilePath = @"S:\iis_logs\global_app.log";

        private const int MaxRankDisplay = 500;
        private const string StageImagePath = @"/images/{0}.jpg";
        private const string RankingViewName = "SimulatedRanking";
        private const string PlayersViewName = "Players";
        private const string PlayerDetailsViewName = "PlayerDetails";
        private const string SweepsViewName = "Sweeps";
        private const string IndexViewName = "Index";
        private const string RankingBoxesViewName = "RankingBoxes";
        private const string LastStandingViewName = "LastStanding";

        private readonly IStatisticsProvider _statisticsProvider;

        public SimulatedRankingController(
            IStatisticsProvider statisticsProvider)
        {
            _statisticsProvider = statisticsProvider;
        }

        [HttpGet("/")]
        public async Task<IActionResult> IndexAsync()
        {
            return await DoAndCatchAsync(
                IndexViewName,
                "List of features",
                async() =>
                {
                    var players = await _statisticsProvider.GetPlayersAsync().ConfigureAwait(false);

                    return new IndexViewData { Players = players };
                }).ConfigureAwait(false);
        }

        /*[HttpGet("/games/{game}/ranking")]
        public async Task<IActionResult> GetFGameRankingAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate)
        {
            return await DoAndCatchAsync(
                RankingBoxesViewName,
                $"{game} ranking",
                async() =>
                {
                    var ranking = await _statisticsProvider
                        .GetGameRankingAsync(game, (rankingDate ?? DateTime.Now).Date, NoDateEntryRankingRule.Average)
                        .ConfigureAwait(false);

                    var pointsRankingData = new List<PointsRankingItemData>();
                    foreach (var entry in ranking.OrderBy(r => r.PointsRank))
                    {
                        pointsRankingData.Add(new PointsRankingItemData
                        {
                            EasyPoints = entry.LevelPoints[Level.Easy],
                            HardPoints = entry.LevelPoints[Level.Hard],
                            MediumPoints = entry.LevelPoints[Level.Medium],
                            PlayerColor = entry.Player.Color,
                            PlayerName = game == Game.PerfectDark
                                ? entry.Player.SurName
                                : entry.Player.RealName,
                            Rank = entry.PointsRank,
                            TotalPoints = entry.Points
                        });
                    }

                    var timeRankingData = new List<TimeRankingItemData>();
                    foreach (var entry in ranking.OrderBy(r => r.TimeRank))
                    {
                        timeRankingData.Add(new TimeRankingItemData
                        {
                            EasyTime = entry.LevelTime[Level.Easy],
                            HardTime = entry.LevelTime[Level.Hard],
                            MediumTime = entry.LevelTime[Level.Medium],
                            PlayerColor = entry.Player.Color,
                            PlayerName = game == Game.PerfectDark
                                ? entry.Player.SurName
                                : entry.Player.RealName,
                            Rank = entry.TimeRank,
                            TotalTime = entry.Time
                        });
                    }

                    return new Tuple<List<PointsRankingItemData>, List<TimeRankingItemData>>(pointsRankingData, timeRankingData);
                })
                .ConfigureAwait(false);
        }*/

        [HttpGet("/games/{game}/sweeps")]
        public async Task<IActionResult> GetSweepsAsync(
            [FromRoute] Game game,
            [FromQuery] bool untied,
            [FromQuery] int? stage)
        {
            return await DoAndCatchAsync(
                SweepsViewName,
                "300 points / untied sweeps",
                async () =>
                {
                    if (stage.HasValue && (stage < 0 || stage > 20))
                    {
                        return BadRequest();
                    }

                    stage = stage.HasValue ? (game == Game.PerfectDark ? stage + 20 : stage) : null;

                    var sweeps = await _statisticsProvider
                        .GetSweepsAsync(game, untied, null, null, stage == null ? (Stage?)null : (Stage)stage.Value)
                        .ConfigureAwait(false);

                    return sweeps.OrderByDescending(s => s.Days).ToList();
                }).ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/longest-standing")]
        public async Task<IActionResult> GetLongestStandingAsync(
            [FromRoute] Game game,
            [FromQuery] StandingType standingType,
            [FromQuery] bool? stillOngoing)
        {
            return await DoAndCatchAsync(
                LastStandingViewName,
                standingType.AsPageTitle(stillOngoing == true),
                async () =>
                {
                    var standings = await _statisticsProvider
                        .GetLongestStandingsAsync(game, null, standingType, stillOngoing)
                        .ConfigureAwait(false);

                    return new StandingWrViewData
                    {
                        TopDetails = standings
                            .Take(25)
                            .Select(x => x.ToStandingWrLevelItemData())
                            .ToList()
                    };
                }).ConfigureAwait(false);
        }

        [HttpGet("/players")]
        public async Task<IActionResult> GetPlayersAsync()
        {
            return await DoAndCatchAsync(
                PlayersViewName,
                "Players list",
                async () =>
                {
                    var players = await _statisticsProvider.GetPlayersAsync().ConfigureAwait(false);

                    return players.ToList();
                }).ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/player-rankings")]
        public async Task<IActionResult> GetRankingByPlayerAsync(
            [FromRoute] Game game,
            [FromQuery] long playerId,
            [FromQuery] DateTime? rankingDate)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _)
                || playerId <= 0)
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, playerId)
                .ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/time-frame-rankings")]
        public async Task<IActionResult> GetRankingByTimeFrameAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] int monthsPrior)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _)
                || monthsPrior <= 0)
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, monthsPrior: monthsPrior)
                .ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/engine-rankings")]
        public async Task<IActionResult> GetRankingByEngineAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] int engine)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _))
            {
                return BadRequest();
            }

            if (!SystemExtensions.Enumerate<Engine>().Any(e => engine == (int)e))
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, engine: engine)
                .ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/losers-bracket-rankings")]
        public async Task<IActionResult> GetRankingWithNoWrAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] bool untied)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _))
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, excludeWinners: (untied ? (bool?)null : true))
                .ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/cherry-pick-rankings")]
        public async Task<IActionResult> GetRankingByStageCherryPickAsync(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] Stage[] skipStages)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _))
            {
                return BadRequest();
            }

            return await SimulateRankingInternalAsync(game, rankingDate, skipStages: skipStages)
                .ConfigureAwait(false);
        }

        [HttpGet("/games/{game}/players/{playerId}/details")]
        public async Task<IActionResult> GetPlayerDetailsForSpecifiedRankingAsync(
            [FromRoute] Game game,
            [FromRoute] long playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] Stage[] skipStages,
            [FromQuery] int? monthsPrior,
            [FromQuery] int? engine)
        {
            return await DoAndCatchAsync(
                PlayerDetailsViewName,
                $"PlayerID {playerId} - {game.ToString()} times",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(game,
                        rankingDate ?? DateTime.Now, playerId, monthsPrior, skipStages, false, engine)
                    .ConfigureAwait(false);

                    var pRanking = rankingEntries.Single(r => r.Player.Id == playerId);

                    return pRanking.ToPlayerDetailsViewData(StageImagePath);
                }).ConfigureAwait(false);
        }

        private async Task<IActionResult> SimulateRankingInternalAsync(
            Game game,
            DateTime? rankingDate,
            long? playerId = null,
            int? monthsPrior = null,
            Stage[] skipStages = null,
            bool? excludeWinners = false,
            int? engine = null)
        {
            return await DoAndCatchAsync(
                RankingViewName,
                "The GoldenEye/PerfectDark World Records and Rankings SIMULATOR",
                async () =>
                {
                    var rankingEntries = await GetRankingsWithParamsAsync(game, rankingDate ?? DateTime.Now, playerId, monthsPrior, skipStages, excludeWinners, engine).ConfigureAwait(false);

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
                        .Select(r => r.ToTimeRankingItemData(rank++))
                        .ToList();

                    var secondsLevel = SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => 0);
                    var stageWorldRecordEntries = game.GetStages()
                        .Select(s => s.ToStageWorldRecordItemData(rankingEntries, secondsLevel, StageImagePath))
                        .ToList();

                    return new SimulatedRankingViewData
                    {
                        CombinedTime = new TimeSpan(0, 0, secondsLevel.Values.Sum()),
                        EasyCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Easy]),
                        MediumCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Medium]),
                        HardCombinedTime = new TimeSpan(0, 0, secondsLevel[Level.Hard]),
                        PointsRankingEntries = pointsRankingEntries,
                        TimeRankingEntries = timeRankingEntries,
                        StageWorldRecordEntries = stageWorldRecordEntries
                    };
                }).ConfigureAwait(false);
        }

        private async Task<List<RankingEntry>> GetRankingsWithParamsAsync(
            Game game,
            DateTime rankingDate,
            long? playerId,
            int? monthsPrior,
            Stage[] skipStages,
            bool? excludeWinners,
            int? engine)
        {
            var request = new RankingRequest
            {
                Game = game,
                FullDetails = true,
                SkipStages = skipStages,
                RankingDate = rankingDate,
                Engine = (Engine?)engine,
                IncludeUnknownEngine = true
            };
            
            if (playerId.HasValue)
            {
                request.PlayerVsLegacy = (playerId.Value, rankingDate);
                request.RankingDate = DateTime.Now;
            }

            if (excludeWinners != false)
            {
                request.ExcludePlayer = excludeWinners.HasValue
                    ? RankingRequest.ExcludePlayerType.HasWorldRecord
                    : RankingRequest.ExcludePlayerType.HasUntied;
            }

            if (monthsPrior.HasValue)
            {
                request.RankingStartDate = rankingDate.AddMonths(-monthsPrior.Value);
            }

            var rankingEntriesBase = await _statisticsProvider
                .GetRankingEntriesAsync(request)
                .ConfigureAwait(false);
            
            return rankingEntriesBase.Select(r => r as RankingEntry).ToList();
        }

        private async Task<IActionResult> DoAndCatchAsync(
            string viewName,
            string title,
            Func<Task<object>> getDatasFunc)
        {
            try
            {
                var datas = await getDatasFunc().ConfigureAwait(false);

                return View($"Views/Template.cshtml", new BaseViewData
                {
                    Data = datas,
                    Name = $"~/Views/{viewName}.cshtml",
                    Title = title
                });
            }
            catch (Exception ex)
            {
                try
                {
                    using (var w = new System.IO.StreamWriter(LogsFilePath, true))
                    {
                        w.WriteLine($"{DateTime.Now.ToString("yyyyMMddhhmmss")}\t{ex.Message}\t{ex.StackTrace}");
                    }
                }
                catch { }

                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}