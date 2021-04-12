using System;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a row in the "wr" table.
    /// </summary>
    public class WrDto
    {
        /// <summary>
        /// Stage identifier.
        /// </summary>
        public long StageId { get; set; }
        /// <summary>
        /// Level identifier.
        /// </summary>
        public long LevelId { get; set; }
        /// <summary>
        /// Player identifier.
        /// </summary>
        public long PlayerId { get; set; }
        /// <summary>
        /// Is untied y/n.
        /// </summary>
        public bool Untied { get; set; }
        /// <summary>
        /// Time.
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// Date.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Is first tied y/n.
        /// </summary>
        public bool FirstTied { get; set; }
    }
}
