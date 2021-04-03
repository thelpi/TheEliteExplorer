using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerDomain.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public class StageSweepBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="players"></param>
        /// <param name="untied"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public IReadOnlyCollection<StageSweep> GetSweeps(
            IReadOnlyCollection<EntryDto> entries,
            IReadOnlyCollection<PlayerDto> players,
            bool untied,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            if ((entries?.Count ?? 0) == 0)
            {
                throw new ArgumentException($"{entries} is null or empty.", nameof(entries));
            }

            var fullList = new List<(long playerId, DateTime date, Stage stage)>();

            var game = Stage.Get().FirstOrDefault(s => s.Id == entries.First().StageId).Game;

            var groupEntries = entries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.StageId, e.LevelId))
                .ToDictionary(e => e.Key, e => e.ToList());

            foreach (var currentDate in SystemExtensions.LoopBetweenDates(
                startDate ?? Extensions.GetEliteFirstDate(game),
                endDate ?? ServiceProviderAccessor.ClockProvider.Now,
                DateStep.Day))
            {
                foreach (var stage in Stage.Get(game))
                {
                    var playersWithWr = new List<long>();
                    long? pId = null;
                    bool isUntiedSweep = true;
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        var currentWr = groupEntries[(stage.Id, (int)level)]
                            .Where(e => e.Date.Value.Date <= currentDate.Date)
                            .GroupBy(e => e.Time)
                            .OrderBy(e => e.Key)
                            .FirstOrDefault();
                        if (untied)
                        {
                            if (currentWr?.Count() == 1)
                            {
                                var currentPId = currentWr.First().PlayerId;
                                if (!pId.HasValue)
                                {
                                    pId = currentPId;
                                }
                                else if (pId.Value != currentPId)
                                {
                                    isUntiedSweep = false;
                                    break;
                                }
                            }
                            else
                            {
                                isUntiedSweep = false;
                                break;
                            }
                        }
                        else
                        {
                            if (currentWr?.Count() > 0)
                            {
                                if (playersWithWr.Count == 0)
                                {
                                    playersWithWr.AddRange(currentWr.Select(_ => _.PlayerId));
                                }
                                else
                                {
                                    playersWithWr = playersWithWr.Intersect(currentWr.Select(_ => _.PlayerId)).ToList();
                                }
                                if (playersWithWr.Count == 0)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                playersWithWr.Clear();
                                break;
                            }
                        }
                    }
                    if (untied)
                    {
                        if (isUntiedSweep)
                        {
                            fullList.Add((pId.Value, currentDate.Date, stage));
                        }
                    }
                    else
                    {
                        fullList.AddRange(playersWithWr.Select(_ => (_, currentDate, stage)));
                    }
                }
            }

            fullList = fullList.OrderBy(f => f.date).ToList();

            var refinedList = new List<StageSweep>();

            foreach (var (playerId, date, stage) in fullList)
            {
                var yesterdayEntry = refinedList.FirstOrDefault(e =>
                    e.PlayerId == playerId
                    && e.Stage == stage
                    && e.EndDate == date.AddDays(-1));
                if (yesterdayEntry == null)
                {
                    refinedList.Add(new StageSweep
                    {
                        EndDate = date,
                        StartDate = date,
                        Player = new Player(players.Single(p => p.Id == playerId)),
                        PlayerId = playerId,
                        Stage = stage
                    });
                }
                else
                {
                    yesterdayEntry.EndDate = yesterdayEntry.EndDate.AddDays(1);
                }
            }

            return refinedList.Select(e =>
            {
                e.EndDate = e.EndDate.AddDays(1);
                return e;
            }).ToList();
        }
    }
}
