using System;
using TheEliteExplorerDomain.Dtos;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    public class Player
    {
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
        /// <param name="dto">Player DTO.</param>
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
    }
}
