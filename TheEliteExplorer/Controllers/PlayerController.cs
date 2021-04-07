using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Models;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Player controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Route("players")]
    public class PlayerController : Controller
    {
        private readonly ISqlContext _sqlContext;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sqlContext"/> is <c>Null</c>.</exception>
        public PlayerController(ISqlContext sqlContext)
        {
            _sqlContext = sqlContext ?? throw new ArgumentNullException(nameof(sqlContext));
        }

        /// <summary>
        /// Gets every players, paginated.
        /// </summary>
        /// <param name="page">Current page (start at <c>1</c>).</param>
        /// <param name="count">Results count to display.</param>
        /// <returns>Collection of players.</returns>
        [HttpGet]
        public async Task<PaginatedCollection<Player>> GetPlayersAsync([FromQuery] int page, [FromQuery] int count)
        {
            var dtos = await _sqlContext.GetPlayersAsync().ConfigureAwait(false);

            return PaginatedCollection<Player>.CreateInstance(dtos, dto => new Player(dto), page, count);
        }

        /// <summary>
        /// Cleans duplicate players.
        /// </summary>
        /// <returns>Nothing.</returns>
        [HttpPatch("duplicates")]
        public async Task CleanDuplicatePlayerAsync()
        {
            var duplicatePlayers = await _sqlContext
                .GetDuplicatePlayersAsync()
                .ConfigureAwait(false);

            DuplicatePlayerDto basePlayer = null;
            foreach (var p in duplicatePlayers)
            {
                if (!p.UrlName.Equals(basePlayer?.UrlName, StringComparison.InvariantCultureIgnoreCase))
                {
                    basePlayer = p;
                }
                else
                {
                    await _sqlContext.UpdatePlayerEntriesAsync(p.Id, basePlayer.Id);
                    await _sqlContext.DeletePlayerAsync(p.Id);
                }
            }
        }
    }
}
