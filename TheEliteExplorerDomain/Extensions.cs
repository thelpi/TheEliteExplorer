using System;
using System.Collections.Generic;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Enums;

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

        private static readonly Dictionary<Game, DateTime> _eliteBeginDate = new Dictionary<Game, DateTime>
        {
            { Game.GoldenEye, new DateTime(1998, 07, 26) },
            { Game.PerfectDark, new DateTime(1998, 07, 26) } // TODO !
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
            { "1.2", ControlStyle.OnePointTwo },
            { "1.3", ControlStyle.OnePointThree },
            { "1.4", ControlStyle.OnePointFour },
            { "2.1", ControlStyle.TwoPointOne },
            { "2.2", ControlStyle.TwoPointTwo },
            { "2.3", ControlStyle.TwoPointThree },
            { "2.4", ControlStyle.TwoPointFour }
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
        /// Gets the elite beginning date for the specified game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>Elite beginning date.</returns>
        public static DateTime GetEliteFirstDate(this Game game)
        {
            return _eliteBeginDate[game];
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
                date?.Date <= ServiceProviderAccessor.ClockProvider.Now.Date
                && date?.Date >= game.GetEliteFirstDate()
            );
        }
    }
}
