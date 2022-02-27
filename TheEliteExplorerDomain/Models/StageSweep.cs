using System;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a sweep on stage (untied or not).
    /// </summary>
    public class StageSweep
    {
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; }

        /// <summary>
        /// Start date.
        /// </summary>
        public DateTime StartDate { get; }

        /// <summary>
        /// End date (exclusive).
        /// </summary>
        public DateTime EndDate { get; private set; }

        /// <summary>
        /// Player.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Days count.
        /// </summary>
        public int Days { get { return (int)(EndDate - StartDate).TotalDays; } }

        internal StageSweep(DateTime date, Stage stage, Dtos.PlayerDto playerDto)
        {
            Stage = stage;
            EndDate = date;
            StartDate = date;
            Player = new Player(playerDto);
        }

        internal void AddDay()
        {
            EndDate = EndDate.AddDays(1);
        }
    }
}
