using System;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Ranking DTO.
    /// </summary>
    public class RankingDto : RankingBaseDto
    {
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; set; }
        /// <summary>
        /// Leve.
        /// </summary>
        public Level Level { get; set; }
        /// <summary>
        /// Date.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Rank.
        /// </summary>
        public int Rank { get; set; }
    }
}
