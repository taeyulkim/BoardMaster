using System.Windows;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// 제목 + 본문 텍스트만 받아 보여주는 범용 규칙 안내 창입니다. Go/War 양쪽 클라이언트가 그대로
    /// 재사용합니다 — 게임마다 창을 새로 만들 필요 없이 텍스트만 다르게 넘기면 됩니다. 비모달로
    /// 열어서(Owner만 지정) 띄워 둔 채로 원래 게임 창을 계속 조작할 수 있게 했습니다.
    /// </summary>
    public partial class RulesWindow : Window
    {
        public RulesWindow(string p_strTitle, string p_strBody)
        {
            InitializeComponent();
            Title = p_strTitle;
            TitleText.Text = p_strTitle;
            BodyText.Text = p_strBody;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
