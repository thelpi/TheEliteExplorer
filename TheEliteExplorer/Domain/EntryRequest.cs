using System;

namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Represents a time entry to process.
    /// </summary>
    public class EntryRequest
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
        /// Player URL name.
        /// </summary>
        public string PlayerUrlName { get; set; }
        /// <summary>
        /// Time.
        /// </summary>
        public long? Time { get; set; }
        /// <summary>
        /// Date.
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// Engine identifier.
        /// </summary>
        public long? EngineId { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <param name="level">Level.</param>
        /// <param name="playerUrlName">Player URL name.</param>
        /// <param name="time">Time.</param>
        /// <param name="date">Date.</param>
        /// <param name="engine">Engine.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stage"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="playerUrlName"/> is empty, <c>Null</c> or white spaces only.</exception>
        /// <exception cref="ArgumentException"><paramref name="date"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="time"/> is invalid.</exception>
        public EntryRequest(Stage stage, Level level, string playerUrlName,
            long? time, DateTime? date, Engine? engine)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            if (string.IsNullOrWhiteSpace(playerUrlName))
            {
                throw new ArgumentNullException(nameof(playerUrlName));
            }

            if (date.HasValue && (date.Value.Date > DateTime.Today || date.Value.Year < stage.Game.GetFirstYear()))
            {
                throw new ArgumentException(nameof(date));
            }

            if (time.HasValue && time.Value <= 0)
            {
                throw new ArgumentException(nameof(time));
            }

            Date = date;
            EngineId = engine.HasValue ? (long)engine.Value : default(long?);
            LevelId = (long)level;
            PlayerUrlName = playerUrlName;
            StageId = stage.Id;
            Time = time;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Concat(StageId, ", ", LevelId, ", ", PlayerUrlName, ", ", Date, ", ", EngineId);
        }
    }
}
