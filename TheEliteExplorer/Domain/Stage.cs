namespace TheEliteExplorer.Domain
{
    /// <summary>
    /// Represents a stage.
    /// </summary>
    public class Stage
    {
        private const int GoldeneyeStagesCount = 20;
        private long Id
        {
            get
            {
                return Game == Game.PerfectDark ? Position + GoldeneyeStagesCount : Position;
            }
        }

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

        private Stage(Game game, string name, int position)
        {
            Game = game;
            Name = name;
            Position = position;
        }

        /// <summary>
        /// Dam, GoldenEye first stage.
        /// </summary>
        public static Stage Dam { get; } = new Stage(Game.GoldenEye, "Dam", 1);
        /// <summary>
        /// Dam, GoldenEye second stage.
        /// </summary>
        public static Stage Facility { get; } = new Stage(Game.GoldenEye, "Facility", 2);
        /// <summary>
        /// Dam, GoldenEye third stage.
        /// </summary>
        public static Stage Runway { get; } = new Stage(Game.GoldenEye, "Runway", 3);
        /// <summary>
        /// Dam, GoldenEye 4th stage.
        /// </summary>
        public static Stage Surface1 { get; } = new Stage(Game.GoldenEye, "Surface 1", 4);
        /// <summary>
        /// Dam, GoldenEye 5th stage.
        /// </summary>
        public static Stage Bunker1 { get; } = new Stage(Game.GoldenEye, "Bunker 1", 5);
        /// <summary>
        /// Dam, GoldenEye 6th stage.
        /// </summary>
        public static Stage Silo { get; } = new Stage(Game.GoldenEye, "Silo", 6);
        /// <summary>
        /// Dam, GoldenEye 7th stage.
        /// </summary>
        public static Stage Frigate { get; } = new Stage(Game.GoldenEye, "Frigate", 7);
        /// <summary>
        /// Dam, GoldenEye 8th stage.
        /// </summary>
        public static Stage Surface2 { get; } = new Stage(Game.GoldenEye, "Surface 2", 8);
        /// <summary>
        /// Dam, GoldenEye 9th stage.
        /// </summary>
        public static Stage Bunker2 { get; } = new Stage(Game.GoldenEye, "Bunker 2", 9);
        /// <summary>
        /// Dam, GoldenEye 10th stage.
        /// </summary>
        public static Stage Statue { get; } = new Stage(Game.GoldenEye, "Statue", 10);
        /// <summary>
        /// Dam, GoldenEye 11th stage.
        /// </summary>
        public static Stage Archives { get; } = new Stage(Game.GoldenEye, "Archives", 11);
        /// <summary>
        /// Dam, GoldenEye 12ht stage.
        /// </summary>
        public static Stage Streets { get; } = new Stage(Game.GoldenEye, "Streets", 12);
        /// <summary>
        /// Dam, GoldenEye 13ht stage.
        /// </summary>
        public static Stage Depot { get; } = new Stage(Game.GoldenEye, "Depot", 13);
        /// <summary>
        /// Dam, GoldenEye 14th stage.
        /// </summary>
        public static Stage Train { get; } = new Stage(Game.GoldenEye, "Train", 14);
        /// <summary>
        /// Dam, GoldenEye 15th stage.
        /// </summary>
        public static Stage Jungle { get; } = new Stage(Game.GoldenEye, "Jungle", 15);
        /// <summary>
        /// Dam, GoldenEye 16th stage.
        /// </summary>
        public static Stage Control { get; } = new Stage(Game.GoldenEye, "Control", 16);
        /// <summary>
        /// Dam, GoldenEye 17th stage.
        /// </summary>
        public static Stage Caverns { get; } = new Stage(Game.GoldenEye, "Caverns", 17);
        /// <summary>
        /// Dam, GoldenEye 18th stage.
        /// </summary>
        public static Stage Cradle { get; } = new Stage(Game.GoldenEye, "Cradle", 18);
        /// <summary>
        /// Dam, GoldenEye 19th stage.
        /// </summary>
        public static Stage Aztec { get; } = new Stage(Game.GoldenEye, "Aztec", 19);
        /// <summary>
        /// Dam, GoldenEye 20th stage.
        /// </summary>
        public static Stage Egypt { get; } = new Stage(Game.GoldenEye, "Egypt", 20);
    }
}
