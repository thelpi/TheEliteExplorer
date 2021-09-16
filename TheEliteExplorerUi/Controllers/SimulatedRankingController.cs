﻿using System;
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
    [Route("simulated-ranking/{game}")]
    public class SimulatedRankingController : Controller
    {
        private const int MaxRankDisplay = 50;
        private const string StageImagePath = @"/images/{0}.jpg";
        private const string RankingViewName = "SimulatedRanking";
        private const string PlayersViewName = "Players";

        private readonly IRankingProvider _rankingProvider;
        private readonly IReadRepository _repository;

        public SimulatedRankingController(IRankingProvider rankingProvider, IReadRepository repository)
        {
            _rankingProvider = rankingProvider;
            _repository = repository;
        }

        [HttpGet("/players")]
        public async Task<IActionResult> GetPlayers()
        {
            try
            {
                var players = await _repository.GetPlayers().ConfigureAwait(false);

                return View($"Views/{PlayersViewName}.cshtml", players.ToList());
            }
            catch (Exception ex)
            {
                using (var w = new System.IO.StreamWriter($@"S:\iis_logs\global_app.log", true))
                {
                    w.WriteLine($"{DateTime.Now.ToString("yyyyMMddhhmmss")}\t{ex.Message}\t{ex.StackTrace}");
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("player/{playerId}")]
        public async Task<IActionResult> ByPlayer(
            [FromRoute] Game game,
            [FromRoute] long playerId,
            [FromQuery] DateTime? rankingDate)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _)
                || playerId <= 0)
            {
                return BadRequest();
            }

            return await SimulateRankingInternal(game, rankingDate, playerId)
                .ConfigureAwait(false);
        }

        [HttpGet("date-range")]
        public async Task<IActionResult> ByDateRange(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] int monthsPrior)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _)
                || monthsPrior <= 0)
            {
                return BadRequest();
            }

            return await SimulateRankingInternal(game, rankingDate, monthsPrior: monthsPrior)
                .ConfigureAwait(false);
        }

        private async Task<IActionResult> SimulateRankingInternal(Game game, DateTime? rankingDate, long? playerId = null, int? monthsPrior = null)
        {
            try
            {
                var rankingEntriesBase = await _rankingProvider
                    .GetRankingEntries(game, rankingDate ?? DateTime.Now, true, playerId, monthsPrior)
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
                    .Select(r => r.ToTimeRankingItemData(rank++))
                    .ToList();

                var secondsLevel = SystemExtensions.Enumerate<Level>().ToDictionary(l => l, l => 0);
                var stageWorldRecordEntries = game.GetStages()
                    .Select(s => s.ToStageWorldRecordItemData(rankingEntries, secondsLevel, StageImagePath))
                    .ToList();

                return View($"Views/{RankingViewName}.cshtml", new SimulatedRankingViewData
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
            catch (Exception ex)
            {
                using (var w = new System.IO.StreamWriter($@"S:\iis_logs\global_app.log", true))
                {
                    w.WriteLine($"{DateTime.Now.ToString("yyyyMMddhhmmss")}\t{ex.Message}\t{ex.StackTrace}");
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}