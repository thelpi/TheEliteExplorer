﻿using System;
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
        private const int PanelHeight = 15;
        private const double PxPerDay = 0.2;

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

            ChangeButton.IsEnabled = false;
            Task.Run(async () =>
                await LoadStandingsAsync(
                        selectionWindow.Game,
                        selectionWindow.StandingType,
                        selectionWindow.Engine,
                        selectionWindow.PlayerId,
                        selectionWindow.OpacityCap)
                    .ConfigureAwait(false));
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

        private async Task LoadStandingsAsync(Game game, StandingType standingType, Engine? engine, long? playerId, int? opacityCap)
        {
            var wrs = await GetStandingWorldRecordsAsync(game, standingType, engine)
                .ConfigureAwait(false);

            if (playerId.HasValue)
                wrs = wrs.Where(x => x.Author.Id == playerId).ToList();

            foreach (var wr in wrs)
            {
                Dispatcher.Invoke(() =>
                {
                    DrawStandingRectangle(game, wr, playerId.HasValue, opacityCap);
                });
            }

            Dispatcher.Invoke(() => ChangeButton.IsEnabled = true);
        }

        private void DrawStandingRectangle(Game game, Standing wr, bool anonymize, int? opacityCap = null)
        {
            var rect = new Rectangle
            {
                Width = PxPerDay * wr.Days,
                Height = PanelHeight - 2,
                Fill = anonymize || opacityCap.HasValue
                    ? Brushes.Red
                    : (SolidColorBrush)new BrushConverter().ConvertFrom($"#{wr.Author.Color}"),
                ToolTip = opacityCap.HasValue ? null : wr,
                Opacity = opacityCap.HasValue ? 1 / (double)opacityCap.Value : 1.0
            };
            var canvas = FindName($"Stage{(game == Game.PerfectDark ? (int)wr.Stage - 20 : (int)wr.Stage)}Level{(int)wr.Level}") as Canvas;
            rect.SetValue(Canvas.TopProperty, 1D);
            rect.SetValue(Canvas.LeftProperty, (wr.StartDate - EliteBeginDate[game]).TotalDays * PxPerDay);
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
