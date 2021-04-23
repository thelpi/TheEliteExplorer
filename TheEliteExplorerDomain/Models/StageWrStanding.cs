using System;
using System.Collections.Generic;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a world record standing.
    /// </summary>
    public class StageWrStanding
    {
        private readonly DateTime _atDate;

        /// <summary>
        /// Level.
        /// </summary>
        public Level Level { get; }
        /// <summary>
        /// Stage.
        /// </summary>
        public Stage Stage { get; }
        /// <summary>
        /// Date when the WR has been set.
        /// </summary>
        public DateTime StartDate { get; }
        /// <summary>
        /// Date when the world record has become the longest standing.
        /// </summary>
        public DateTime? StandingStartDate { get; internal set; }
        /// <summary>
        /// Date whent he WR has been beaten.
        /// </summary>
        public DateTime? EndDate { get; protected set; }
        /// <summary>
        /// Time.
        /// </summary>
        public long Time { get; }
        /// <summary>
        /// Player who sets the WR.
        /// </summary>
        public string StartPlayerName { get; }
        /// <summary>
        /// Player who beats the WR; <c>Null</c> if no one.
        /// </summary>
        public string EndPlayerName { get; protected set; }

        /// <summary>
        /// Days while being the WR.
        /// </summary>
        public int Days => (int)(EndDate.GetValueOrDefault(_atDate) - StartDate).TotalDays;
        /// <summary>
        /// Days while being the WR standing.
        /// </summary>
        public int StandingDays => StandingStartDate.HasValue
            ? (int)(EndDate.GetValueOrDefault(_atDate) - StandingStartDate.Value).TotalDays
            : 0;
        /// <summary>
        /// Entry still the WR y/n.
        /// </summary>
        public bool StillWr => !EndDate.HasValue;

        internal StageWrStanding(WrDto dto, IDictionary<long, PlayerDto> players, DateTime? atDate)
        {
            Level = dto.Level;
            Stage = dto.Stage;
            Time = dto.Time;
            StartDate = dto.Date;
            StartPlayerName = players[dto.PlayerId].RealName;
            _atDate = atDate ?? ServiceProviderAccessor.ClockProvider.Now;
        }

        internal virtual void StopStanding(WrDto dto, IDictionary<long, PlayerDto> players)
        {
            EndDate = dto.Date;
            EndPlayerName = players[dto.PlayerId].RealName;
        }
    }
}
