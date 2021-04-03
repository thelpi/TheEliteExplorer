using System;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a row in the "entry" table.
    /// </summary>
    public class EntryDto
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
        /// "level_id" column value.
        /// </summary>
        public long LevelId { get; set; }
        /// <summary>
        /// "stage_id" column value.
        /// </summary>
        public long StageId { get; set; }
        /// <summary>
        /// "date" column value.
        /// </summary>
        /// <remarks>Nullable.</remarks>
        public DateTime? Date { get; set; }
        /// <summary>
        /// "time" column value.
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// "system_id" column value.
        /// </summary>
        /// <remarks>Nullable.</remarks>
        public long? SystemId { get; set; }
    }
}
