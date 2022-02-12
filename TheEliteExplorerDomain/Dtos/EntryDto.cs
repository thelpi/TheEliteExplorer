namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a row in the "entry" table.
    /// </summary>
    public class EntryDto : EntryBaseDto
    {
        /// <summary>
        /// "id" column value.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// "player_id" column value.
        /// </summary>
        public long PlayerId { get; set; }
        /// <summary>
        /// Is simulated date y/n.
        /// </summary>
        public bool IsSimulatedDate { get; set; }
    }
}
