using System;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    public class Player
    {
        // Virtual date where submitted times should have a date, except for newcomers.
        internal static readonly DateTime LastEmptyDate = new DateTime(2013, 1, 1);
        // Default player's hexadecimal color.
        internal const string DefaultPlayerHexColor = "000000";

        /// <summary>
        /// Identifier.
        /// </summary>
        public long Id { get; }
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
        /// Hexadecimal color.
        /// </summary>
        public string Color { get; }

        internal Player(PlayerDto dto)
        {
            Id = dto.Id;
            RealName = dto.RealName;
            SurName = dto.SurName;
            ControlStyle = Extensions.ToControlStyle(dto.ControlStyle);
            Color = dto.Color;
        }
    }
}
