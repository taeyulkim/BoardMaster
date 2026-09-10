using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using Ont = BoardMaster.Core.Ontology;
using GoRules = BoardMaster.Core.Rules.Go;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// 기보(GoKifuRecord) JSON을 불러와 처음부터 한 수씩 앞뒤로 넘겨보는 뷰어입니다. GoGameSession의
    /// 규칙을 전혀 재구현하지 않습니다 — GoKifuSerializer.ReplaySnapshots가 기록된 수순을 실제로
    /// GoGameSession에 재생해서 얻은 GameContext 스냅샷 목록(인덱스 0 = 초기 상태)을 그대로 순서대로
    /// 렌더링만 합니다. 그래서 탐색은 매번 다시 재생하지 않고 이미 계산된 스냅샷 리스트를 인덱싱하는
    /// 것뿐이라 즉각적입니다.
    ///
    /// MainWindow와 마찬가지로 순수한 시각화 계층입니다 — 다만 보드 크기를 9x9로 고정하지 않고
    /// 불러온 기보의 실제 Width/Height를 따릅니다(외부에서 다른 크기의 기보를 불러올 수도 있으므로).
    /// </summary>
    public partial class ReplayWindow : Window
    {
        private const double CELL_SIZE = 42;
        private const double MARGIN = 25;
        private const double STONE_RADIUS = 18;

        private GoRules.GoKifuRecord? m_objRecord;
        private IReadOnlyList<Ont.GameContext> m_lisSnapshots = Array.Empty<Ont.GameContext>();
        private int m_nCurrentIndex;
        private string? m_strLoadedJson;
        private bool m_bSyncingSelection;

        public ReplayWindow(string? p_strInitialKifuJson = null)
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(p_strInitialKifuJson))
            {
                TryLoadFromJson(p_strInitialKifuJson);
            }
            else
            {
                MoveCounterText.Text = "불러온 기보가 없습니다";
                InfoText.Text = "\"파일에서 열기\"로 기보 JSON을 불러오세요.";
                SaveButton.IsEnabled = false;
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog objDialog = new OpenFileDialog
            {
                Filter = "기보 JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                Title = "기보 파일 열기"
            };

            if (objDialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                string strJson = File.ReadAllText(objDialog.FileName);
                TryLoadFromJson(strJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"기보를 불러오지 못했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_strLoadedJson is null)
            {
                return;
            }

            SaveFileDialog objDialog = new SaveFileDialog
            {
                Filter = "기보 JSON 파일 (*.json)|*.json",
                FileName = "kifu.json"
            };

            if (objDialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(objDialog.FileName, m_strLoadedJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"저장하지 못했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FirstButton_Click(object sender, RoutedEventArgs e) => JumpToIndex(0);

        private void PrevButton_Click(object sender, RoutedEventArgs e) => JumpToIndex(m_nCurrentIndex - 1);

        private void NextButton_Click(object sender, RoutedEventArgs e) => JumpToIndex(m_nCurrentIndex + 1);

        private void LastButton_Click(object sender, RoutedEventArgs e) => JumpToIndex(m_lisSnapshots.Count - 1);

        private void MoveListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_bSyncingSelection || MoveListBox.SelectedIndex < 0)
            {
                return;
            }

            // 수순 목록의 i번째 항목("N수째")은 스냅샷 인덱스 i+1(그 수를 둔 직후 상태)에 대응한다 —
            // 스냅샷 인덱스 0은 목록에 없는 "첫 수를 두기 전" 상태이기 때문이다.
            JumpToIndex(MoveListBox.SelectedIndex + 1);
        }

        /// <summary>
        /// 현재 대국(진행 중이거나 종료된) 또는 임의의 기보 JSON을 곧바로 불러와 보여줄 때 씁니다.
        /// 손상된 JSON이나 재생 중 규칙 위반이 발생하면 메시지를 띄우고 이전 상태를 그대로 유지합니다.
        /// </summary>
        public void TryLoadFromJson(string p_strJson)
        {
            try
            {
                GoRules.GoKifuRecord objRecord = GoRules.GoKifuSerializer.Parse(p_strJson);
                IReadOnlyList<Ont.GameContext> lisSnapshots = GoRules.GoKifuSerializer.ReplaySnapshots(objRecord);

                m_objRecord = objRecord;
                m_lisSnapshots = lisSnapshots;
                m_strLoadedJson = p_strJson;
                SaveButton.IsEnabled = true;

                PopulateMoveList();
                JumpToIndex(m_lisSnapshots.Count - 1); // 기본은 마지막 수(최종 국면)부터 보여준다.
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"기보를 재생하지 못했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateMoveList()
        {
            if (m_objRecord is null)
            {
                return;
            }

            List<string> lisLines = new();
            for (int i = 0; i < m_objRecord.Moves.Count; i++)
            {
                GoRules.GoKifuMoveRecord objMove = m_objRecord.Moves[i];
                string strColorLabel = objMove.Color == "Black" ? "흑" : "백";
                string strMoveLabel = objMove.IsPass ? "패스" : $"({objMove.X},{objMove.Y})";
                lisLines.Add($"{i + 1,3}. {strColorLabel} {strMoveLabel}");
            }

            MoveListBox.ItemsSource = lisLines;

            string strResultLine = m_objRecord.IsGameOver
                ? $"종국 — 흑 {m_objRecord.BlackScore}집 / 백 {m_objRecord.WhiteScore}집"
                : "진행 중인 대국의 기보입니다.";
            InfoText.Text = $"{m_objRecord.Width}x{m_objRecord.Height} 보드, {m_objRecord.Moves.Count}수\n{strResultLine}";
        }

        private void JumpToIndex(int p_nIndex)
        {
            if (m_lisSnapshots.Count == 0)
            {
                return;
            }

            m_nCurrentIndex = Math.Clamp(p_nIndex, 0, m_lisSnapshots.Count - 1);

            m_bSyncingSelection = true;
            MoveListBox.SelectedIndex = m_nCurrentIndex - 1; // 인덱스 0(초기 상태)은 목록에 대응 항목이 없다.
            if (MoveListBox.SelectedIndex >= 0)
            {
                MoveListBox.ScrollIntoView(MoveListBox.SelectedItem);
            }
            m_bSyncingSelection = false;

            MoveCounterText.Text = $"수 {m_nCurrentIndex} / {m_lisSnapshots.Count - 1}";
            RedrawBoard();
        }

        private void RedrawBoard()
        {
            BoardCanvas.Children.Clear();

            if (m_objRecord is null || m_lisSnapshots.Count == 0)
            {
                return;
            }

            int nWidth = m_objRecord.Width;
            int nHeight = m_objRecord.Height;

            BoardCanvas.Width = MARGIN * 2 + ((nWidth - 1) * CELL_SIZE);
            BoardCanvas.Height = MARGIN * 2 + ((nHeight - 1) * CELL_SIZE);

            double dEndX = MARGIN + ((nWidth - 1) * CELL_SIZE);
            double dEndY = MARGIN + ((nHeight - 1) * CELL_SIZE);

            for (int nX = 0; nX < nWidth; nX++)
            {
                double dPos = MARGIN + (nX * CELL_SIZE);
                BoardCanvas.Children.Add(new Line { X1 = dPos, Y1 = MARGIN, X2 = dPos, Y2 = dEndY, Stroke = Brushes.Black, StrokeThickness = 1 });
            }

            for (int nY = 0; nY < nHeight; nY++)
            {
                double dPos = MARGIN + (nY * CELL_SIZE);
                BoardCanvas.Children.Add(new Line { X1 = MARGIN, Y1 = dPos, X2 = dEndX, Y2 = dPos, Stroke = Brushes.Black, StrokeThickness = 1 });
            }

            int[,] a_nGrid = m_lisSnapshots[m_nCurrentIndex].mv_stCurrentState.m_a_nBoardGrid;
            for (int nY = 0; nY < nHeight; nY++)
            {
                for (int nX = 0; nX < nWidth; nX++)
                {
                    Ont.E_PlayerColor eColor = (Ont.E_PlayerColor)a_nGrid[nX, nY];
                    if (eColor != Ont.E_PlayerColor.None)
                    {
                        DrawStone(nX, nY, eColor);
                    }
                }
            }
        }

        private void DrawStone(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            double dCenterX = MARGIN + (p_nX * CELL_SIZE);
            double dCenterY = MARGIN + (p_nY * CELL_SIZE);

            Ellipse objStone = new()
            {
                Width = STONE_RADIUS * 2,
                Height = STONE_RADIUS * 2,
                Fill = p_eColor == Ont.E_PlayerColor.Black ? Brushes.Black : Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = p_eColor == Ont.E_PlayerColor.Black ? 0 : 1,
                Effect = new DropShadowEffect { ShadowDepth = 1.5, BlurRadius = 3, Opacity = 0.4 }
            };

            Canvas.SetLeft(objStone, dCenterX - STONE_RADIUS);
            Canvas.SetTop(objStone, dCenterY - STONE_RADIUS);
            BoardCanvas.Children.Add(objStone);
        }
    }
}
