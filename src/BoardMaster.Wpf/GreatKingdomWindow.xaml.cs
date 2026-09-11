using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Ont = BoardMaster.Core.Ontology;
using OntDyn = BoardMaster.Core.Ontology.Dynamic;
using GkRules = BoardMaster.Core.Rules.GreatKingdom;
using AiGk = BoardMaster.Core.AI.GreatKingdom;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core의 GreatKingdomGameSession을 그대로 감싸는 그레이트 킹덤(이세돌, WIZSTONE)
    /// 클라이언트입니다. Go의 MainWindow와 구조가 거의 같습니다(같은 9x9 격자, 같은 MCTS 패턴) —
    /// 다른 점은 반상 정중앙의 중립 성 표시와, 종국 사유가 둘로 나뉜다는 것(포위 즉시 승리 /
    /// 패스 후 영토 판정)뿐입니다. 화면 갱신 외에는 규칙을 전혀 재구현하지 않았습니다.
    /// </summary>
    public partial class GreatKingdomWindow : Window
    {
        private const int BOARD_SIZE = 9;
        private const double CELL_SIZE = 55;
        private const double MARGIN = 25;
        private const double STONE_RADIUS = 24;
        private const int DEFAULT_AI_ITERATIONS = 400; // 초급 — DifficultyComboBox의 SelectedIndex="0"과 짝을 맞춘 값.
        private const int AI_MAX_ROLLOUT_MOVES = 150;

        private readonly AiGk.GreatKingdomMctsSearcher m_objAiSearcher = new(p_nMaxRolloutMoves: AI_MAX_ROLLOUT_MOVES);
        private readonly Random m_objAiRandom = new();

        private GkRules.GreatKingdomGameSession m_objSession = null!;
        private bool m_bAiThinking;
        private int m_nAiSearchIterations = DEFAULT_AI_ITERATIONS;

        public GreatKingdomWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void StartNewGame()
        {
            m_objSession = new GkRules.GreatKingdomGameSession(GkRules.GreatKingdomGameFactory.CreateStandardGame());
            m_bAiThinking = false;

            ResultText.Text = string.Empty;
            CandidateListBox.ItemsSource = null;
            SetStatus("선공(사용자) 차례입니다. 반상을 클릭해 착수하세요.", Brushes.DarkGreen);

            RedrawBoard();
            UpdateStatusPanel();
        }

        private async void BoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsHumanTurnAvailable())
            {
                return;
            }

            Point objClickPoint = e.GetPosition(BoardCanvas);
            (int X, int Y)? stIntersection = FindNearestIntersection(objClickPoint);
            if (stIntersection is null)
            {
                return;
            }

            try
            {
                m_objSession.PlaceStone(stIntersection.Value.X, stIntersection.Value.Y);
            }
            catch (OntDyn.RuleViolationException ex)
            {
                SetStatus($"불법적인 수입니다: {ex.Message}", Brushes.Firebrick);
                return;
            }

            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult(p_bEndedBySiege: true);
                return;
            }

            await PlayAiTurnAsync();
        }

        private async void PassButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsHumanTurnAvailable())
            {
                return;
            }

            m_objSession.Pass();
            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult(p_bEndedBySiege: false);
                return;
            }

            await PlayAiTurnAsync();
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void ViewRulesButton_Click(object sender, RoutedEventArgs e)
        {
            RulesWindow objRulesWindow = new RulesWindow("그레이트 킹덤 규칙", RULES_TEXT) { Owner = this };
            objRulesWindow.Show();
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DifficultyComboBox.SelectedItem is ComboBoxItem objItem
                && objItem.Tag is string strTag
                && int.TryParse(strTag, out int nIterations))
            {
                m_nAiSearchIterations = nIterations;
            }
        }

        private bool IsHumanTurnAvailable()
        {
            if (m_bAiThinking || m_objSession.CurrentPhaseName != "MainPlay")
            {
                return false;
            }

            return m_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor == Ont.E_PlayerColor.Black;
        }

        private async Task PlayAiTurnAsync()
        {
            m_bAiThinking = true;
            PassButton.IsEnabled = false;
            DifficultyComboBox.IsEnabled = false;
            SetStatus($"AI(후공)가 생각 중입니다... (탐색 {m_nAiSearchIterations}회)", Brushes.DarkOrange);

            int nIterationsForThisMove = m_nAiSearchIterations;
            AiGk.GreatKingdomMctsSearchResult objSearchResult = await Task.Run(
                () => m_objAiSearcher.Search(m_objSession, nIterationsForThisMove, m_objAiRandom));

            bool bAiPassed = objSearchResult.BestMove is null || objSearchResult.BestMove.Value.IsPass;
            if (bAiPassed)
            {
                m_objSession.Pass();
            }
            else
            {
                m_objSession.PlaceStone(objSearchResult.BestMove!.Value.X, objSearchResult.BestMove.Value.Y);
            }

            PopulateCandidatePanel(objSearchResult);

            m_bAiThinking = false;
            PassButton.IsEnabled = true;
            DifficultyComboBox.IsEnabled = true;

            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult(p_bEndedBySiege: !bAiPassed);
            }
            else
            {
                SetStatus("선공(사용자) 차례입니다. 반상을 클릭해 착수하세요.", Brushes.DarkGreen);
            }
        }

        private void PopulateCandidatePanel(AiGk.GreatKingdomMctsSearchResult p_objResult)
        {
            const int MAX_DISPLAYED = 10;

            List<string> lisLines = new();
            int nCount = Math.Min(p_objResult.CandidateMoves.Count, MAX_DISPLAYED);

            for (int i = 0; i < nCount; i++)
            {
                AiGk.GreatKingdomMctsCandidateStat objCandidate = p_objResult.CandidateMoves[i];
                bool bIsChosen = p_objResult.BestMove.HasValue && objCandidate.Move == p_objResult.BestMove.Value;
                string strMoveLabel = objCandidate.Move.IsPass ? "패스" : $"({objCandidate.Move.X},{objCandidate.Move.Y})";
                string strMarker = bIsChosen ? "▶" : " ";

                lisLines.Add($"{strMarker} {strMoveLabel,-8} 방문 {objCandidate.VisitCount,4}회  승률 {objCandidate.WinRate * 100,5:0.0}%");
            }

            CandidateListBox.ItemsSource = lisLines;
        }

        private void UpdateStatusPanel()
        {
            Ont.E_PlayerColor eActiveColor = m_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            string strTurn = eActiveColor == Ont.E_PlayerColor.Black ? "선공(사용자)" : "후공(AI)";

            TurnText.Text = $"현재 차례: {strTurn} (턴 {m_objSession.mv_objCurrentContext.mv_stCurrentState.m_nTurnNumber})";
            PhaseText.Text = $"페이즈: {m_objSession.CurrentPhaseName}";
        }

        private void ShowGameResult(bool p_bEndedBySiege)
        {
            List<Ont.PlayerState> lisPlayers = m_objSession.mv_objCurrentContext.mv_lisPlayers;
            Ont.PlayerState objPlayer1 = lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Ont.PlayerState objPlayer2 = lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;

            string strWinner = objPlayer1.mv_nScore > objPlayer2.mv_nScore ? "선공(사용자)" : "후공(AI)";
            string strReason = p_bEndedBySiege ? "성 포위" : "영토 판정";

            ResultText.Text = $"대국 종료 — {strWinner} 승리! ({strReason})";
            SetStatus("새 게임을 시작하려면 '새 게임' 버튼을 누르세요.", Brushes.Gray);
        }

        private void SetStatus(string p_strMessage, Brush p_objColor)
        {
            StatusText.Text = p_strMessage;
            StatusText.Foreground = p_objColor;
        }

        private (int X, int Y)? FindNearestIntersection(Point p_objClickPoint)
        {
            int nX = (int)Math.Round((p_objClickPoint.X - MARGIN) / CELL_SIZE);
            int nY = (int)Math.Round((p_objClickPoint.Y - MARGIN) / CELL_SIZE);

            if (nX < 0 || nX >= BOARD_SIZE || nY < 0 || nY >= BOARD_SIZE)
            {
                return null;
            }

            double dCenterX = MARGIN + (nX * CELL_SIZE);
            double dCenterY = MARGIN + (nY * CELL_SIZE);
            double dDistance = Math.Sqrt(Math.Pow(p_objClickPoint.X - dCenterX, 2) + Math.Pow(p_objClickPoint.Y - dCenterY, 2));

            return dDistance > CELL_SIZE * 0.45 ? null : (nX, nY);
        }

        private void RedrawBoard()
        {
            BoardCanvas.Children.Clear();

            double dEnd = MARGIN + ((BOARD_SIZE - 1) * CELL_SIZE);
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                double dPos = MARGIN + (i * CELL_SIZE);

                BoardCanvas.Children.Add(new Line { X1 = MARGIN, Y1 = dPos, X2 = dEnd, Y2 = dPos, Stroke = Brushes.Black, StrokeThickness = 1 });
                BoardCanvas.Children.Add(new Line { X1 = dPos, Y1 = MARGIN, X2 = dPos, Y2 = dEnd, Stroke = Brushes.Black, StrokeThickness = 1 });
            }

            int[,] a_nGrid = m_objSession.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;
            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    int nCellValue = a_nGrid[nX, nY];
                    if (nCellValue != GkRules.GreatKingdomCell.Empty)
                    {
                        DrawCastle(nX, nY, nCellValue);
                    }
                }
            }
        }

        private void DrawCastle(int p_nX, int p_nY, int p_nCellValue)
        {
            double dCenterX = MARGIN + (p_nX * CELL_SIZE);
            double dCenterY = MARGIN + (p_nY * CELL_SIZE);

            Brush objFill = p_nCellValue switch
            {
                GkRules.GreatKingdomCell.Player1 => new SolidColorBrush(Color.FromRgb(0x2C, 0x5F, 0x9E)), // 파란 성
                GkRules.GreatKingdomCell.Player2 => new SolidColorBrush(Color.FromRgb(0xE0, 0x83, 0x25)), // 주황 성
                _ => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)) // 중립 성
            };

            Ellipse objStone = new()
            {
                Width = STONE_RADIUS * 2,
                Height = STONE_RADIUS * 2,
                Fill = objFill,
                Stroke = Brushes.Black,
                StrokeThickness = p_nCellValue == GkRules.GreatKingdomCell.Neutral ? 2 : 1,
                Effect = new DropShadowEffect { ShadowDepth = 1.5, BlurRadius = 3, Opacity = 0.4 }
            };

            Canvas.SetLeft(objStone, dCenterX - STONE_RADIUS);
            Canvas.SetTop(objStone, dCenterY - STONE_RADIUS);
            BoardCanvas.Children.Add(objStone);
        }

        private const string RULES_TEXT =
@"9x9 반상 정중앙에 누구의 것도 아닌 회색 중립 성 하나로 시작합니다. 선공(파란 성, 사용자)과 후공(주황 성, AI)이 번갈아 성을 하나씩 놓거나 패스합니다.

포위와 즉시 승리
바둑처럼 상하좌우로 이어진 같은 편 성 무리(그룹)가 활로(인접한 빈 칸)를 모두 잃으면 그 그룹 전체가 제거됩니다. 다만 그레이트 킹덤은 여기서 바둑과 완전히 갈라집니다 — 성이 하나라도 포위되어 잡히면 그 즉시 대국이 끝나고 잡은 쪽이 승리합니다. 계가까지 가지 않습니다.

자충수 금지
활로가 하나도 안 남는 수는 금지됩니다 — 단, 그 수로 상대 그룹을 포위해 따낼 수 있다면 예외로 허용됩니다.

상대의 완성된 영토엔 착수 불가 (바둑과 다른 점)
빈 칸으로 이어진 한 영역이 한쪽 성에만 접해 있으면(중립 성에 접해도 안 됨) 그건 그 쪽의 완성된 영토입니다. 이미 완성된 상대의 영토에는 아예 착수할 수 없습니다 — 그래서 바둑보다 집을 지키기가 훨씬 쉽습니다.

종국과 승패
아무도 포위되지 않고 양쪽이 연속으로 패스하면, 완성된 영토의 크기를 비교합니다. 선공이 후공보다 영토가 3칸 이상 많아야 선공이 승리하고, 그렇지 않으면(동률 포함) 후공이 승리합니다 — 무승부는 없습니다.

AI
정책/가치망 없는 순수 MCTS(UCT, 균등 무작위 롤아웃)입니다. 난이도는 탐색 반복 횟수만 다릅니다.";
    }
}
