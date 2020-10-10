using System;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Ranking DTO.
    /// </summary>
    public class RankingDto
    {
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; set; }
        /// <summary>
        /// Stage identifier.
        /// </summary>
        public long StageId { get; set; }
        /// <summary>
        /// Level identifier.
        /// </summary>
        public long LevelId { get; set; }
        /// <summary>
        /// Date.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Time.
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// Rank.
        /// </summary>
        public int Rank { get; set; }
    }
}
