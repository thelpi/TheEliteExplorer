using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a stage.
    /// </summary>
    public class Stage
    {
        private const int GoldeneyeStagesCount = 20;

        /// <summary>
        /// Game.
        /// </summary>
        public Game Game { get; }
        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Position in the game narrative (starts at <c>1</c>).
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Inferred; name of the stage without spaces and full lowercase.
        /// </summary>
        public string FormatedName { get { return Name.Replace(" ", string.Empty).ToLowerInvariant(); } }
        /// <summary>
        /// Inferred; stage technical identifier.
        /// </summary>
        public long Id
        {
            get
            {
                return Game == Game.PerfectDark ? Position + GoldeneyeStagesCount : Position;
            }
        }

        /// <summary>
        /// Gets every stages of the specified game.
        /// </summary>
        /// <param name="game">The <see cref="Game"/>.</param>
        /// <returns>Collection of <see cref="Stage"/>.</returns>
        public static IReadOnlyCollection<Stage> Get(Game game)
        {
            if (game == Game.GoldenEye)
            {
                return new List<Stage>
                {
                    Dam, Facility, Runway, Surface1, Bunker1,
                    Silo, Frigate, Surface2, Bunker2, Statue,
                    Archives, Streets, Depot, Train, Jungle,
                    Control, Caverns, Cradle, Aztec, Egypt
                };
            }
            else
            {
                return new List<Stage>
                {
                    Defection, Investigation, Extraction, Villa, Chicago,
                    G5, Infiltration, Rescue, Escape, AirBase,
                    AirForceOne, CrashSite, PelagicII, DeepSea, CI,
                    AttackShip, SkedarRuins, MBR, MaianSOS, WAR
                };
            }
        }

        /// <summary>
        /// Gets every stages of every games.
        /// </summary>
        /// <returns>Collection of <see cref="Stage"/>.</returns>
        public static IReadOnlyCollection<Stage> Get()
        {
            return SystemExtensions.Enumerate<Game>().SelectMany(Get).ToList();
        }

        private Stage(Game game, string name, int position)
        {
            Game = game;
            Name = name;
            Position = position;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

        #region GoldenEye stages instances

        /// <summary>
        /// Dam, GoldenEye first stage.
        /// </summary>
        public static Stage Dam { get; } = new Stage(Game.GoldenEye, "Dam", 1);
        /// <summary>
        /// Facility, GoldenEye second stage.
        /// </summary>
        public static Stage Facility { get; } = new Stage(Game.GoldenEye, "Facility", 2);
        /// <summary>
        /// Runway, GoldenEye third stage.
        /// </summary>
        public static Stage Runway { get; } = new Stage(Game.GoldenEye, "Runway", 3);
        /// <summary>
        /// Surface 1, GoldenEye 4th stage.
        /// </summary>
        public static Stage Surface1 { get; } = new Stage(Game.GoldenEye, "Surface 1", 4);
        /// <summary>
        /// Bunker 1, GoldenEye 5th stage.
        /// </summary>
        public static Stage Bunker1 { get; } = new Stage(Game.GoldenEye, "Bunker 1", 5);
        /// <summary>
        /// Silo, GoldenEye 6th stage.
        /// </summary>
        public static Stage Silo { get; } = new Stage(Game.GoldenEye, "Silo", 6);
        /// <summary>
        /// Frigate, GoldenEye 7th stage.
        /// </summary>
        public static Stage Frigate { get; } = new Stage(Game.GoldenEye, "Frigate", 7);
        /// <summary>
        /// Surface 2, GoldenEye 8th stage.
        /// </summary>
        public static Stage Surface2 { get; } = new Stage(Game.GoldenEye, "Surface 2", 8);
        /// <summary>
        /// Bunker 2, GoldenEye 9th stage.
        /// </summary>
        public static Stage Bunker2 { get; } = new Stage(Game.GoldenEye, "Bunker 2", 9);
        /// <summary>
        /// Statue, GoldenEye 10th stage.
        /// </summary>
        public static Stage Statue { get; } = new Stage(Game.GoldenEye, "Statue", 10);
        /// <summary>
        /// Archives, GoldenEye 11th stage.
        /// </summary>
        public static Stage Archives { get; } = new Stage(Game.GoldenEye, "Archives", 11);
        /// <summary>
        /// Streets, GoldenEye 12ht stage.
        /// </summary>
        public static Stage Streets { get; } = new Stage(Game.GoldenEye, "Streets", 12);
        /// <summary>
        /// Depot, GoldenEye 13ht stage.
        /// </summary>
        public static Stage Depot { get; } = new Stage(Game.GoldenEye, "Depot", 13);
        /// <summary>
        /// Train, GoldenEye 14th stage.
        /// </summary>
        public static Stage Train { get; } = new Stage(Game.GoldenEye, "Train", 14);
        /// <summary>
        /// Jungle, GoldenEye 15th stage.
        /// </summary>
        public static Stage Jungle { get; } = new Stage(Game.GoldenEye, "Jungle", 15);
        /// <summary>
        /// Control, GoldenEye 16th stage.
        /// </summary>
        public static Stage Control { get; } = new Stage(Game.GoldenEye, "Control", 16);
        /// <summary>
        /// Caverns, GoldenEye 17th stage.
        /// </summary>
        public static Stage Caverns { get; } = new Stage(Game.GoldenEye, "Caverns", 17);
        /// <summary>
        /// Cradle, GoldenEye 18th stage.
        /// </summary>
        public static Stage Cradle { get; } = new Stage(Game.GoldenEye, "Cradle", 18);
        /// <summary>
        /// Aztec, GoldenEye 19th stage.
        /// </summary>
        public static Stage Aztec { get; } = new Stage(Game.GoldenEye, "Aztec", 19);
        /// <summary>
        /// Egypt, GoldenEye 20th stage.
        /// </summary>
        public static Stage Egypt { get; } = new Stage(Game.GoldenEye, "Egypt", 20);

        #endregion GoldenEye stages instances

        #region Perfect Dark stages instances

        /// <summary>
        /// dataDyne Central - Defection, Perfect Dark first stage.
        /// </summary>
        public static Stage Defection { get; } = new Stage(Game.PerfectDark, "dataDyne Central - Defection", 1);
        /// <summary>
        /// dataDyne Research - Investigation, Perfect Dark second stage.
        /// </summary>
        public static Stage Investigation { get; } = new Stage(Game.PerfectDark, "dataDyne Research - Investigation", 2);
        /// <summary>
        /// dataDyne Central - Extraction, Perfect Dark third stage.
        /// </summary>
        public static Stage Extraction { get; } = new Stage(Game.PerfectDark, "dataDyne Central - Extraction", 3);
        /// <summary>
        /// Carrington Villa - Hostage One, Perfect Dark 4th stage.
        /// </summary>
        public static Stage Villa { get; } = new Stage(Game.PerfectDark, "Carrington Villa - Hostage One", 4);
        /// <summary>
        /// Chicago - Stealth, Perfect Dark 5th stage.
        /// </summary>
        public static Stage Chicago { get; } = new Stage(Game.PerfectDark, "Chicago - Stealth", 5);
        /// <summary>
        /// G5 Building - Reconnaissance, Perfect Dark 6th stage.
        /// </summary>
        public static Stage G5 { get; } = new Stage(Game.PerfectDark, "G5 Building - Reconnaissance", 6);
        /// <summary>
        /// Area 51 - Infiltration, Perfect Dark 7th stage.
        /// </summary>
        public static Stage Infiltration { get; } = new Stage(Game.PerfectDark, "Area 51 - Infiltration", 7);
        /// <summary>
        /// Area 51 - Rescue, Perfect Dark 8th stage.
        /// </summary>
        public static Stage Rescue { get; } = new Stage(Game.PerfectDark, "Area 51 - Rescue", 8);
        /// <summary>
        /// Area 51 - Escape, Perfect Dark 9th stage.
        /// </summary>
        public static Stage Escape { get; } = new Stage(Game.PerfectDark, "Area 51 - Escape", 9);
        /// <summary>
        /// Air Base - Espionage, Perfect Dark 10th stage.
        /// </summary>
        public static Stage AirBase { get; } = new Stage(Game.PerfectDark, "Air Base - Espionage", 10);
        /// <summary>
        /// Air Force One - Antiterrorism, Perfect Dark 11th stage.
        /// </summary>
        public static Stage AirForceOne { get; } = new Stage(Game.PerfectDark, "Air Force One - Antiterrorism", 11);
        /// <summary>
        /// Crash Site - Confrontation, Perfect Dark 12ht stage.
        /// </summary>
        public static Stage CrashSite { get; } = new Stage(Game.PerfectDark, "Crash Site - Confrontation", 12);
        /// <summary>
        /// Pelagic II - Exploration, Perfect Dark 13ht stage.
        /// </summary>
        public static Stage PelagicII { get; } = new Stage(Game.PerfectDark, "Pelagic II - Exploration", 13);
        /// <summary>
        /// Deep Sea - Nullify Threat, Perfect Dark 14th stage.
        /// </summary>
        public static Stage DeepSea { get; } = new Stage(Game.PerfectDark, "Deep Sea - Nullify Threat", 14);
        /// <summary>
        /// Carrington Institute - Defense, Perfect Dark 15th stage.
        /// </summary>
        public static Stage CI { get; } = new Stage(Game.PerfectDark, "Carrington Institute - Defense", 15);
        /// <summary>
        /// Attack Ship - Covert Assault, Perfect Dark 16th stage.
        /// </summary>
        public static Stage AttackShip { get; } = new Stage(Game.PerfectDark, "Attack Ship - Covert Assault", 16);
        /// <summary>
        /// Skedar Ruins - Battle Shrine, GoldenEye 17th stage.
        /// </summary>
        public static Stage SkedarRuins { get; } = new Stage(Game.PerfectDark, "Skedar Ruins - Battle Shrine", 17);
        /// <summary>
        /// Mr. Blonde's Revenge, Perfect Dark 18th stage.
        /// </summary>
        public static Stage MBR { get; } = new Stage(Game.PerfectDark, "Mr. Blonde's Revenge", 18);
        /// <summary>
        /// Maian SOS, Perfect Dark 19th stage.
        /// </summary>
        public static Stage MaianSOS { get; } = new Stage(Game.PerfectDark, "Maian SOS", 19);
        /// <summary>
        /// WAR!, Perfect Dark 20th stage.
        /// </summary>
        public static Stage WAR { get; } = new Stage(Game.PerfectDark, "WAR!", 20);

        #endregion Perfect Dark stages instances
    }
}
