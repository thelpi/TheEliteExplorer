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
        public Engine? Engine { get; private set; } = null;
        public long? PlayerId { get; private set; } = null;
        public int? OpacityCap { get; private set; } = null;
        public bool Anonymise { get; private set; } = false;
        public OpacityStyle? OpacityStyle { get; private set; } = null;

        public SelectionWindow()
        {
            InitializeComponent();
            GameCombo.ItemsSource = Enum.GetValues(typeof(Game)).Cast<Game>();
            TypeCombo.ItemsSource = Enum.GetValues(typeof(StandingType)).Cast<StandingType>();
            EngineCombo.ItemsSource = Enum.GetValues(typeof(Engine)).Cast<Engine>();
            OpacityStyleCombo.ItemsSource = Enum.GetValues(typeof(OpacityStyle)).Cast<OpacityStyle>();
            GameCombo.SelectedItem = Game;
            TypeCombo.SelectedItem = StandingType;
            AnonymiseCheckBox.IsChecked = Anonymise;
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameCombo.SelectedIndex >= 0 && TypeCombo.SelectedIndex >= 0)
            {
                Game = (Game)GameCombo.SelectedItem;
                StandingType = (StandingType)TypeCombo.SelectedItem;
                Engine = EngineCombo.SelectedIndex >= 0 ? (Engine)EngineCombo.SelectedItem : default(Engine?);
                OpacityStyle = OpacityStyleCombo.SelectedIndex >= 0 ? (OpacityStyle)OpacityStyleCombo.SelectedItem : default(OpacityStyle?);
                PlayerId = long.TryParse(PlayerIdText.Text, out long pId) && pId > 0 ? pId : default(long?);
                OpacityCap = int.TryParse(OpacityCapText.Text, out int oCap) && oCap > 0 ? oCap : default(int?);
                Anonymise = AnonymiseCheckBox.IsChecked == true;
                Close();
            }
        }
    }
}
