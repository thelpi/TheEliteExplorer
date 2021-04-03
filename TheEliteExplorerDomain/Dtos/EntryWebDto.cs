using System;

namespace TheEliteExplorerDomain.Dtos
{
    /// <summary>
    /// Represents a time entry to process from the web datas.
    /// </summary>
    public class EntryWebDto : EntryBaseDto
    {
        /// <summary>
        /// Player URL name.
        /// </summary>
        public string PlayerUrlName { get; set; }

        /// <summary>
        /// Transforms the instance into <see cref="EntryDto"/>.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <returns>Instance of <see cref="EntryDto"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="playerId"/> value.</exception>
        public EntryDto ToEntry(long playerId)
        {
            if (playerId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), playerId, $"{nameof(playerId)} is below 1");
            }

            return new EntryDto
            {
                PlayerId = playerId,
                StageId = StageId,
                LevelId = LevelId,
                Date = Date,
                Time = Time,
                SystemId = SystemId
            };
        }
    }
}
