using System.Collections.Generic;

namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Extension methods.
    /// </summary>

    internal static class Extension
    {
        private static readonly Dictionary<(Level, Game), string> _levelLabels = new Dictionary<(Level, Game), string>
        {
            { (Level.Easy, Game.GoldenEye), "Agent" },
            { (Level.Medium, Game.GoldenEye), "Secret agent" },
            { (Level.Hard, Game.GoldenEye), "00 agent" },
            { (Level.Easy, Game.PerfectDark), "Agent" },
            { (Level.Medium, Game.PerfectDark), "Special agent" },
            { (Level.Hard, Game.PerfectDark), "Perfect agent" },
        };

        /// <summary>
        /// Gets the label associated to a <see cref="Level"/> for a specified <see cref="Game"/>.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="game">The game.</param>
        /// <returns>The label.</returns>
        internal static string GetLabel(this Level level, Game game)
        {
            return _levelLabels[(level, game)];
        }
    }
}
