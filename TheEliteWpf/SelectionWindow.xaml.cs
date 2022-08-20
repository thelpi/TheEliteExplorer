using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TheEliteWpf.Datas;

namespace TheEliteWpf
{
    public partial class SelectionWindow : Window
    {
        public bool Configured { get; private set; } = false;
        public Game Game { get; private set; }
        public GraphType GraphType { get; private set; }
        public bool Anonymise { get; private set; }
        public long? PlayerId { get; private set; }
        public Engine? Engine { get; private set; }

        public SelectionWindow(IReadOnlyCollection<Player> players)
        {
            InitializeComponent();
            GameCombo.ItemsSource = System.Enum.GetValues(typeof(Game)).Cast<Game>();
            EngineCombo.ItemsSource = System.Enum.GetValues(typeof(Engine)).Cast<Engine>();
            GraphCombo.ItemsSource = GraphTypeDisplay.GetValues();
            PlayerCombo.ItemsSource = players.OrderBy(_ => _.SurName);
            AnonymiseCheckBox.IsChecked = false;
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Select game!");
                return;
            }
            Game = (Game)GameCombo.SelectedItem;

            if (GraphCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Select graph type!");
                return;
            }
            GraphType = (GraphCombo.SelectedItem as GraphTypeDisplay).GraphType;

            Engine = EngineCombo.SelectedIndex < 0
                ? default(Engine?)
                : (Engine)EngineCombo.SelectedItem;

            PlayerId = PlayerCombo.SelectedIndex < 0
                ? default(long?)
                : ((Player)PlayerCombo.SelectedItem).Id;

            Anonymise = AnonymiseCheckBox.IsChecked == true;

            if (!PlayerId.HasValue && (GraphType == GraphType.Leaderboard || GraphType == GraphType.Ranking))
            {
                MessageBox.Show("Select a player for this type of graph!");
                return;
            }

            Configured = true;
            Close();
        }

        private class GraphTypeDisplay
        {
            public GraphType GraphType { get; set; }
            public string ExplainValue { get; set; }

            public static IReadOnlyCollection<GraphTypeDisplay> GetValues()
            {
                return new List<GraphTypeDisplay>
                {
                    new GraphTypeDisplay
                    {
                        GraphType = GraphType.AllUnslay,
                        ExplainValue = "Every WR (every player or specific one)"
                    },
                    new GraphTypeDisplay
                    {
                        GraphType = GraphType.FirstUnslay,
                        ExplainValue = "Standing WR (every player or specific one)"
                    },
                    new GraphTypeDisplay
                    {
                        GraphType = GraphType.Leaderboard,
                        ExplainValue = "Leaderboard history of a player"
                    },
                    new GraphTypeDisplay
                    {
                        GraphType = GraphType.Ranking,
                        ExplainValue = "Ranking history of a player"
                    },
                    new GraphTypeDisplay
                    {
                        GraphType = GraphType.Untied,
                        ExplainValue = "Untied WR (every player or specific one)"
                    }
                };
            }
        }

        private void ClearEngineCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            EngineCombo.SelectedIndex = -1;
            ClearEngineCheckBox.IsChecked = false;
        }

        private void ClearPlayerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PlayerCombo.SelectedIndex = -1;
            ClearPlayerCheckBox.IsChecked = false;
        }
    }
}
