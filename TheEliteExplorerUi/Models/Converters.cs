using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Enums;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorerUi.Models
{
    internal static class Converters
    {
        internal static PointsRankingItemData ToPointsRankingItemData(this RankingEntry entry)
        {
            return new PointsRankingItemData
            {
                EasyPoints = entry.LevelPoints[Level.Easy],
                HardPoints = entry.LevelPoints[Level.Hard],
                MediumPoints = entry.LevelPoints[Level.Medium],
                PlayerColor = entry.PlayerColor,
                PlayerName = entry.PlayerName,
                Rank = entry.Rank,
                TotalPoints = entry.Points
            };
        }

        internal static TimeRankingItemData ToTimeRankingItemData(this RankingEntry entry, int rank)
        {
            return new TimeRankingItemData
            {
                EasyTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Easy]),
                HardTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Hard]),
                MediumTime = new TimeSpan(0, 0, (int)entry.LevelCumuledTime[Level.Medium]),
                PlayerColor = entry.PlayerColor,
                PlayerName = entry.PlayerName,
                Rank = rank,
                TotalTime = new TimeSpan(0, 0, (int)entry.CumuledTime)
            };
        }

        internal static StageWorldRecordItemData ToStageWorldRecordItemData(this Stage stage,
            List<RankingEntry> rankingEntries,
            Dictionary<Level, int> secondsLevelCollector,
            string stageImagePath)
        {
            var coloredInitialsLevel = new Dictionary<Level, List<(string, string, string)>>();
            var secondsLevelStage = new Dictionary<Level, int>();
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var bestTime = GetStageAndLevelBestTime(rankingEntries, stage, level);
                secondsLevelStage.Add(level, bestTime);
                coloredInitialsLevel.Add(level, GetPlayersRankedAtStageAndLevelTime(rankingEntries, stage, level, bestTime));
                secondsLevelCollector[level] += bestTime;
            }

            return new StageWorldRecordItemData
            {
                EasyColoredInitials = coloredInitialsLevel[Level.Easy],
                EasyTime = new TimeSpan(0, 0, secondsLevelStage[Level.Easy]),
                MediumColoredInitials = coloredInitialsLevel[Level.Medium],
                MediumTime = new TimeSpan(0, 0, secondsLevelStage[Level.Medium]),
                HardColoredInitials = coloredInitialsLevel[Level.Hard],
                HardTime = new TimeSpan(0, 0, secondsLevelStage[Level.Hard]),
                Image = string.Format(stageImagePath, (int)stage),
                Name = stage.ToString(),
                Code = $"s{(int)stage}"
            };
        }

        private static List<(string, string, string)> GetPlayersRankedAtStageAndLevelTime(List<RankingEntry> rankingEntries, Stage stage, Level level, int bestTime)
        {
            return rankingEntries
                .Where(x => x.Details.ContainsKey(stage)
                    && x.Details[stage].ContainsKey(level)
                    && x.Details[stage][level].Item3 == bestTime)
                .OrderBy(x => x.Details[stage][level].Item4)
                .Select(x => (x.PlayerName.ToInitials(), x.PlayerColor, x.PlayerName))
                .ToList();
        }

        private static int GetStageAndLevelBestTime(List<RankingEntry> rankingEntries, Stage stage, Level level)
        {
            return (int)rankingEntries
                .Where(x => x.Details.ContainsKey(stage)
                    && x.Details[stage].ContainsKey(level))
                .OrderBy(x => x.Details[stage][level].Item3)
                .First()
                .Details[stage][level]
                .Item3;
        }

        private static string ToInitials(this string playerName)
        {
            var parts = playerName
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(_ => _.Trim())
                .ToArray();

            var initials = parts.Length < 2
                ? parts[0].Substring(0, 2)
                : string.Concat(parts[0].Substring(0, 1), parts[parts.Length == 2 ? 1 : 2].Substring(0, 1));

            return initials.ToUpperInvariant();
        }
    }
}
