using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Ont = BoardMaster.Core.Ontology;
using OntDyn = BoardMaster.Core.Ontology.Dynamic;
using GoRules = BoardMaster.Core.Rules.Go;
using AiGo = BoardMaster.Core.AI.Go;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core를 그대로 감싸는 9x9 바둑 클라이언트입니다. GoGameSession으로 실제 착수/패스를
    /// 처리하고, GoMctsSearcher로 백(AI) 차례를 자동으로 둡니다. 화면 갱신 로직 외에는 게임 규칙을
    /// 전혀 재구현하지 않았습니다 — 이 창은 순수하게 GoGameSession의 얇은 시각화 계층입니다.
    ///
    /// 보드 크기(9x9)와 난이도별 탐색 반복 횟수(초급 800 / 중급 4000 / 고급 10000)는 임의로 고른
    /// 값이 아닙니다 — 빈 보드와 중반 국면 양쪽에서 반복 횟수별 실제 응답 시간을 직접 측정해서
    /// (각각 대략 1초 / 3초 / 7초 — Cond_NotSuperko를 Zobrist 해시로, GetLegalMoves()의 자충수
    /// 판정을 GoLibertyCache로 최적화한 뒤 재측정한 값입니다) 비동기 대기로 감당할 만한 수준인지
    /// 확인하고 고른 값입니다.
    /// 반복 횟수가 유일한 진짜 "난이도" 레버입니다 — 정책/가치망이 없어서 "똑똑하게 약하게 두는"
    /// 방법은 없고, 그냥 얼마나 오래 탐색해 통계를 더 정확하게 만드느냐로만 세기를 조절합니다.
    /// 19x19 같은 큰 보드는 Cond_NotSuperko의 O(width*height) 시뮬레이션 때문에 훨씬 느려질 수
    /// 있습니다(엔진 쪽에 이미 문서화된 한계).
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int BOARD_SIZE = 9;
        private const double CELL_SIZE = 55;
        private const double MARGIN = 25;
        private const double STONE_RADIUS = 24;
        private const int DEFAULT_AI_ITERATIONS = 800; // 초급 — DifficultyComboBox의 SelectedIndex="0"과 짝을 맞춘 값.
        private const int AI_MAX_ROLLOUT_MOVES = 150;

        private readonly AiGo.GoMctsSearcher m_objAiSearcher = new(p_nMaxRolloutMoves: AI_MAX_ROLLOUT_MOVES);
        private readonly Random m_objAiRandom = new();

        private GoRules.GoGameSession m_objSession = null!;
        private bool m_bAiThinking;
        private int m_nAiSearchIterations = DEFAULT_AI_ITERATIONS;

        public MainWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void StartNewGame()
        {
            Ont.ST_BoardState stInitialState = new(
                1, Ont.E_PlayerColor.Black, new int[BOARD_SIZE, BOARD_SIZE], 0, 0);
            Ont.GameContext objContext = new(stInitialState);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("You", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("AI", Ont.E_PlayerColor.White));

            m_objSession = new GoRules.GoGameSession(objContext);
            m_bAiThinking = false;

            ResultText.Text = string.Empty;
            CandidateListBox.ItemsSource = null;
            SetStatus("흑(사용자) 차례입니다. 반상을 클릭해 착수하세요.", Brushes.DarkGreen);

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
                m_objSession.PlayStone(stIntersection.Value.X, stIntersection.Value.Y);
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
                ShowGameResult();
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
                ShowGameResult();
                return;
            }

            await PlayAiTurnAsync();
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        /// <summary>
        /// 현재 세션의 기보(진행 중이든 종료됐든 상관없이 지금까지 둔 수순)를 그 시점의 스냅샷으로
        /// 내보내 별도의 ReplayWindow에서 보여줍니다. 비모달로 열어서 재생 창을 띄워 둔 채로도
        /// MainWindow에서 계속 대국을 진행할 수 있습니다(재생 창은 열던 순간의 스냅샷이라 자동으로
        /// 갱신되지는 않습니다 — 다시 보려면 새로 열면 됩니다).
        /// </summary>
        private void ViewKifuButton_Click(object sender, RoutedEventArgs e)
        {
            string strKifuJson = m_objSession.ExportKifu();
            ReplayWindow objReplayWindow = new ReplayWindow(strKifuJson) { Owner = this };
            objReplayWindow.Show();
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
            SetStatus($"AI(백)가 생각 중입니다... (탐색 {m_nAiSearchIterations}회)", Brushes.DarkOrange);

            int nIterationsForThisMove = m_nAiSearchIterations;
            AiGo.GoMctsSearchResult objSearchResult = await Task.Run(
                () => m_objAiSearcher.Search(m_objSession, nIterationsForThisMove, m_objAiRandom));

            if (objSearchResult.BestMove is null || objSearchResult.BestMove.Value.IsPass)
            {
                m_objSession.Pass();
            }
            else
            {
                m_objSession.PlayStone(objSearchResult.BestMove.Value.X, objSearchResult.BestMove.Value.Y);
            }

            PopulateCandidatePanel(objSearchResult);

            m_bAiThinking = false;
            PassButton.IsEnabled = true;
            DifficultyComboBox.IsEnabled = true;

            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult();
            }
            else
            {
                SetStatus("흑(사용자) 차례입니다. 반상을 클릭해 착수하세요.", Brushes.DarkGreen);
            }
        }

        /// <summary>
        /// 직전 AI 탐색에서 루트가 실제로 펼쳐본 후보 수들을 방문 횟수 순으로 보여줍니다(이미
        /// GoMctsSearcher.Search가 방문 횟수 내림차순으로 정렬해서 반환합니다). 방문 횟수가 많을수록
        /// 탐색이 그 수에 시간을 더 많이 썼다는 뜻이고, 이게 UCT의 최종 선택 기준(BestMove)이기도
        /// 합니다 — 승률(WinRate)만 보고 고르지 않는 이유이기도 합니다.
        /// </summary>
        private void PopulateCandidatePanel(AiGo.GoMctsSearchResult p_objResult)
        {
            const int MAX_DISPLAYED = 10;

            List<string> lisLines = new();
            int nCount = Math.Min(p_objResult.CandidateMoves.Count, MAX_DISPLAYED);

            for (int i = 0; i < nCount; i++)
            {
                AiGo.GoMctsCandidateStat objCandidate = p_objResult.CandidateMoves[i];
                bool bIsChosen = p_objResult.BestMove.HasValue && objCandidate.Move == p_objResult.BestMove.Value;
                string strMoveLabel = objCandidate.Move.IsPass ? "패스" : $"({objCandidate.Move.X},{objCandidate.Move.Y})";
                string strMarker = bIsChosen ? "▶" : " ";

                lisLines.Add($"{strMarker} {strMoveLabel,-8} 방문 {objCandidate.VisitCount,4}회  승률 {objCandidate.WinRate * 100,5:0.0}%");
            }

            CandidateListBox.ItemsSource = lisLines;
        }

        private void UpdateStatusPanel()
        {
            Ont.ST_BoardState stState = m_objSession.mv_objCurrentContext.mv_stCurrentState;
            string strTurn = stState.m_eActiveColor == Ont.E_PlayerColor.Black ? "흑(사용자)" : "백(AI)";

            TurnText.Text = $"현재 차례: {strTurn} (턴 {stState.m_nTurnNumber})";
            PhaseText.Text = $"페이즈: {m_objSession.CurrentPhaseName}";
            PrisonerText.Text = $"포로 - 흑: {stState.m_nBlackPrisoners} / 백: {stState.m_nWhitePrisoners}";
        }

        private void ShowGameResult()
        {
            List<Ont.PlayerState> lisPlayers = m_objSession.mv_objCurrentContext.mv_lisPlayers;
            Ont.PlayerState? objBlack = lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black);
            Ont.PlayerState? objWhite = lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White);

            int nBlackScore = objBlack?.mv_nScore ?? 0;
            int nWhiteScore = objWhite?.mv_nScore ?? 0;

            string strWinner = nBlackScore > nWhiteScore
                ? "흑(사용자) 승리!"
                : nWhiteScore > nBlackScore ? "백(AI) 승리!" : "무승부";

            ResultText.Text = $"대국 종료 — {strWinner}\n흑: {nBlackScore}집 / 백: {nWhiteScore}집";
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

            foreach ((int X, int Y) in GetStarPoints())
            {
                DrawStarPoint(X, Y);
            }

            int[,] a_nGrid = m_objSession.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;
            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    Ont.E_PlayerColor eColor = (Ont.E_PlayerColor)a_nGrid[nX, nY];
                    if (eColor != Ont.E_PlayerColor.None)
                    {
                        DrawStone(nX, nY, eColor);
                    }
                }
            }
        }

        private static IEnumerable<(int X, int Y)> GetStarPoints()
        {
            yield return (2, 2);
            yield return (2, 6);
            yield return (6, 2);
            yield return (6, 6);
            yield return (4, 4);
        }

        private void DrawStarPoint(int p_nX, int p_nY)
        {
            double dCenterX = MARGIN + (p_nX * CELL_SIZE);
            double dCenterY = MARGIN + (p_nY * CELL_SIZE);

            Ellipse objDot = new() { Width = 6, Height = 6, Fill = Brushes.Black };
            Canvas.SetLeft(objDot, dCenterX - 3);
            Canvas.SetTop(objDot, dCenterY - 3);
            BoardCanvas.Children.Add(objDot);
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
