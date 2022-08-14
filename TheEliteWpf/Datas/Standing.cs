using System;
using System.Collections.Generic;

namespace TheEliteWpf.Datas
{
    public class Standing
    {
        public Stage Stage { get; set; }
        public Level Level { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Player Author { get; set; }
        public Player Slayer { get; set; }
        public IReadOnlyCollection<long> Times { get; set; }
        public int Days { get; set; }

        public override string ToString()
        {
            var datas = new[]
            {
                $"{Stage} - {Level}",
                Author.ToString((int)Stage > 20 ? Game.PerfectDark : Game.GoldenEye),
                $"{Days} {(Days <= 1 ? "day" : "days")}",
                $"From {StartDate:yyyy-MM-dd} {(EndDate.HasValue ? $"to {EndDate.Value:yyyy-MM-dd}" : "(ongoing)")}"
            };
            return string.Join('\n', datas);
        }
    }
}
