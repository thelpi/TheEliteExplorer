using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerInfrastructure.Configuration;

namespace TheEliteExplorerInfrastructure
{
    /// <summary>
    /// The-elite website parse.
    /// </summary>
    /// <seealso cref="ITheEliteWebSiteParser"/>
    public class TheEliteWebSiteParser : ITheEliteWebSiteParser
    {
        private readonly TheEliteWebsiteConfiguration _configuration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> or its inner value is <c>Null</c>.</exception>
        public TheEliteWebSiteParser(IOptions<TheEliteWebsiteConfiguration> configuration)
        {
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyCollection<EntryRequest>, IReadOnlyCollection<string>)> ExtractTimeEntryAsync(Game game, int year, int month, DateTime? minimalDateToScan)
        {
            var linksValues = new List<EntryRequest>();
            var logs = new List<string>();

            string uri = string.Format(_configuration.HistoryPage, year, month);

            string historyContent = await GetPageStringContentAsync(uri, logs).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(historyContent))
            {
                return (linksValues, logs);
            }

            var htmlDoc = new HtmlDocument();
            try
            {
                htmlDoc.LoadHtml(historyContent);
            }
            catch (Exception ex)
            {
                logs.Add($"Error while parsing into HTML document the page string content - {ex.Message}");
                return (linksValues, logs);
            }

            const string timeClass = "time";

            HtmlNodeCollection links = htmlDoc.DocumentNode.SelectNodes("//a");
            foreach (HtmlNode link in links)
            {
                try
                {
                    bool useLink = link.Attributes.Contains("class")
                        && link.Attributes["class"].Value == timeClass;

                    if (useLink)
                    {
                        EntryRequest linkValues = await ExtractTimeLinkDetailsAsync(game, link, logs, minimalDateToScan).ConfigureAwait(false);
                        if (linkValues != null)
                        {
                            linksValues.Add(linkValues);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"Error while processing the node - {ex.Message}");
                }
            }

            return (linksValues, logs);
        }

        private async Task<EntryRequest> ExtractTimeLinkDetailsAsync(Game game, HtmlNode link, List<string> logs, DateTime? minimalDateToScan)
        {
            const char linkSeparator = '-';
            const string playerUrlPrefix = "/~";

            string[] linkParts = CleanString(link.InnerText)
                .Split(linkSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (linkParts.Length < 2)
            {
                return null;
            }

            DateTime? date = ExtractAndCheckDate(link, logs, minimalDateToScan, out bool exit);
            if (exit)
            {
                return null;
            }

            string stageName = linkParts[0].ToLowerInvariant().Replace(" ", string.Empty);
            var stage = Stage.Get(game).FirstOrDefault(g => g.FormatedName.Equals(stageName));
            if (stage == null)
            {
                return null;
            }

            string playerUrl = null;
            try
            {
                playerUrl = link.ParentNode.ParentNode.ChildNodes[3].ChildNodes.First().Attributes["href"].Value;
                playerUrl = playerUrl.Replace(playerUrlPrefix, string.Empty).Replace("+", " ");
            }
            catch (Exception ex)
            {
                logs.Add($"Unable to extract the player name - {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(playerUrl))
            {
                return null;
            }

            Level? level = SystemExtensions
                .Enumerate<Level>()
                .Select(l => (Level?)l)
                .FirstOrDefault(l => l.Value.GetLabel(game).Equals(linkParts[1], StringComparison.InvariantCultureIgnoreCase));

            if (!level.HasValue)
            {
                return null;
            }

            long? time = ExtractTime(linkParts[2], logs, out bool failToExtractTime);
            if (failToExtractTime)
            {
                return null;
            }
            
            return new EntryRequest(stage, level.Value, playerUrl, time, date,
                await ExtractTimeEntryEngineAsync(link, logs).ConfigureAwait(false));
        }

        private static DateTime? ExtractAndCheckDate(HtmlNode link, List<string> logs, DateTime? minimalDateToScan, out bool exit)
        {
            exit = false;

            string dateString = null;

            try
            {
                dateString = link.ParentNode.ParentNode.ChildNodes[1].InnerText;
            }
            catch (Exception ex)
            {
                logs.Add($"Unable to extract the date - {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(dateString))
            {
                exit = true;
                return null;
            }

            DateTime? date = ParseDateFromString(dateString, logs, out bool failToExtractDate);
            if (failToExtractDate)
            {
                exit = true;
                return null;
            }

            if (date < minimalDateToScan)
            {
                exit = true;
                return null;
            }

            return date;
        }

        private async Task<Engine?> ExtractTimeEntryEngineAsync(HtmlNode link, List<string> logs)
        {
            const string engineStringBeginString = "System:</strong>";
            const string engineStringEndString = "</li>";

            string pageContent = await GetPageStringContentAsync(link.Attributes["href"].Value, logs).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(pageContent))
            {
                int engineStringBeginPos = pageContent.IndexOf(engineStringBeginString);
                if (engineStringBeginPos >= 0)
                {
                    string pageContentAtBeginPos = pageContent.Substring(engineStringBeginPos + engineStringBeginString.Length);
                    int engineStringEndPos = pageContentAtBeginPos.Trim().IndexOf(engineStringEndString);
                    if (engineStringEndPos >= 0)
                    {
                        string engineString = pageContentAtBeginPos.Substring(0, engineStringEndPos + 1);

                        return SystemExtensions
                            .Enumerate<Engine>()
                            .Select(e => (Engine?)e)
                            .FirstOrDefault(e => e.ToString().Equals(engineString.Trim().Replace("-", "_"), StringComparison.InvariantCultureIgnoreCase));
                    }
                }
            }

            return null;
        }

        private async Task<string> GetPageStringContentAsync(string partUri, List<string> logs)
        {
            var uri = new Uri(string.Concat(_configuration.BaseUri, partUri));

            using (var webClient = new WebClient())
            {
                try
                {
                    return await webClient
                        .DownloadStringTaskAsync(uri)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                    when (ex is WebException || ex is NotSupportedException)
                {
                    logs.Add($"Error while downloading content of the page {uri.OriginalString} - {ex.Message}");
                    return null;
                }
            }
        }

        private static string CleanString(string input)
        {
            return input.Replace("\t", "").Replace("\n", "").Replace("\r", "");
        }

        private static long? ExtractTime(string timeString, List<string> logs, out bool failToExtractTime)
        {
            const string untiedString = "(untied!)";
            const string N_A = "N/A";
            const char separator = ':';

            failToExtractTime = false;

            timeString = timeString.Replace(untiedString, string.Empty).Trim();
            if (timeString.IndexOf(separator) >= 0)
            {
                string[] timeComponents = timeString.Split(separator);
                if (timeComponents.Length > 3)
                {
                    logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                int hours = 0;
                if (timeComponents.Length > 2)
                {
                    if (!int.TryParse(timeComponents[0], out hours))
                    {
                        logs.Add("Invalid time value");
                        failToExtractTime = true;
                        return null;
                    }
                    timeComponents[0] = timeComponents[1];
                    timeComponents[1] = timeComponents[2];
                }
                if (!int.TryParse(timeComponents[0], out int minutes))
                {
                    logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                if (!int.TryParse(timeComponents[1], out int seconds))
                {
                    logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                return (hours * 60 * 60) + (minutes * 60) + seconds;
            }
            else if (timeString != N_A)
            {
                logs.Add("Invalid time value");
                failToExtractTime = true;
                return null;
            }

            return null;
        }

        private static DateTime? ParseDateFromString(string dateString, List<string> logs, out bool failToExtractDate)
        {
            const char separator = ' ';
            IReadOnlyDictionary<string, int> monthsLabel = new Dictionary<string, int>
            {
                { "January", 1 },
                { "February", 2 },
                { "March", 3 },
                { "April", 4 },
                { "May", 5 },
                { "June", 6 },
                { "July", 7 },
                { "August", 8 },
                { "September", 9 },
                { "October", 10 },
                { "November", 11 },
                { "December", 12 }
            };

            failToExtractDate = false;

            if (dateString != Extensions.DefaultLabel)
            {
                string[] dateComponents = dateString.Split(separator);
                if (dateComponents.Length != 3)
                {
                    logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (!monthsLabel.ContainsKey(dateComponents[1]))
                {
                    logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (!int.TryParse(dateComponents[0], out int day))
                {
                    logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (!int.TryParse(dateComponents[2], out int year))
                {
                    logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                return new DateTime(year, monthsLabel[dateComponents[1]], day);
            }

            return null;
        }
    }
}
