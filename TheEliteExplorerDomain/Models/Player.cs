using System;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    // TODO: complete this class
    /// <summary>
    /// Represents a player.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Virtual date where submitted times should have a date, except for newcomers.
        /// </summary>
        public static readonly DateTime LastEmptyDate = new DateTime(2013, 1, 1);
        /// <summary>
        /// Default player's hexadecimal color.
        /// </summary>
        public static readonly string DefaultPlayerHexColor = "000000";

        /// <summary>
        /// Real name.
        /// </summary>
        public string RealName { get; }
        /// <summary>
        /// Surname.
        /// </summary>
        public string SurName { get; }
        /// <summary>
        /// Control style.
        /// </summary>
        public ControlStyle? ControlStyle { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dto">Instance of <see cref="PlayerDto"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dto"/> is <c>Null</c>.</exception>
        public Player(PlayerDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            RealName = dto.RealName;
            SurName = dto.SurName;
            ControlStyle = Extensions.ToControlStyle(dto.ControlStyle);
        }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"{SurName} - {RealName}";
        }
    }
}
