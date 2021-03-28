using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// 
    /// </summary>
    public class UntiedSweepBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="players"></param>
        /// <returns></returns>
        public IReadOnlyCollection<UntiedSweep> GetUntiedSweeps(
            IReadOnlyCollection<EntryDto> entries,
            IReadOnlyCollection<PlayerDto> players)
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

            var game = entries.First().Game.Value;

            var groupEntries = entries
                .Where(e => e.Date.HasValue)
                .GroupBy(e => (e.StageId, e.LevelId))
                .ToDictionary(e => e.Key, e => e.ToList());

            foreach (var currentDate in SystemExtensions.LoopBetweenDates(
                Extensions.GetEliteFirstDate(game),
                DateStep.Day))
            {
                foreach (var stage in Stage.Get(game))
                {
                    long? pId = null;
                    bool isUntiedSweep = true;
                    foreach (var level in SystemExtensions.Enumerate<Level>())
                    {
                        var currentWr = groupEntries[(stage.Id, (int)level)]
                            .Where(e => e.Date.Value.Date <= currentDate.Date)
                            .GroupBy(e => e.Time)
                            .OrderBy(e => e.Key)
                            .FirstOrDefault();
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
                    if (isUntiedSweep)
                    {
                        fullList.Add((pId.Value, currentDate.Date, stage));
                    }
                }
            }

            fullList = fullList.OrderBy(f => f.date).ToList();

            var refinedList = new List<UntiedSweep>();

            foreach (var (playerId, date, stage) in fullList)
            {
                var yesterdayEntry = refinedList.FirstOrDefault(e =>
                    e.PlayerId == playerId
                    && e.Stage == stage
                    && e.EndDate == date.AddDays(-1));
                if (yesterdayEntry == null)
                {
                    refinedList.Add(new UntiedSweep
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
