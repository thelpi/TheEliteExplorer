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
        private const int PanelHeight = 15;
        private const double PxPerDay = 0.2;

        public MainWindow()
        {
            InitializeComponent();

            var selectionWindow = new SelectionWindow();
            selectionWindow.ShowDialog();

            MainGrid.Children.Cast<FrameworkElement>()
                .Where(uie => uie.GetType() == typeof(Image)
                    && uie.Tag != null
                    && int.TryParse(uie.Tag.ToString(), out int tagId)
                    && tagId != (int)selectionWindow.Game)
                .All(uie => { uie.Visibility = Visibility.Collapsed; return true; });

            LoadStandingsAsync(selectionWindow.Game, selectionWindow.StandingType);
        }

        private async Task LoadStandingsAsync(Game game, StandingType standingType)
        {
            var wrs = await GetStandingWorldRecordsAsync(game, standingType)
                .ConfigureAwait(false);

            foreach (var wr in wrs)
            {
                Dispatcher.Invoke(() =>
                {
                    DrawStandingRectangle(game, wr);
                });
            }
        }

        private void DrawStandingRectangle(Game game, Standing wr)
        {
            var rect = new Rectangle
            {
                Width = PxPerDay * wr.Days.Value,
                Height = PanelHeight - 2,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom($"#{wr.Author.Color}")
            };
            var canvas = FindName($"Stage{(game == Game.PerfectDark ? (int)wr.Stage - 20 : (int)wr.Stage)}Level{(int)wr.Level}") as Canvas;
            rect.SetValue(Canvas.TopProperty, 1D);
            rect.SetValue(Canvas.LeftProperty, (wr.StartDate - EliteBeginDate[game]).TotalDays * PxPerDay);
            canvas.Children.Add(rect);
        }

        private static async Task<IReadOnlyCollection<Standing>> GetStandingWorldRecordsAsync(Game game, StandingType standingType)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(EndpointUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };

            var response = await client
                .SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri($"games/{(int)game}/longest-standings?standingType={(int)standingType}&count={int.MaxValue}", UriKind.Relative),
                    Method = HttpMethod.Get
                })
                .ConfigureAwait(false);

            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IReadOnlyCollection<Standing>>(content);
        }
    }
}
