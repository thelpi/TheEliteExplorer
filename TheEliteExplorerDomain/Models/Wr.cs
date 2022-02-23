using System;
using System.Collections.Generic;
using System.Linq;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorerDomain.Models
{
    /// <summary>
    /// Represents a world record.
    /// </summary>
    public class Wr : WrBase
    {
        private readonly List<(PlayerDto, DateTime, Engine?)> _holders = new List<(PlayerDto, DateTime, Engine?)>();

        /// <summary>
        /// Player.
        /// </summary>
        public PlayerDto Player => _holders[0].Item1;

        /// <summary>
        /// Date.
        /// </summary>
        public DateTime Date => _holders[0].Item2;

        /// <summary>
        /// Engine.
        /// </summary>
        public Engine? Engine => _holders[0].Item3;

        /// <summary>
        /// Player who slays the untied wr (<c>Null</c> if still untied wr).
        /// </summary>
        public PlayerDto UntiedSlayPlayer => _holders.Count > 1 ? _holders[1].Item1 : default(PlayerDto);

        /// <summary>
        /// Date of untied slay (<c>Null</c> if still untied wr).
        /// </summary>
        public DateTime? UntiedSlayDate => _holders.Count > 1 ? _holders[1].Item2 : default(DateTime?);

        /// <summary>
        /// Every players who holds the wr.
        /// </summary>
        public IReadOnlyCollection<(PlayerDto, DateTime, Engine?)> Holders => _holders;

        /// <summary>
        /// Date of slay (<c>Null</c> if still wr).
        /// </summary>
        public DateTime? SlayDate { get; private set; }

        /// <summary>
        /// Player who slays the wr (<c>Null</c> if still wr).
        /// </summary>
        public PlayerDto SlayPlayer { get; private set; }

        internal Wr(Stage stage, Level level, long time, PlayerDto player, DateTime date, Engine? engine)
            : base(stage, level, time)
        {
            _holders.Add((player, date.Date, engine));
        }

        internal void AddHolder(PlayerDto player, DateTime date, Engine? engine)
        {
            // Avoid duplicate of same player on multiple engines
            if (_holders.Any(h => h.Item1 == player)) return;

            _holders.Add((player, date.Date, engine));
        }

        internal void AddSlayer(PlayerDto player, DateTime date)
        {
            SlayPlayer = player;
            SlayDate = date.Date;
        }

        internal bool CheckAmbiguousHolders(int i)
        {
            return _holders.Count > i && _holders[i - 1].Item2 == _holders[i].Item2;
        }

        /// <summary>
        /// Gets the number of days this time has been a (untied) wr.
        /// </summary>
        /// <param name="date">Reference date.</param>
        /// <param name="untied"><c>True</c> to check untied.</param>
        /// <returns>Number of days.</returns>
        public int GetDaysBeforeSlay(DateTime date, bool untied)
        {
            var endDate = date.Date;

            if (untied && UntiedSlayDate.HasValue && UntiedSlayDate < date)
                endDate = UntiedSlayDate.Value;
            else if (!untied && SlayDate.HasValue && SlayDate < date)
                endDate = SlayDate.Value;

            return (int)Math.Floor((endDate - Date).TotalDays);
        }

        // TODO: ugly
        internal WrBase ToBase()
        {
            return new WrBase(Stage, Level, Time);
        }
    }
}
