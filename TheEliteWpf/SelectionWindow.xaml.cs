using System;
using System.Linq;
using System.Windows;
using TheEliteWpf.Datas;

namespace TheEliteWpf
{
    public partial class SelectionWindow : Window
    {
        public Game Game { get; private set; } = Game.GoldenEye;
        public StandingType StandingType { get; private set; } = StandingType.UntiedExceptSelf;

        public SelectionWindow()
        {
            InitializeComponent();
            GameCombo.ItemsSource = Enum.GetValues(typeof(Game)).Cast<Game>();
            TypeCombo.ItemsSource = Enum.GetValues(typeof(StandingType)).Cast<StandingType>();
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameCombo.SelectedIndex >= 0 && TypeCombo.SelectedIndex >= 0)
            {
                Game = (Game)GameCombo.SelectedItem;
                StandingType = (StandingType)TypeCombo.SelectedItem;
                Close();
            }
        }
    }
}
