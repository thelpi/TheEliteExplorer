using System;
using System.Linq;
using TheEliteExplorerCommon;

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
            if (Id <= 0 || PlayerId <= 0 || Time <= 0)
            {
                return false;
            }

            if (!SystemExtensions.Enumerate<Level>().Any(l => (int)l == LevelId))
            {
                return false;
            }

            if (SystemId.HasValue && !SystemExtensions.Enumerate<Engine>().Contains((Engine)SystemId))
            {
                return false;
            }

            Game? game = Stage.Get().FirstOrDefault(s => s.Id == StageId)?.Game;
            if (!game.HasValue)
            {
                return false;
            }

            if (!game.Value.InGameLifeSpan(Date))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public EntryDto() { }

        /// <summary>
        /// Constructor from a <see cref="EntryRequest"/> instance and a player identifier.
        /// </summary>
        /// <param name="request">Instance of <see cref="EntryRequest"/>.</param>
        /// <param name="playerId">Player identifier</param>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerId"/> is below <c>1</c>.</exception>
        public EntryDto(EntryRequest request, long playerId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (playerId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), playerId, $"{nameof(playerId)} is below 1");
            }

            PlayerId = playerId;
            StageId = request.StageId;
            LevelId = request.LevelId;
            Date = request.Date;
            Time = request.Time;
            SystemId = request.EngineId;
        }

        /// <summary>
        /// Inferred; gets the game related to the entry, if any.
        /// </summary>
        /// <returns>The game, or <c>Null</c>.</returns>
        public Game? Game
        {
            get
            {
                return Stage.Get().FirstOrDefault(s => s.Id == StageId)?.Game;
            }
        }
    }
}
