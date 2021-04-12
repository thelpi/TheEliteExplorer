using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain
{
    /// <summary>
    /// Extension methods and tools.
    /// </summary>
    public static class Extensions
    {
        private const int GoldeneyeStagesCount = 20;

        /// <summary>
        /// Default label for unknown data.
        /// </summary>
        public const string DefaultLabel = "Unknown";

        private static readonly Dictionary<Game, string> _eliteUrlName = new Dictionary<Game, string>
        {
            { Game.GoldenEye, "goldeneye" },
            { Game.PerfectDark, "perfect-dark" } 
        };
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
        private static readonly Dictionary<Stage, string> _stageLabels = new Dictionary<Stage, string>
        {
            { Stage.Dam, "Dam" },
            { Stage.Facility, "Facility" },
            { Stage.Runway, "Runway" },
            { Stage.Surface1, "Surface 1" },
            { Stage.Bunker1, "Bunker 1" },
            { Stage.Silo, "Silo" },
            { Stage.Frigate, "Frigate" },
            { Stage.Surface2, "Surface 2" },
            { Stage.Bunker2, "Bunker 2" },
            { Stage.Statue, "Statue" },
            { Stage.Archives, "Archives" },
            { Stage.Streets, "Streets" },
            { Stage.Depot, "Depot" },
            { Stage.Train, "Train" },
            { Stage.Jungle, "Jungle" },
            { Stage.Control, "Control" },
            { Stage.Caverns, "Caverns" },
            { Stage.Cradle, "Cradle" },
            { Stage.Aztec, "Aztec" },
            { Stage.Egypt, "Egypt" },
            // TODO: might be invalid from here
            { Stage.Defection, "dataDyne Central - Defection" },
            { Stage.Investigation, "dataDyne Research - Investigation" },
            { Stage.Extraction, "dataDyne Central - Extraction" },
            { Stage.Villa, "Carrington Villa - Hostage One" },
            { Stage.Chicago, "Chicago - Stealth" },
            { Stage.G5, "G5 Building - Reconnaissance" },
            { Stage.Infiltration, "Area 51 - Infiltration" },
            { Stage.Rescue, "Area 51 - Rescue" },
            { Stage.Escape, "Area 51 - Escape" },
            { Stage.AirBase, "Air Base - Espionage" },
            { Stage.AirForceOne, "Air Force One - Antiterrorism" },
            { Stage.CrashSite, "Crash Site - Confrontation" },
            { Stage.PelagicII, "Pelagic II - Exploration" },
            { Stage.DeepSea, "Deep Sea - Nullify Threat" },
            { Stage.CI, "Carrington Institute - Defense" },
            { Stage.AttackShip, "Attack Ship - Covert Assault" },
            { Stage.SkedarRuins, "Skedar Ruins - Battle Shrine" },
            { Stage.MBR, "Mr. Blonde's Revenge" },
            { Stage.MaianSOS, "Maian SOS" },
            { Stage.War, "WAR!" },
        };

        /// <summary>
        /// Tries to get a stage from its label.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="stageLabel">Stage label.</param>
        /// <returns>Stage or <c>Null</c>.</returns>
        public static Stage? GetStageFromLabel(this Game game, string stageLabel)
        {
            var matches = _stageLabels.Where(_ =>
                _.Value.Equals(stageLabel, StringComparison.InvariantCultureIgnoreCase));

            return matches.Any() ? matches.First().Key : default(Stage?);
        }

        /// <summary>
        /// Tries to get a level from its label.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="levelLabel">Level label.</param>
        /// <returns>Level or <c>Null</c>.</returns>
        public static Level? GetLevelFromLabel(this Game game, string levelLabel)
        {
            var matches = _levelLabels.Where(_ =>
                _.Value.Equals(levelLabel, StringComparison.InvariantCultureIgnoreCase));

            return matches.Any() ? matches.First().Key.Item1 : default(Level?);
        }

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
        /// Gets the elite URL name part for the specified game.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <returns>URL name part.</returns>
        public static string GetGameUrlName(this Game game)
        {
            return _eliteUrlName[game];
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

        /// <summary>
        /// Gets every stages of the specified game.
        /// </summary>
        /// <param name="game">The <see cref="Game"/>.</param>
        /// <returns>Collection of <see cref="Stage"/>.</returns>
        public static IReadOnlyCollection<Stage> GetStages(this Game game)
        {
            if (game == Game.GoldenEye)
            {
                return SystemExtensions.Enumerate<Stage>()
                    .Where(s => (int)s <= GoldeneyeStagesCount)
                    .ToList();
            }
            else
            {
                return SystemExtensions.Enumerate<Stage>()
                    .Where(s => (int)s > GoldeneyeStagesCount)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the game related to a stage.
        /// </summary>
        /// <param name="stage">Stage.</param>
        /// <returns>Game.</returns>
        public static Game GetGame(this Stage stage)
        {
            return ((int)stage) <= GoldeneyeStagesCount
                ? Game.GoldenEye
                : Game.PerfectDark;
        }
    }
}
