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
    [Route("simulated-ranking/{game}")]
    public class SimulatedRankingController : Controller
    {
        private const int MaxRankDisplay = 50;
        private const string StageImagePath = @"/images/{0}.jpg";
        private const string RankingViewName = "SimulatedRanking";
        private const string PlayersViewName = "Players";
        private const string LastTiedWrViewName = "LastTiedWr";
        private const string PlayerDetailsViewName = "PlayerDetails";

        private readonly IRankingProvider _rankingProvider;
        private readonly IReadRepository _repository;
        private readonly IWorldRecordProvider _worldRecordProvider;

        public SimulatedRankingController(
            IRankingProvider rankingProvider,
            IReadRepository repository,
            IWorldRecordProvider worldRecordProvider)
        {
            _rankingProvider = rankingProvider;
            _repository = repository;
            _worldRecordProvider = worldRecordProvider;
        }

        [HttpGet("/game/{game}/sweeps")]
        public async Task<IActionResult> GetSweeps(
            [FromRoute] Game game,
            [FromQuery] bool untied,
            [FromQuery] int? stage)
        {
            if (stage.HasValue && (stage < 0 || stage > 20))
            {
                return BadRequest();
            }

            stage = stage.HasValue ? (game == Game.PerfectDark ? stage + 20 : stage) : null;

            var sweeps = await _worldRecordProvider
                .GetSweeps(game, untied, null, null, stage == null ? (Stage?)null: (Stage)stage.Value)
                .ConfigureAwait(false);

            sweeps = sweeps.OrderByDescending(s => s.Days).ToList();

            return View($"Views/Sweeps.cshtml", sweeps);
        }

        [HttpGet("/game/{game}/last-tied-wr")]
        public async Task<IActionResult> GetLastTiedWr(
            [FromRoute] Game game,
            [FromQuery] DateTime? date)
        {
            try
            {
                date = date ?? DateTime.Now;

                var entries = await _worldRecordProvider
                    .GetLastTiedWrs(game, date.Value)
                    .ConfigureAwait(false);

                var players = await _repository
                    .GetPlayers()
                    .ConfigureAwait(false);

                return View($"Views/{LastTiedWrViewName}.cshtml", entries.ToLastTiedWrViewData(date, players, StageImagePath));
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

        [HttpGet("cherry-pick")]
        public async Task<IActionResult> ByStagesSkip(
            [FromRoute] Game game,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] Stage[] skipStages)
        {
            if (!Enum.TryParse(typeof(Game), game.ToString(), out _))
            {
                return BadRequest();
            }

            return await SimulateRankingInternal(game, rankingDate, skipStages: skipStages)
                .ConfigureAwait(false);
        }

        [HttpGet("player-details/{playerId}")]
        public async Task<IActionResult> GetPlayerDetailsForSpecifeidRanking(
            [FromRoute] Game game,
            [FromRoute] long playerId,
            [FromQuery] DateTime? rankingDate,
            [FromQuery] Stage[] skipStages,
            [FromQuery] int? monthsPrior)
        {
            try
            {
                var rankingEntries = await GetRankingsWithParams(game, rankingDate ?? DateTime.Now, playerId, monthsPrior, skipStages).ConfigureAwait(false);

                var pRanking = rankingEntries.Single(r => r.PlayerId == playerId);

                return View($"Views/{PlayerDetailsViewName}.cshtml", pRanking.ToPlayerDetailsViewData(StageImagePath));
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

        private async Task<IActionResult> SimulateRankingInternal(
            Game game,
            DateTime? rankingDate,
            long? playerId = null,
            int? monthsPrior = null,
            Stage[] skipStages = null)
        {
            try
            {
                var rankingEntries = await GetRankingsWithParams(game, rankingDate ?? DateTime.Now, playerId, monthsPrior, skipStages).ConfigureAwait(false);

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

        private async Task<List<RankingEntry>> GetRankingsWithParams(
            Game game,
            DateTime rankingDate,
            long? playerId,
            int? monthsPrior,
            Stage[] skipStages)
        {
            var rankingEntriesBase = await _rankingProvider
                                .GetRankingEntries(game, rankingDate, true, playerId, monthsPrior, skipStages)
                                .ConfigureAwait(false);
            
            return rankingEntriesBase.Select(r => r as RankingEntry).ToList();
        }
    }
}