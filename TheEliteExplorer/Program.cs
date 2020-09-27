using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace TheEliteExplorer
{
    /// <summary>
    /// Program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        /// <summary>
        /// Builds a web host.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Instance of <see cref="IWebHost"/>.</returns>
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
