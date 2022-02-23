using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a world record base.
    /// </summary>
    public class WrBase
    {
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; }

        /// <summary>
        /// Level.
        /// </summary>
        public Level Level { get; }

        /// <summary>
        /// Time (seconds).
        /// </summary>
        public long Time { get; }

        internal WrBase(Stage stage, Level level, long time)
        {
            Stage = stage;
            Level = level;
            Time = time;
        }
    }
}
