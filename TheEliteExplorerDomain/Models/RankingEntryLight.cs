﻿using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a ranking entry.
    /// </summary>
    /// <seealso cref="Ranking"/>
    public class RankingEntryLight : Ranking
    {
        /// <summary>
        /// When a time is unknown, the value used is <c>20</c> minutes.
        /// </summary>
        public static readonly long UnsetTimeValueSeconds = 20 * 60;

        /// <summary>
        /// Game.
        /// </summary>
        public Game Game { get; }
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; }
        /// <summary>
        /// Player name (real name).
        /// </summary>
        public string PlayerName { get; }
        /// <summary>
        /// Player color.
        /// </summary>
        public string PlayerColor { get; }
        /// <summary>
        /// Points.
        /// </summary>
        public int Points { get; private set; }
        /// <summary>
        /// Time cumuled on every level/stage.
        /// </summary>
        public long CumuledTime { get; private set; }
        /// <summary>
        /// Count of untied world records.
        /// </summary>
        public int UntiedRecordsCount { get; private set; }
        /// <summary>
        /// Count of world records.
        /// </summary>
        public int RecordsCount { get; private set; }

        internal RankingEntryLight(Game game, PlayerDto player)
        {
            Game = game;
            PlayerId = player.Id;
            PlayerName = player.RealName;
            PlayerColor = player.Color;

            Points = 0;
            UntiedRecordsCount = 0;
            RecordsCount = 0;

            CumuledTime = (UnsetTimeValueSeconds * Game.GetStages().Count) * SystemExtensions.Count<Level>();
        }

        internal virtual int AddStageAndLevelDatas(RankingDto ranking, bool untied)
        {
            int points = (100 - ranking.Rank) - 2;
            if (points < 0)
            {
                points = 0;
            }
            if (ranking.Rank == 1)
            {
                points = 100;
                RecordsCount++;
                if (untied)
                {
                    UntiedRecordsCount++;
                }
            }
            else if (ranking.Rank == 2)
            {
                points = 97;
            }

            Points += points;

            if (ranking.Time < UnsetTimeValueSeconds)
            {
                CumuledTime -= UnsetTimeValueSeconds - ranking.Time;
            }

            return points;
        }
    }
}
