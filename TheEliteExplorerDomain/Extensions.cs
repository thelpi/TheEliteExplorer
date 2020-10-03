﻿using System;
using System.Collections.Generic;
using TheEliteExplorerCommon;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Extension methods and tools.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Default label for unknown data.
        /// </summary>
        public const string DefaultLabel = "Unknown";

        private static readonly Dictionary<Game, int> _firstYear = new Dictionary<Game, int>
        {
            { Game.GoldenEye, 1998 },
            { Game.PerfectDark, 2000 }
        };
        private static readonly Dictionary<(Level, Game), string> _levelLabels = new Dictionary<(Level, Game), string>
        {
            { (Level.Easy, Game.GoldenEye), "Agent" },
            { (Level.Medium, Game.GoldenEye), "Secret agent" },
            { (Level.Hard, Game.GoldenEye), "00 agent" },
            { (Level.Easy, Game.PerfectDark), "Agent" },
            { (Level.Medium, Game.PerfectDark), "Special agent" },
            { (Level.Hard, Game.PerfectDark), "Perfect agent" },
        };
        private static readonly Dictionary<string, ControlStyle> _controlStyleConverters = new Dictionary<string, ControlStyle>
        {
            { "1.1", ControlStyle.OnePointOne },
            { "1.2", ControlStyle.OnePointTwo }
        };

        /// <summary>
        /// Gets the label associated to a level for specified game.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="game">The game.</param>
        /// <returns>The label.</returns>
        public static string GetLabel(this Level level, Game game)
        {
            return _levelLabels.ContainsKey((level, game)) ?
                _levelLabels[(level, game)] : DefaultLabel;
        }

        /// <summary>
        /// Tries to transform a string representing the control style into a <see cref="ControlStyle"/>.
        /// </summary>
        /// <param name="controlStyleLabel">The control style label.</param>
        /// <returns>The <see cref="ControlStyle"/> value or <c>Null</c>.</returns>
        public static ControlStyle? ToControlStyle(string controlStyleLabel)
        {
            return controlStyleLabel != null && _controlStyleConverters.ContainsKey(controlStyleLabel) ?
                _controlStyleConverters[controlStyleLabel] : default(ControlStyle?);
        }

        /// <summary>
        /// Gets the release year of the game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>Release year.</returns>
        public static int GetReleaseYear(this Game game)
        {
            return _firstYear.ContainsKey(game) ?
                _firstYear[game] : 0;
        }

        /// <summary>
        /// Checks if a date is in the life span of the game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="date">The date.</param>
        /// <returns><c>True</c> if the date is in the lifespan, or <c>Null</c>; <c>False</c> otherwise.</returns>
        public static bool InGameLifeSpan(this Game game, DateTime? date)
        {
            return !date.HasValue || (
                date.Value.Date <= ServiceProviderAccessor.ClockProvider.Now.Date
                && date.Value.Year >= game.GetReleaseYear()
            );
        }
    }
}
