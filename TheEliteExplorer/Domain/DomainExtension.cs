﻿using System.Collections.Generic;

namespace TheEliteExplorer.Domain
{
    internal static class DomainExtension
    {
        private static readonly Dictionary<Game, int> _firstYear = new Dictionary<Game, int>
        {
            { Game.GoldenEye, 1998 },
            { Game.PerfectDark, 2000 }
        };
        internal const string DefaultLabel = "Unknown";

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

        internal static string GetLabel(this Level level, Game game)
        {
            return _levelLabels.ContainsKey((level, game)) ?
                _levelLabels[(level, game)] : DefaultLabel;
        }

        internal static ControlStyle? ToControlStyle(string controlStyleLabel)
        {
            return controlStyleLabel != null && _controlStyleConverters.ContainsKey(controlStyleLabel) ?
                _controlStyleConverters[controlStyleLabel] : default(ControlStyle?);
        }

        internal static int GetFirstYear(this Game game)
        {
            return _firstYear.ContainsKey(game) ?
                _firstYear[game] : 0;
        }
    }
}
