using System;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Ranking base DTO.
    /// </summary>
    public class RankingBaseDto
    {
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; set; }
        /// <summary>
        /// Entry date.
        /// </summary>
        public DateTime? EntryDate { get; set; }
        /// <summary>
        /// Time.
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// Date is simulated y/n.
        /// </summary>
        public bool IsSimulatedDate { get; set; }
        /// <summary>
        /// Entry identifier.
        /// </summary>
        public long EntryId { get; set; }
        /// <summary>
        /// System identifier.
        /// </summary>
        public Engine? SystemId { get; set; }
    }
}
