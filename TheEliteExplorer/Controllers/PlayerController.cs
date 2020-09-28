using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheEliteExplorer.Domain;
using TheEliteExplorer.Infrastructure;

namespace TheEliteExplorer.Controllers
{
    /// <summary>
    /// Player controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Route("[controller]")]
    public class PlayerController : Controller
    {
        private readonly ISqlContext _sqlContext;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sqlContext">Instance of <see cref="ISqlContext"/>.</param>
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
        public async Task<IReadOnlyCollection<Player>> GetPlayersAsync([FromQuery] int page, [FromQuery] int count)
        {
            page = PaginationHelper.EnsurePage(page);
            count = PaginationHelper.EnsureCount(count);

            var dtos = await _sqlContext.GetPlayersAsync().ConfigureAwait(false);

            return dtos.Skip((page - 1) * count).Take(count).Select(dto => new Player(dto)).ToList();
        }
    }
}
