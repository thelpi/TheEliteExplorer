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
            var invalid = false;
            var coloredInitialsLevel = new Dictionary<Level, List<(string, string, string)>>();
            var secondsLevelStage = new Dictionary<Level, int>();
            foreach (var level in SystemExtensions.Enumerate<Level>())
            {
                var bestTime = GetStageAndLevelBestTime(rankingEntries, stage, level);
                if (!bestTime.HasValue)
                {
                    invalid = true;
                    break;
                }
                secondsLevelStage.Add(level, bestTime.Value);
                coloredInitialsLevel.Add(level, GetPlayersRankedAtStageAndLevelTime(rankingEntries, stage, level, bestTime.Value));
                secondsLevelCollector[level] += bestTime.Value;
            }

            // sets the whole stage as invalid...
            if (invalid) return null;

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

        internal static LastTiedWrViewData ToLastTiedWrViewData(
            this Dictionary<Stage, Dictionary<Level, (TheEliteExplorerDomain.Dtos.EntryDto, bool)>> entries,
            DateTime? date,
            IReadOnlyCollection<TheEliteExplorerDomain.Dtos.PlayerDto> players,
            string stageImagePath)
        {
            var vd = new LastTiedWrViewData
            {
                StageDetails = entries.Keys
                    .Select(stage => new LastTiedWrStageItemData
                    {
                        EasyData = entries[stage].ToLastTiedWrLevelItemData(Level.Easy, date, players, stage),
                        HardData = entries[stage].ToLastTiedWrLevelItemData(Level.Hard, date, players, stage),
                        MediumData = entries[stage].ToLastTiedWrLevelItemData(Level.Medium, date, players, stage),
                        Image = string.Format(stageImagePath, (int)stage),
                        Name = stage.ToString()
                    })
                    .ToList()
            };

            vd.TopDetails = vd.StageDetails
                .SelectMany(_ => new[] { _.EasyData, _.HardData, _.MediumData })
                .OrderByDescending(_ => _.EntryDays)
                .ToList();

            return vd;
        }

        private static LastTiedWrLevelItemData ToLastTiedWrLevelItemData(
            this Dictionary<Level, (TheEliteExplorerDomain.Dtos.EntryDto, bool)> levelData,
            Level level,
            DateTime? date,
            IReadOnlyCollection<TheEliteExplorerDomain.Dtos.PlayerDto> players,
            Stage stage)
        {
            if (!levelData.ContainsKey(level) || levelData[level].Item1 == null) return null;

            var p = players.FirstOrDefault(_ => _.Id == levelData[level].Item1.PlayerId);

            return new LastTiedWrLevelItemData
            {
                EntryDate = levelData[level].Item1.Date.Value,
                EntryDays = (int)Math.Floor((date.Value - levelData[level].Item1.Date.Value).TotalDays),
                EntryTime = new TimeSpan(0, 0, (int)levelData[level].Item1.Time),
                PlayerColor = p?.Color,
                PlayerInitials = p?.RealName.ToInitials(),
                PlayerName = p?.RealName,
                Stage = stage,
                Level = level,
                Untied = levelData[level].Item2 ? 'Y' : 'N'
            };
        }

        private static List<(string, string, string)> GetPlayersRankedAtStageAndLevelTime(List<RankingEntry> rankingEntries, Stage stage, Level level, int bestTime)
        {
            return rankingEntries
                .Where(x => IsValid(stage, level, x)
                    && x.Details[stage][level].Item3 == bestTime)
                .OrderBy(x => x.Details[stage][level].Item4)
                .Select(x => (x.PlayerName.ToInitials(), PlayerColor: x.PlayerColor, PlayerName: x.PlayerName))
                .ToList();
        }

        private static bool IsValid(Stage stage, Level level, RankingEntry x)
        {
            return x.Details != null
                && x.Details.ContainsKey(stage)
                && x.Details[stage] != null
                && x.Details[stage].ContainsKey(level);
        }

        private static int? GetStageAndLevelBestTime(List<RankingEntry> rankingEntries, Stage stage, Level level)
        {
            var item = rankingEntries
                .Where(x => IsValid(stage, level, x)
                    && x.Details[stage][level].Item3.HasValue)
                .OrderBy(x => x.Details[stage][level].Item3)
                .FirstOrDefault();

            if (item == null)
            {
                return null;
            }

            return (int)item
                .Details[stage][level]
                .Item3;
        }

        private static string ToInitials(this string playerName)
        {
            if (playerName == null) return "";

            var parts = playerName
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(_ => _.Trim())
                .ToArray();

            if (parts.Length == 0) return "";

            var initials = parts.Length < 2
                ? parts[0].Substring(0, 2)
                : string.Concat(parts[0].Substring(0, 1), parts[parts.Length == 2 ? 1 : 2].Substring(0, 1));

            return initials.ToUpperInvariant();
        }
    }
}
