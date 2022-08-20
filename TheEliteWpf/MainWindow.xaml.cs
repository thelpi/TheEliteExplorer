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
        private const double PicRatio = 1.55;
        private const int PanelHeight = 45;
        private const double PxPerDay = 0.2;

        private static readonly DateTime FirstDate = new DateTime(1998, 5, 1);
        private static readonly double TotalDays = (DateTime.Today.AddDays(1) - FirstDate).TotalDays;
        private static readonly double FirstColWidth = Math.Ceiling(PanelHeight * PicRatio);
        private static readonly double AvailableWidth = Math.Ceiling(TotalDays * PxPerDay);

        private readonly List<Action> _clearers = new List<Action>();

        public MainWindow()
        {
            InitializeComponent();

            foreach (var rowDef in MainGrid.RowDefinitions)
                rowDef.Height = new GridLength(PanelHeight);
            MainGrid.ColumnDefinitions[0].Width = new GridLength(FirstColWidth);
            MainGrid.Width = FirstColWidth + AvailableWidth;

            SetStandingProcedure();
        }

        private void SetStandingProcedure()
        {
            _clearers.ForEach(_ => _());
            _clearers.Clear();

            var players = GetPlayersAsync().GetAwaiter().GetResult();

            SelectionWindow selectionWindow;
            bool? stop = null;
            do
            {
                selectionWindow = new SelectionWindow(players);
                selectionWindow.ShowDialog();
                if (!selectionWindow.Configured)
                {
                    var response = MessageBox.Show("Configuration not finished!\nDo you want to retry?\nProgram will shutdown otherwise.", "", MessageBoxButton.OKCancel);
                    if (response == MessageBoxResult.Cancel)
                        stop = true;
                }
                else
                    stop = false;
            }
            while (!stop.HasValue);

            if (stop == true)
            {
                Environment.Exit(0);
            }

            CollapseImagesFromOtherGame(selectionWindow);

            ChangeButton.IsEnabled = false;
            LoadingBar.Visibility = Visibility.Visible;
            if (selectionWindow.GraphType == GraphType.Leaderboard
                || selectionWindow.GraphType == GraphType.Ranking)
            {
                Task.Run(() => LoadStandings(
                    selectionWindow.Game,
                    selectionWindow.PlayerId,
                    selectionWindow.Anonymise,
                    selectionWindow.GraphType == GraphType.Leaderboard
                        ? OpacityStyle.Top10
                        : OpacityStyle.Points));
            }
            else
            {
                Task.Run(async () =>
                    await LoadStandingsAsync(
                            selectionWindow.Game,
                            selectionWindow.GraphType == GraphType.AllUnslay
                                ? StandingType.Unslayed
                                : (selectionWindow.GraphType == GraphType.FirstUnslay
                                    ? StandingType.FirstUnslayed
                                    : StandingType.Untied),
                            selectionWindow.Engine,
                            selectionWindow.PlayerId,
                            null,
                            selectionWindow.Anonymise)
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

        private void LoadStandings(Game game, long? playerId, bool anonymize, OpacityStyle? os)
        {
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
                    var opt = LeaderboardGroupOptions.FirstRankedFirst;
                    Func<LeaderboardItem, double> opacityFunc = it => 1;
                    if (playerId.HasValue)
                    {
                        switch (os)
                        {
                            case OpacityStyle.Points:
                                opt = LeaderboardGroupOptions.None;
                                opacityFunc = it => (0.00083 * Math.Pow(it.Points, 2) + 0.0839) / 100; // it.Points / (double)300;
                                break;
                            case OpacityStyle.Top10:
                                opt = LeaderboardGroupOptions.RankedTop10;
                                opacityFunc = it => it.Rank > 10 ? 0 : (11 - it.Rank) / (double)10;
                                break;
                        }
                    }

                    var wrs = GetLeaderboardsAsync(stage, opt).GetAwaiter().GetResult();

                    foreach (var wr in wrs)
                    {
                        var itemToDisplay = wr.Items.FirstOrDefault(_ => !playerId.HasValue || playerId == _.Player.Id);
                        if (itemToDisplay != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DrawLeaderboardRectangle(game, wr, itemToDisplay, anonymize, opacityFunc);
                            });
                        }
                    }
                }
            });

            Dispatcher.Invoke(() => { ChangeButton.IsEnabled = true; LoadingBar.Visibility = Visibility.Collapsed; });
        }

        private void DrawLeaderboardRectangle(Game game, Leaderboard ld, LeaderboardItem it, bool anonymize, Func<LeaderboardItem, double> opacityFunc)
        {
            var rect = new Rectangle
            {
                Width = PxPerDay * (ld.DateEnd - ld.DateStart).TotalDays,
                Height = PanelHeight - 2,
                Fill = anonymize
                    ? Brushes.White
                    : (SolidColorBrush)new BrushConverter().ConvertFrom($"#{it.Player.Color}"),
                Opacity = opacityFunc(it),
                ToolTip = anonymize
                    ? null
                    : $"Date:{ld.DateStart}\nPoints:{it.Points}\nRank:{it.Rank}"
            };
            var canvas = FindName($"Stage{(game == Game.PerfectDark ? (int)ld.Stage - 20 : (int)ld.Stage)}") as Canvas;
            rect.SetValue(Canvas.TopProperty, 1D);
            rect.SetValue(Canvas.LeftProperty, Math.Ceiling((ld.DateStart - FirstDate).TotalDays * PxPerDay));
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

            var url = $"stages/{(int)stage}/leaderboard-history?groupOption={groupOptions}&daysStep=5";

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

        private async Task LoadStandingsAsync(Game game, StandingType standingType, Engine? engine, long? playerId, int? opacityCap, bool anonymize)
        {
            var wrs = await GetStandingWorldRecordsAsync(game, standingType, engine)
                .ConfigureAwait(false);

            if (playerId.HasValue)
                wrs = wrs.Where(x => x.Author.Id == playerId).ToList();

            foreach (var wr in wrs)
            {
                Dispatcher.Invoke(() =>
                {
                    DrawStandingRectangle(game, wr, anonymize, opacityCap);
                });
            }

            Dispatcher.Invoke(() => { ChangeButton.IsEnabled = true; LoadingBar.Visibility = Visibility.Collapsed; });
        }

        private void DrawStandingRectangle(Game game, Standing wr, bool anonymize, int? opacityCap = null)
        {
            var thirdSize = PanelHeight / 3;
            var rect = new Rectangle
            {
                Width = PxPerDay * wr.Days,
                Height = thirdSize - 2,
                Fill = anonymize || opacityCap.HasValue
                    ? Brushes.White
                    : (SolidColorBrush)new BrushConverter().ConvertFrom($"#{wr.Author.Color}"),
                ToolTip = opacityCap.HasValue ? null : wr,
                Opacity = opacityCap.HasValue ? 1 / (double)opacityCap.Value : 1.0
            };

            var canvas = FindName($"Stage{(game == Game.PerfectDark ? (int)wr.Stage - 20 : (int)wr.Stage)}") as Canvas;
            rect.SetValue(Canvas.TopProperty, 1D + (thirdSize * ((int)wr.Level - 1)));
            rect.SetValue(Canvas.LeftProperty, Math.Ceiling((wr.StartDate - FirstDate).TotalDays * PxPerDay));
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

        private static async Task<IReadOnlyCollection<Player>> GetPlayersAsync()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(EndpointUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };

            var response = await client
                .SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri("players", UriKind.Relative),
                    Method = HttpMethod.Get
                })
                .ConfigureAwait(false);

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Player>>(content);
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            SetStandingProcedure();
        }
    }
}
