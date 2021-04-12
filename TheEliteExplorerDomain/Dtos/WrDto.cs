using System;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a row in the "wr" table.
    /// </summary>
    public class WrDto
    {
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; set; }
        /// <summary>
        /// Level.
        /// </summary>
        public Level Level { get; set; }
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
