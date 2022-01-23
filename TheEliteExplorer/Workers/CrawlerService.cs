using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Enums;

namespace TheEliteExplorer.Workers
{
    /// <summary>
    /// Baackground crawler service.
    /// </summary>
    /// <seealso cref="BackgroundService"/>
    public class CrawlerService : BackgroundService
    {
        private readonly IIntegrationProvider _integrationProvider;
        private readonly TimeSpan _delay;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="integrationProvider">Instance of <see cref="IIntegrationProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="integrationProvider"/> is <c>Null</c>.</exception>
        public CrawlerService(IIntegrationProvider integrationProvider)
        {
            _integrationProvider = integrationProvider ?? throw new ArgumentNullException(nameof(integrationProvider));
            _delay = new TimeSpan(2, 0, 0);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CrawlGameTimesAsync(Game.GoldenEye).ConfigureAwait(false);
                await CrawlGameTimesAsync(Game.PerfectDark).ConfigureAwait(false);

                await Task.Delay(_delay, stoppingToken);
            }
        }

        private async Task CrawlGameTimesAsync(Game game)
        {
            try
            {
                await _integrationProvider
                    .ScanTimePageAsync(game, null)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                using (var w = new StreamWriter($@"S:\iis_logs\api_global_app.log", true))
                {
                    w.WriteLine($"{DateTime.Now.ToString("yyyyMMddhhmmss")}\t{ex.Message}\t{ex.StackTrace}");
                }
            }
        }
    }
}
