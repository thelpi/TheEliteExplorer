using System;
using System.Collections.Concurrent;
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
        public async Task<IReadOnlyCollection<EntryWebDto>> ExtractTimeEntriesAsync(Game game, int year, int month, DateTime? minimalDateToScan)
        {
            var linksValues = new ConcurrentBag<EntryWebDto>();

            string uri = string.Format(_configuration.HistoryPage, year, month);

            string historyContent = await GetPageStringContentAsync(uri)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(historyContent))
            {
                return linksValues;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(historyContent);

            const string timeClass = "time";

            var linksOk = new List<HtmlNode>();

            var links = htmlDoc.DocumentNode.SelectNodes("//a");
            foreach (HtmlNode link in links)
            {
                if (link.Attributes.Contains("class")
                    && link.Attributes["class"].Value == timeClass)
                {
                    linksOk.Add(link);
                }
            }

            const int parallel = 4;
            for (var i = 0; i < linksOk.Count; i += parallel)
            {
                await Task
                    .WhenAll(linksOk.Skip(i).Take(parallel).Select(link =>
                        ProcessLinkAndAddToListAsync(game, minimalDateToScan, linksValues, link)))
                    .ConfigureAwait(false);
            }

            return linksValues;
        }

        private async Task ProcessLinkAndAddToListAsync(Game game, DateTime? minimalDateToScan, ConcurrentBag<EntryWebDto> linksValues, HtmlNode link)
        {
            var linkValues = await ExtractTimeLinkDetailsAsync(game, link, minimalDateToScan).ConfigureAwait(false);
            if (linkValues != null)
            {
                linksValues.Add(linkValues);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryWebDto>> ExtractStageAllTimeEntriesAsync(Stage stage)
        {
            var entries = new List<EntryWebDto>();
            var logs = new List<string>();

            string pageContent = await GetPageStringContentAsync($"/ajax/stage/{(long)stage}/{_configuration.AjaxKey}").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(pageContent))
            {
                throw new FormatException($"Unables to load the page content for stage {stage}.");
            }

            var jsonContent = Newtonsoft.Json.JsonConvert.DeserializeObject<IReadOnlyCollection<IReadOnlyCollection<object>>>(pageContent);

            if (jsonContent == null || jsonContent.Count != SystemExtensions.Count<Level>())
            {
                throw new FormatException($"The list of entries by level is invalid.");
            }

            Dictionary<Level, List<long>> links = ExtractEntryIdListFromJsonContent(jsonContent, logs);

            foreach (Level levelKey in links.Keys)
            {
                foreach (long entryId in links[levelKey])
                {
                    var entryDetails = await ExtractEntryDetailsAsync(entryId, stage, levelKey, logs).ConfigureAwait(false);
                    entries.AddRange(entryDetails);
                }
            }

            return entries;
        }

        /// <inheritdoc />
        public async Task<PlayerDto> GetPlayerInformationAsync(string urlName, string defaultHexPlayer)
        {
            var logs = new List<string>();
            string realName = null;
            string surname = null;
            string color = null;
            string controlStyle = null;

            var pageContent = await GetPageStringContentAsync($"/~{urlName.Replace(" ", "+")}", true)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent))
            {
                return null;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContent);

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

            return p;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<EntryWebDto>> GetPlayerEntriesHistoryAsync(Game game, string playerUrlName)
        {
            var entries = new List<EntryWebDto>();

            var pageContent = await GetPageStringContentAsync($"~{playerUrlName}/{game.GetGameUrlName()}/history", true)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent))
            {
                // Do not return an empty list here.
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            foreach (var row in doc.DocumentNode.SelectNodes("//tr[td]"))
            {
                var rowDatas = row.SelectNodes("td").Select(td => td.InnerText).ToArray();

                var stage = game.GetStageFromLabel(rowDatas[1]);
                if (stage == null)
                {
                    continue;
                }

                var level = game.GetLevelFromLabel(rowDatas[2]);
                if (!level.HasValue)
                {
                    continue;
                }

                var engine = ToEngine(rowDatas[4]);
                if (!engine.HasValue)
                {

                }

                var date = ParseDateFromString(rowDatas[0], out bool failToExtractDate, true);
                if (failToExtractDate)
                {

                }

                var time = ExtractTime(rowDatas[3], out bool failToExtractTime);
                if (failToExtractTime || !time.HasValue)
                {
                    continue;
                }

                entries.Add(new EntryWebDto
                {
                    Date = date,
                    Level = level.Value,
                    PlayerUrlName = playerUrlName,
                    Stage = stage.Value,
                    Engine = engine,
                    Time = time.Value
                });
            }

            return entries;
        }

        private async Task<List<EntryWebDto>> ExtractEntryDetailsAsync(long entryId, Stage stage, Level levelKey, List<string> logs)
        {
            var finalEntries = new List<EntryWebDto>();

            // /!\/!\/!\ Any name can go in the URL
            string linkData = await GetPageStringContentAsync($"/~Karl+Jobst/time/{entryId}")
                .ConfigureAwait(false);

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

                var entryRequest = ExtractEntryFromHead(stage, levelKey, timeFromHead, playerUrlName, htmlParts[0], logs);
                if (entryRequest != null)
                {
                    finalEntries.Add(entryRequest);
                }
            }
            else
            {
                finalEntries.AddRange(ExtractEntriesFromTable(stage, levelKey, playerUrlName, htmlParts[1], logs));
            }

            return finalEntries;
        }

        private EntryWebDto ExtractEntryFromHead(Stage stage, Level levelKey, string timeFromhead, string playerUrlName, string content, List<string> logs)
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

            long? time = ExtractTime(timeFromhead, out bool failToExtract);
            if (failToExtract || !time.HasValue)
            {
                return null;
            }

            DateTime? date = ParseDateFromString(dateFromHead, out failToExtract);
            if (failToExtract)
            {
                return null;
            }

            var engine = ToEngine(versionFromHead);
            return new EntryWebDto
            {
                Date = date,
                Level = levelKey,
                PlayerUrlName = playerUrlName,
                Stage = stage,
                Engine = engine,
                Time = time.Value
            };
        }

        private IEnumerable<EntryWebDto> ExtractEntriesFromTable(Stage stage, Level levelKey, string playerUrlName, string content, List<string> logs)
        {
            string tableContent = string.Concat("<table>", content, "</table>");

            var doc = new HtmlDocument();
            doc.LoadHtml(tableContent);
            
            foreach (HtmlNode row in doc.DocumentNode.SelectNodes("//tr[td]"))
            {
                var rowDatas = row.SelectNodes("td").Select(td => td.InnerText).ToArray();

                long? time = ExtractTime(rowDatas[1], out bool failToExtract);
                if (!failToExtract && time.HasValue)
                {
                    DateTime? date = ParseDateFromString(rowDatas[0], out failToExtract);
                    if (!failToExtract)
                    {
                        var engine = ToEngine(rowDatas[3]);
                        yield return new EntryWebDto
                        {
                            Date = date,
                            Level = levelKey,
                            PlayerUrlName = playerUrlName,
                            Stage = stage,
                            Engine = engine,
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
                m++;
            }

            return entryIdListByLevel;
        }

        private async Task<EntryWebDto> ExtractTimeLinkDetailsAsync(Game game, HtmlNode link, DateTime? minimalDateToScan)
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

            DateTime? date = ExtractAndCheckDate(link, minimalDateToScan, out bool exit);
            if (exit)
            {
                return null;
            }

            string stageName = linkParts[0].ToLowerInvariant().Replace(" ", string.Empty);
            if (!Extensions.StageFormatedNames.ContainsKey(stageName))
            {
                if (stageName != Extensions.PerfectDarkDuelStageFormatedName)
                {
                    throw new FormatException($"Unable to extract the stage ID.");
                }
                return null;
            }
            else if (game != Extensions.StageFormatedNames[stageName].GetGame())
            {
                return null;
            }

            string playerUrl = link
                .ParentNode
                .ParentNode
                .ChildNodes[3]
                .ChildNodes
                .First()
                .Attributes["href"]
                .Value
                .Replace(playerUrlPrefix, string.Empty)
                .Replace("+", " ");

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

            long? time = ExtractTime(linkParts[2], out bool failToExtractTime);
            if (failToExtractTime || !time.HasValue)
            {
                return null;
            }

            return new EntryWebDto
            {
                Date = date,
                Level = level.Value,
                PlayerUrlName = playerUrl,
                Stage = Extensions.StageFormatedNames[stageName],
                Engine = await ExtractTimeEntryEngineAsync(link).ConfigureAwait(false),
                Time = time.Value
            };
        }

        private static DateTime? ExtractAndCheckDate(HtmlNode link, DateTime? minimalDateToScan, out bool exit)
        {
            exit = false;

            string dateString = link.ParentNode.ParentNode.ChildNodes[1].InnerText;

            if (string.IsNullOrWhiteSpace(dateString))
            {
                exit = true;
                return null;
            }

            DateTime? date = ParseDateFromString(dateString, out bool failToExtractDate);
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

        private async Task<Engine?> ExtractTimeEntryEngineAsync(HtmlNode link)
        {
            const string engineStringBeginString = "System:</strong>";
            const string engineStringEndString = "</li>";

            string pageContent = await GetPageStringContentAsync(link.Attributes["href"].Value).ConfigureAwait(false);

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

        private async Task<string> GetPageStringContentAsync(string partUri, bool ignoreNotFound = false)
        {
            var uri = new Uri(string.Concat(_configuration.BaseUri, partUri));
            
            string data = null;
            int attemps = 0;
            while (attemps < _configuration.PageAttemps)
            {
                using (var webClient = new WebClient())
                {
                    try
                    {
                        data = await webClient
                            .DownloadStringTaskAsync(uri)
                            .ConfigureAwait(false);
                        attemps = _configuration.PageAttemps;
                    }
                    catch (Exception ex)
                    {
                        if (ignoreNotFound && ex.IsWebNotFound())
                        {
                            return data;
                        }
                        attemps++;
                        if (attemps == _configuration.PageAttemps)
                        {
                            throw;
                        }
                    }
                }
            }

            return data;
        }
        
        private static string CleanString(string input)
        {
            return input.Replace("\t", "").Replace("\n", "").Replace("\r", "");
        }

        private static long? ExtractTime(string timeString, out bool failToExtractTime)
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
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                int hours = 0;
                if (timeComponents.Length > 2)
                {
                    if (!int.TryParse(timeComponents[0], out hours))
                    {
                        //logs.Add("Invalid time value");
                        failToExtractTime = true;
                        return null;
                    }
                    timeComponents[0] = timeComponents[1];
                    timeComponents[1] = timeComponents[2];
                }
                if (!int.TryParse(timeComponents[0], out int minutes))
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                if (!int.TryParse(timeComponents[1], out int seconds))
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                return (hours * 60 * 60) + (minutes * 60) + seconds;
            }
            else if (timeString != N_A)
            {
                //logs.Add("Invalid time value");
                failToExtractTime = true;
                return null;
            }

            return null;
        }

        private static DateTime? ParseDateFromString(
            string dateString,
            out bool failToExtractDate,
            bool partialMonthName = false)
        {
            const char separator = ' ';

            var monthsLabel = new Dictionary<string, int>
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

            var monthsPartialLabel = new Dictionary<string, int>
            {
                { "Jan", 1 },
                { "Feb", 2 },
                { "Mar", 3 },
                { "Apr", 4 },
                { "May", 5 },
                { "Jun", 6 },
                { "Jul", 7 },
                { "Aug", 8 },
                { "Sep", 9 },
                { "Oct", 10 },
                { "Nov", 11 },
                { "Dec", 12 }
            };

            failToExtractDate = false;

            dateString = dateString?.Trim();

            if (dateString != Extensions.DefaultLabel)
            {
                string[] dateComponents = dateString.Split(separator);
                if (dateComponents.Length != 3)
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (partialMonthName)
                {
                    if (!monthsPartialLabel.ContainsKey(dateComponents[1]))
                    {
                        //logs.Add("No date found !");
                        failToExtractDate = true;
                        return null;
                    }
                }
                else
                {
                    if (!monthsLabel.ContainsKey(dateComponents[1]))
                    {
                        //logs.Add("No date found !");
                        failToExtractDate = true;
                        return null;
                    }
                }
                if (!int.TryParse(dateComponents[0], out int day))
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (!int.TryParse(dateComponents[2], out int year))
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                return new DateTime(year, partialMonthName ? monthsPartialLabel[dateComponents[1]] : monthsLabel[dateComponents[1]], day);
            }

            return null;
        }
    }
}
