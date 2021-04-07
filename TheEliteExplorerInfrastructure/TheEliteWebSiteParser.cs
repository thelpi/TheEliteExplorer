using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Dtos;
using TheEliteExplorerDomain.Enums;
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
        public async Task<(IReadOnlyCollection<EntryWebDto>, IReadOnlyCollection<string>)> ExtractTimeEntriesAsync(Game game, int year, int month, DateTime? minimalDateToScan)
        {
            var linksValues = new List<EntryWebDto>();
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
                        var linkValues = await ExtractTimeLinkDetailsAsync(game, link, logs, minimalDateToScan).ConfigureAwait(false);
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

        /// <inheritdoc />
        public async Task<(IReadOnlyCollection<EntryWebDto>, IReadOnlyCollection<string>)> ExtractStageAllTimeEntriesAsync(int stageId)
        {
            var entries = new List<EntryWebDto>();
            var logs = new List<string>();

            string pageContent = await GetPageStringContentAsync($"/ajax/stage/{stageId}/{_configuration.AjaxKey}", logs).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(pageContent))
            {
                logs.Add($"Unables to load the page content for stage {stageId}.");
                return (entries, logs);
            }

            IReadOnlyCollection<IReadOnlyCollection<object>> jsonContent = null;
            try
            {
                jsonContent = Newtonsoft.Json.JsonConvert.DeserializeObject<IReadOnlyCollection<IReadOnlyCollection<object>>>(pageContent);
            }
            catch (Exception ex)
            {
                logs.Add($"An error occured while parsing the content - {ex.Message}.");
                return (entries, logs);
            }

            if (jsonContent == null || jsonContent.Count != SystemExtensions.Count<Level>())
            {
                logs.Add($"The list of entries by level is invalid.");
                return (entries, logs);
            }

            Dictionary<Level, List<long>> links = ExtractEntryIdListFromJsonContent(jsonContent, logs);

            foreach (Level levelKey in links.Keys)
            {
                foreach (long entryId in links[levelKey])
                {
                    try
                    {
                        var entryDetails = await ExtractEntryDetailsAsync(entryId, stageId, levelKey, logs).ConfigureAwait(false);
                        entries.AddRange(entryDetails);
                    }
                    catch (Exception ex)
                    {
                        logs.Add($"General exception occured while retrieving entry details - {ex.Message}");
                    }
                }
            }

            return (entries, logs);
        }

        /// <inheritdoc />
        public async Task<(PlayerDto, IReadOnlyCollection<string>)> GetPlayerInformation(string urlName, string defaultHexPlayer)
        {
            var logs = new List<string>();
            string realName = null;
            string surname = null;
            string color = null;
            string controlStyle = null;

            var pageContent = await GetPageStringContentAsync($"/~{urlName.Replace(" ", "+")}", logs).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent))
            {
                return (null, logs);
            }

            var htmlDoc = new HtmlDocument();
            try
            {
                htmlDoc.LoadHtml(pageContent);
            }
            catch (Exception ex)
            {
                logs.Add($"Error while parsing into HTML document the page string content - {ex.Message}");
                return (null, logs);
            }

            var headFull = htmlDoc.DocumentNode.SelectNodes("//h1");
            var h1Node = headFull.Count > 1 ? headFull[1] : headFull.First();

            surname = h1Node.InnerText.Trim().Replace("\r", "").Replace("\n", "").Replace("\t", "");
            if (string.IsNullOrWhiteSpace(surname))
            {
                surname = null;
            }

            color = h1Node.Attributes["style"].Value.Replace("color:#", "").Trim();
            if (color.Length != 6)
            {
                color = null;
            }

            var indexofControlStyle = pageContent.IndexOf("uses the <strong>");
            if (indexofControlStyle >= 0)
            {
                var controlStyleTxt = pageContent.Substring(indexofControlStyle + "uses the <strong>".Length);
                controlStyleTxt = controlStyleTxt.Split(new[] { "</strong>" }, StringSplitOptions.RemoveEmptyEntries).First().Trim().Replace("\r", "").Replace("\n", "").Replace("\t", "");
                if (!string.IsNullOrWhiteSpace(controlStyleTxt))
                {
                    controlStyle = controlStyleTxt;
                }
            }

            var indexofRealname = pageContent.IndexOf("real name is <strong>");
            if (indexofRealname >= 0)
            {
                var realnameTxt = pageContent.Substring(indexofRealname + "real name is <strong>".Length);
                realnameTxt = realnameTxt.Split(new[] { "</strong>" }, StringSplitOptions.RemoveEmptyEntries).First().Trim().Replace("\r", "").Replace("\n", "").Replace("\t", "");
                if (!string.IsNullOrWhiteSpace(realnameTxt))
                {
                    realName = realnameTxt;
                }
            }

            var p = new PlayerDto
            {
                Color = color ?? defaultHexPlayer,
                ControlStyle = controlStyle,
                RealName = realName ?? (surname ?? urlName),
                SurName = surname ?? urlName,
                UrlName = urlName
            };

            return (p, logs);
        }

        private async Task<List<EntryWebDto>> ExtractEntryDetailsAsync(long entryId, int stageId, Level levelKey, List<string> logs)
        {
            var finalEntries = new List<EntryWebDto>();

            // /!\/!\/!\ Any name can go in the URL
            string linkData = await GetPageStringContentAsync($"/~Karl+Jobst/time/{entryId}", logs).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(linkData))
            {
                // This case occurs, for the most part, because of a temporary security issue
                // So we wait one second and retry
                System.Threading.Thread.Sleep(1000);
                linkData = await GetPageStringContentAsync($"/~Karl+Jobst/time/{entryId}", logs).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(linkData))
            {
                return finalEntries;
            }

            var htmlDocHead = new HtmlDocument();
            htmlDocHead.LoadHtml(linkData);
            
            string playerUrlName = htmlDocHead
                .DocumentNode.SelectNodes("//h1/a").First()
                .Attributes.AttributesWithName("href").First().Value;

            const string playerUrlPrefix = "/~";
            const string N_A = "N/A";

            playerUrlName = playerUrlName.Replace(playerUrlPrefix, string.Empty).Split('/').First().Replace("+", " ");

            string[] htmlParts = linkData.Split(new string[] { "<table>", "</table>" }, StringSplitOptions.RemoveEmptyEntries);
            if (htmlParts.Length != 3)
            {
                HtmlNodeCollection headTitle = htmlDocHead.DocumentNode.SelectNodes("//h1");
                string headTitleText = (headTitle.Count > 1 ? headTitle[1] : headTitle.First()).InnerText;
                var indexOfDoubleDot = headTitleText.IndexOf(":");

                string timeFromHead = indexOfDoubleDot < 0 ? N_A : string.Join(string.Empty,
                    Enumerable.Range(-2, 5).Select(i => headTitleText[indexOfDoubleDot + i]));

                var entryRequest = ExtractEntryFromHead(stageId, levelKey, timeFromHead, playerUrlName, htmlParts[0], logs);
                if (entryRequest != null)
                {
                    finalEntries.Add(entryRequest);
                }
            }
            else
            {
                finalEntries.AddRange(ExtractEntriesFromTable(stageId, levelKey, playerUrlName, htmlParts[1], logs));
            }

            return finalEntries;
        }

        private EntryWebDto ExtractEntryFromHead(int stageId, Level levelKey, string timeFromhead, string playerUrlName, string content, List<string> logs)
        {
            var versionFromHead = "Unknown";
            var dateFromHead = "Unknown";
            const string achievedPart = "<strong>Achieved:</strong>";
            const string systemPart = "<strong>System:</strong>";

            var i1 = content.IndexOf(achievedPart);
            if (i1 >= 0)
            {
                var subpart = content.Substring(i1 + achievedPart.Length).Split(new string[] { "</li>" }, StringSplitOptions.RemoveEmptyEntries);
                if (subpart.Length > 0)
                {
                    dateFromHead = subpart[0];
                }
            }

            var i2 = content.IndexOf(systemPart);
            if (i2 >= 0)
            {
                var subpart = content.Substring(i2 + systemPart.Length).Split(new string[] { "</li>" }, StringSplitOptions.RemoveEmptyEntries);
                if (subpart.Length > 0)
                {
                    versionFromHead = subpart[0];
                }
            }

            long? time = ExtractTime(timeFromhead, logs, out bool failToExtract);
            if (failToExtract || !time.HasValue)
            {
                return null;
            }

            DateTime? date = ParseDateFromString(dateFromHead, logs, out failToExtract);
            if (failToExtract)
            {
                return null;
            }

            var system = ToEngine(versionFromHead);
            return new EntryWebDto
            {
                Date = date,
                LevelId = (int)levelKey,
                PlayerUrlName = playerUrlName,
                StageId = stageId,
                SystemId = system.HasValue ? (int)system.Value : (int?)null,
                Time = time.Value
            };
        }

        private IEnumerable<EntryWebDto> ExtractEntriesFromTable(int stageId, Level levelKey, string playerUrlName, string content, List<string> logs)
        {
            string tableContent = string.Concat("<table>", content, "</table>");

            var doc = new HtmlDocument();
            doc.LoadHtml(tableContent);
            
            foreach (HtmlNode row in doc.DocumentNode.SelectNodes("//tr[td]"))
            {
                var rowDatas = row.SelectNodes("td").Select(td => td.InnerText).ToArray();

                long? time = ExtractTime(rowDatas[1], logs, out bool failToExtract);
                if (!failToExtract && time.HasValue)
                {
                    DateTime? date = ParseDateFromString(rowDatas[0], logs, out failToExtract);
                    if (!failToExtract)
                    {
                        var system = ToEngine(rowDatas[3]);
                        yield return new EntryWebDto
                        {
                            Date = date,
                            LevelId = (int)levelKey,
                            PlayerUrlName = playerUrlName,
                            StageId = stageId,
                            SystemId = system.HasValue ? (int)system.Value : (int?)null,
                            Time = time.Value
                        };
                    }
                }
            }
        }

        private Dictionary<Level, List<long>> ExtractEntryIdListFromJsonContent(IReadOnlyCollection<IReadOnlyCollection<object>> stageJsonContent, List<string> logs)
        {
            var entryIdListByLevel = new Dictionary<Level, List<long>>();

            const int positionOfId = 7;
            const int positionOfStart = 4;

            int m = 0;
            foreach (IReadOnlyCollection<object> jsonLevelEntries in stageJsonContent)
            {
                var entryIdList = new List<long>();
                entryIdListByLevel.Add(SystemExtensions.Enumerate<Level>().ElementAt(m), entryIdList);
                try
                {
                    int j = positionOfStart;
                    foreach (object jsonEntry in jsonLevelEntries)
                    {
                        if (j % positionOfId == 0)
                        {
                            if (jsonEntry == null)
                            {
                                logs.Add("The JSON entry was null.");
                            }
                            else if (!long.TryParse(jsonEntry.ToString(), out long entryId))
                            {
                                logs.Add($"The JSON entry was not a long - value: {jsonEntry}");
                            }
                            else
                            {
                                entryIdList.Add(entryId);
                            }
                        }
                        j++;
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"Error while parsing JSON entry - {ex.Message}");
                }
                m++;
            }

            return entryIdListByLevel;
        }

        private async Task<EntryWebDto> ExtractTimeLinkDetailsAsync(Game game, HtmlNode link, List<string> logs, DateTime? minimalDateToScan)
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
            if (!_stageNames.ContainsKey(stageName))
            {
                if (stageName != _duelStageName)
                {
                    logs.Add($"Unable to extract the stage ID.");
                }
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
            if (failToExtractTime || !time.HasValue)
            {
                return null;
            }

            var system = await ExtractTimeEntryEngineAsync(link, logs).ConfigureAwait(false);

            return new EntryWebDto
            {
                Date = date,
                LevelId = (int)level.Value,
                PlayerUrlName = playerUrl,
                StageId = _stageNames[stageName],
                SystemId = system.HasValue ? (int)system.Value : (int?)null,
                Time = time.Value
            };
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

                        return ToEngine(engineString);
                    }
                }
            }

            return null;
        }

        private static Engine? ToEngine(string engineString)
        {
            return SystemExtensions
                .Enumerate<Engine>()
                .Select(e => (Engine?)e)
                .FirstOrDefault(e => e.ToString().Equals(engineString.Trim().Replace("-", "_"), StringComparison.InvariantCultureIgnoreCase));
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

            dateString = dateString?.Trim();

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

        private static readonly IReadOnlyDictionary<string, int> _stageNames = new Dictionary<string, int>
        {
            { "dam", 1 },
            { "facility", 2 },
            { "runway", 3 },
            { "surface1", 4 },
            { "bunker1", 5 },
            { "silo", 6 },
            { "frigate", 7 },
            { "surface2", 8 },
            { "bunker2", 9 },
            { "statue", 10 },
            { "archives", 11 },
            { "streets", 12 },
            { "depot", 13 },
            { "train", 14 },
            { "jungle", 15 },
            { "control", 16 },
            { "caverns", 17 },
            { "cradle", 18 },
            { "aztec", 19 },
            { "egypt", 20 },
            { "defection", 1 },
            { "investigation", 2 },
            { "extraction", 3 },
            { "villa", 4 },
            { "chicago", 5 },
            { "g5", 6 },
            { "infiltration", 7 },
            { "rescue", 8 },
            { "escape", 9 },
            { "airbase", 10 },
            { "airforceone", 11 },
            { "crashsite", 12 },
            { "pelagicii", 13 },
            { "deepsea", 14 },
            { "ci", 15 },
            { "attackship", 16 },
            { "skedarruins", 17 },
            { "mbr", 18 },
            { "maiansos", 19 },
            { "war!", 20 }
        };
        private const string _duelStageName = "duel";
    }
}
