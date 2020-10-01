using System;
using System.Linq;
using TheEliteExplorer.Domain;

namespace TheEliteExplorer.Infrastructure.Dtos
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
        /// <remarks>Nullable.</remarks>
        public long? Time { get; set; }
        /// <summary>
        /// "system_id" column value.
        /// </summary>
        /// <remarks>Nullable.</remarks>
        public long? SystemId { get; set; }

        /// <summary>
        /// Checks if the instance is valid.
        /// </summary>
        /// <returns><c>True</c> if valid; <c>False</c> otherwise.</returns>
        public bool IsValid()
        {
            if (Id <= 0 || PlayerId <= 0 || Time.Value <= 0)
            {
                return false;
            }

            if (!TypeExtensions.Enumerate<Level>().Any(l => (int)l == LevelId))
            {
                return false;
            }

            if (SystemId.HasValue && !TypeExtensions.Enumerate<Engine>().Any(e => (int)e == SystemId.Value))
            {
                return false;
            }

            Game? game = Stage.Get().FirstOrDefault(s => s.Id == StageId)?.Game;
            if (!game.HasValue)
            {
                return false;
            }

            if (Date.HasValue && (Date.Value.Date > DateTime.Today || Date.Value.Year < game.Value.GetFirstYear()))
            {
                return false;
            }

            return true;
        }
    }
}
