using System.Windows;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// 앱 시작 시 가장 먼저 뜨는 게임 선택 화면입니다. 고른 게임의 창을 열고 이 창은 숨겨 두었다가,
    /// 그 창이 닫히면 다시 보여줘서 다른 게임을 고를 수 있게 합니다(앱을 새로 시작하지 않아도
    /// 바둑 ↔ War를 오갈 수 있습니다). App.xaml의 기본 ShutdownMode(OnLastWindowClose) 그대로 두어도,
    /// 이 창이 Hide()될 뿐 실제로 닫히지는 않으므로 게임 창을 닫아도 앱 전체가 종료되지 않습니다.
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGameWindow(new MainWindow());
        }

        private void WarButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGameWindow(new WarWindow());
        }

        private void GuryongTuButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGameWindow(new GuryongTuWindow());
        }

        private void ChessButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGameWindow(new ChessWindow());
        }

        private void GreatKingdomButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGameWindow(new GreatKingdomWindow());
        }

        private void NineKnightsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGameWindow(new NineKnightsWindow());
        }

        private void OpenGameWindow(Window p_objGameWindow)
        {
            Hide();
            p_objGameWindow.Closed += (_, _) => Show();
            p_objGameWindow.Show();
        }
    }
}
