using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using TheEliteWpf.Datas;

namespace TheEliteWpf
{
    public partial class MainWindow : Window
    {
        private const string EndpointUrl = "http://localhost:54460/";
        private static readonly Dictionary<Game, DateTime> EliteBeginDate =
            new Dictionary<Game, DateTime>
            {
                { Game.GoldenEye, new DateTime(1998, 05, 14) },
                { Game.PerfectDark, new DateTime(2000, 06, 06) }
            };
        private const int ImageWidth = 90;
        private const int PanelHeight = 45;

        private readonly List<Action> _clearers = new List<Action>();

        public MainWindow()
        {
            InitializeComponent();
            SetStandingProcedure();
        }

        private void SetStandingProcedure()
        {
            _clearers.ForEach(_ => _());
            _clearers.Clear();

            var selectionWindow = new SelectionWindow();
            selectionWindow.ShowDialog();

            CollapseImagesFromOtherGame(selectionWindow);

            var width = MainGrid.Width - ImageWidth;

            ChangeButton.IsEnabled = false;
            if (selectionWindow.StandingType == StandingType.LeaderboardView)
            {
                Task.Run(() => LoadStandings(selectionWindow.Game, selectionWindow.PlayerId, width));
            }
            else
            {
                Task.Run(async () =>
                    await LoadStandingsAsync(
                            selectionWindow.Game,
                            selectionWindow.StandingType,
                            selectionWindow.Engine,
                            selectionWindow.PlayerId,
                            selectionWindow.OpacityCap,
                            width)
                        .ConfigureAwait(false));
            }
        }

        private void CollapseImagesFromOtherGame(SelectionWindow selectionWindow)
        {
            MainGrid.Children.Cast<FrameworkElement>()
                .Where(uie => uie.GetType() == typeof(Image)
                    && uie.Tag != null
                    && int.TryParse(uie.Tag.ToString(), out int tagId)
                    && tagId != (int)selectionWindow.Game)
                .All(uie => { uie.Visibility = Visibility.Collapsed; return true; });

            MainGrid.Children.Cast<FrameworkElement>()
                .Where(uie => uie.GetType() == typeof(Image)
                    && uie.Tag != null
                    && int.TryParse(uie.Tag.ToString(), out int tagId)
                    && tagId == (int)selectionWindow.Game)
                .All(uie => { uie.Visibility = Visibility.Visible; return true; });
        }

        private void LoadStandings(Game game, long? playerId, double totalWidth)
        {
            var pxPerDay = totalWidth / (DateTime.Today.AddDays(1) - EliteBeginDate[game]).TotalDays;

            var stages = Enum
                .GetValues(typeof(Stage))
                .Cast<Stage>()
                .Where(s => game == Game.GoldenEye ? (int)s <= 20 : (int)s > 20)
                .ToList();

            int countByParallelTask = 5;
            Parallel.For(0, stages.Count / countByParallelTask, x =>
            {
                foreach (var stage in stages.Skip(x * countByParallelTask).Take(countByParallelTask))
                {
                    var wrs = GetLeaderboardsAsync(stage, playerId.HasValue ? LeaderboardGroupOptions.None : LeaderboardGroupOptions.FirstRankedFirst).GetAwaiter().GetResult();

                    foreach (var wr in wrs)
                    {
                        var itemToDisplay = wr.Items.FirstOrDefault(_ => !playerId.HasValue || playerId == _.Player.Id);
                        if (itemToDisplay != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DrawLeaderboardRectangle(game, wr, itemToDisplay, pxPerDay);
                            });
                        }
                    }
                }
            });

            Dispatcher.Invoke(() => ChangeButton.IsEnabled = true);
        }

        private void DrawLeaderboardRectangle(Game game, Leaderboard ld, LeaderboardItem it, double pxPerDay)
        {
            var rect = new Rectangle
            {
                Width = pxPerDay * (ld.DateEnd - ld.DateStart).TotalDays,
                Height = PanelHeight - 2,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom($"#{it.Player.Color}"),
                Opacity = it.Rank > 10 ? 0 : (11 - it.Rank) / (double)10
            };
            var canvas = FindName($"Stage{(game == Game.PerfectDark ? (int)ld.Stage - 20 : (int)ld.Stage)}") as Canvas;
            rect.SetValue(Canvas.TopProperty, 1D);
            rect.SetValue(Canvas.LeftProperty, (ld.DateStart - EliteBeginDate[game]).TotalDays * pxPerDay);
            canvas.Children.Add(rect);
            _clearers.Add(() => canvas.Children.Remove(rect));
        }

        private static async Task<IReadOnlyCollection<Leaderboard>> GetLeaderboardsAsync(Stage stage, LeaderboardGroupOptions groupOptions)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(EndpointUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };

            var url = $"stages/{(int)stage}/leaderboard-history?groupOption={groupOptions}";

            var response = await client
                .SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri(url, UriKind.Relative),
                    Method = HttpMethod.Get
                })
                .ConfigureAwait(false);

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Leaderboard>>(content);
        }

        private async Task LoadStandingsAsync(Game game, StandingType standingType, Engine? engine, long? playerId, int? opacityCap, double totalWidth)
        {
            var pxPerDay = totalWidth / (DateTime.Today.AddDays(1) - EliteBeginDate[game]).TotalDays;

            var wrs = await GetStandingWorldRecordsAsync(game, standingType, engine)
                .ConfigureAwait(false);

            if (playerId.HasValue)
                wrs = wrs.Where(x => x.Author.Id == playerId).ToList();

            foreach (var wr in wrs)
            {
                Dispatcher.Invoke(() =>
                {
                    DrawStandingRectangle(game, wr, pxPerDay, playerId.HasValue, opacityCap);
                });
            }

            Dispatcher.Invoke(() => ChangeButton.IsEnabled = true);
        }

        private void DrawStandingRectangle(Game game, Standing wr, double pxPerDay, bool anonymize, int? opacityCap = null)
        {
            var thirdSize = PanelHeight / 3;
            var rect = new Rectangle
            {
                Width = pxPerDay * wr.Days,
                Height = thirdSize - 2,
                Fill = anonymize || opacityCap.HasValue
                    ? Brushes.Red
                    : (SolidColorBrush)new BrushConverter().ConvertFrom($"#{wr.Author.Color}"),
                ToolTip = opacityCap.HasValue ? null : wr,
                Opacity = opacityCap.HasValue ? 1 / (double)opacityCap.Value : 1.0
            };

            var iLevel = (int)wr.Level - 1;


            var canvas = FindName($"Stage{(game == Game.PerfectDark ? (int)wr.Stage - 20 : (int)wr.Stage)}") as Canvas;
            rect.SetValue(Canvas.TopProperty, 1D + (thirdSize * iLevel));
            rect.SetValue(Canvas.LeftProperty, (wr.StartDate - EliteBeginDate[game]).TotalDays * pxPerDay);
            canvas.Children.Add(rect);
            _clearers.Add(() => canvas.Children.Remove(rect));
        }

        private static async Task<IReadOnlyCollection<Standing>> GetStandingWorldRecordsAsync(Game game, StandingType standingType, Engine? engine)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(EndpointUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };

            var url = $"games/{(int)game}/longest-standings?standingType={(int)standingType}&count={int.MaxValue}";
            if (engine.HasValue)
                url += $"&engine={(int)engine}";

            var response = await client
                .SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri(url, UriKind.Relative),
                    Method = HttpMethod.Get
                })
                .ConfigureAwait(false);

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Standing>>(content);
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            SetStandingProcedure();
        }
    }
}
