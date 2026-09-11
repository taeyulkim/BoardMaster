using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ont = BoardMaster.Core.Ontology;
using OntDyn = BoardMaster.Core.Ontology.Dynamic;
using NK = BoardMaster.Core.Rules.NineKnights;
using AiNk = BoardMaster.Core.AI.NineKnights;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core의 NineKnightsGameSession을 그대로 감싸는 나인 나이츠(이세돌, WIZSTONE)
    /// 클라이언트입니다. 이 장르의 핵심은 "지속적인 은닉 정보"입니다 — 화면은 내(Black) 기사의
    /// 번호는 항상 보여주지만, 상대(White) 기사는 실제로 전투가 벌어져 공개되기 전까지 번호 대신
    /// '?'만 그립니다. AI 쪽도 마찬가지로 정직합니다(NineKnightsHeuristicAi 문서 참고) — 이 창은
    /// 그 원칙을 화면에서도 깨지 않도록, session.IsRevealed로 걸러지지 않은 상대 기물의 실제
    /// Entity.mv_strType 값은 아예 그리지 않습니다.
    ///
    /// AI는 반복 탐색이 아니라 한 수 앞만 보는 기대값 휴리스틱이라(MCTS 계열과 달리 "반복 횟수"라는
    /// 난이도 축이 없습니다), Chess/GreatKingdom 창에 있는 난이도 선택 콤보박스를 이 창에는 넣지
    /// 않았습니다.
    /// </summary>
    public partial class NineKnightsWindow : Window
    {
        private const double CELL_SIZE = 52;
        private const int BOARD_SIZE = 9;

        private const Ont.E_PlayerColor HUMAN_COLOR = Ont.E_PlayerColor.Black;
        private const Ont.E_PlayerColor AI_COLOR = Ont.E_PlayerColor.White;

        private readonly AiNk.NineKnightsHeuristicAi m_objAi = new();
        private readonly Random m_objAiRandom = new();

        private NK.NineKnightsGameSession m_objSession = null!;
        private (int X, int Y)? m_stSelectedSquare;
        private List<(int X, int Y)> m_lisSelectedPieceDestinations = new();
        private bool m_bAiThinking;

        public NineKnightsWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void StartNewGame()
        {
            m_objSession = new NK.NineKnightsGameSession(NK.NineKnightsGameFactory.CreateStandardGame());
            m_stSelectedSquare = null;
            m_lisSelectedPieceDestinations = new List<(int X, int Y)>();
            m_bAiThinking = false;

            ResultText.Text = string.Empty;
            LogListBox.Items.Clear();
            SetStatus("Black(사용자) 차례입니다. 기사를 클릭해 선택하세요.", Brushes.DarkGreen);

            MissionText.Text = $"임무: 내 {m_objSession.GetMissionNumber(HUMAN_COLOR)}번 기사가 상대 뒷줄(맨 위 줄)에 살아서 도달하면 승리합니다.";
            HiddenTokenText.Text = $"히든 토큰: {m_objSession.GetHiddenNumber(HUMAN_COLOR)}번 — 이 번호의 내 기사가 상대의 8번을 공격하면 무조건 이깁니다.";

            RedrawBoard();
            UpdateStatusPanel();
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void ViewRulesButton_Click(object sender, RoutedEventArgs e)
        {
            RulesWindow objRulesWindow = new RulesWindow("나인 나이츠 규칙", RULES_TEXT) { Owner = this };
            objRulesWindow.Show();
        }

        private void BoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (m_bAiThinking || m_objSession.CurrentPhaseName != "MainPlay")
            {
                return;
            }

            Point objClickPoint = e.GetPosition(BoardCanvas);
            (int X, int Y) stClicked = ScreenToSquare(objClickPoint);
            if (!InBounds(stClicked.X, stClicked.Y))
            {
                return;
            }

            if (m_stSelectedSquare is { } stSelected && m_lisSelectedPieceDestinations.Contains(stClicked))
            {
                TryMakeHumanMove(stSelected.X, stSelected.Y, stClicked.X, stClicked.Y);
                return;
            }

            // 선택 안 된 상태에서 클릭했거나, 합법 목적지가 아닌 다른 칸을 클릭했다 — 그 칸에 내
            // (Black) 기사가 있으면 새로 선택하고, 아니면 선택을 해제한다.
            Ont.Entity? objPiece = m_objSession.GetPieceAt(stClicked.X, stClicked.Y);
            if (objPiece is not null && objPiece.mv_eColor == HUMAN_COLOR)
            {
                SelectSquare(stClicked.X, stClicked.Y);
            }
            else
            {
                ClearSelection();
            }

            RedrawBoard();
        }

        private void SelectSquare(int p_nX, int p_nY)
        {
            m_stSelectedSquare = (p_nX, p_nY);
            m_lisSelectedPieceDestinations = m_objSession.GetLegalDestinations(p_nX, p_nY);
        }

        private void ClearSelection()
        {
            m_stSelectedSquare = null;
            m_lisSelectedPieceDestinations = new List<(int X, int Y)>();
        }

        private async void TryMakeHumanMove(int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            try
            {
                ExecuteMove(HUMAN_COLOR, p_nFromX, p_nFromY, p_nToX, p_nToY);
            }
            catch (OntDyn.RuleViolationException ex)
            {
                SetStatus($"불법적인 수입니다: {ex.Message}", Brushes.Firebrick);
                ClearSelection();
                RedrawBoard();
                return;
            }

            ClearSelection();
            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult();
                return;
            }

            await PlayAiTurnAsync();
        }

        private async Task PlayAiTurnAsync()
        {
            m_bAiThinking = true;
            NewGameButton.IsEnabled = false;
            SetStatus("AI(White)가 생각 중입니다...", Brushes.DarkOrange);

            (int FromX, int FromY, int ToX, int ToY)? stMove = await Task.Run(
                () => m_objAi.ChooseMove(m_objSession, AI_COLOR, m_objAiRandom));

            if (stMove is { } stChosenMove)
            {
                ExecuteMove(AI_COLOR, stChosenMove.FromX, stChosenMove.FromY, stChosenMove.ToX, stChosenMove.ToY);
            }

            m_bAiThinking = false;
            NewGameButton.IsEnabled = true;

            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult();
            }
            else
            {
                SetStatus("Black(사용자) 차례입니다. 기사를 클릭해 선택하세요.", Brushes.DarkGreen);
            }
        }

        /// <summary>
        /// 실제 이동을 적용하고 기록을 남깁니다 — 전투가 없는 조용한 수는 White(AI)가 뒀을 때
        /// 번호를 절대 로그에 남기지 않습니다(사용자가 아직 몰라야 할 정보이므로). 전투가 벌어진
        /// 수는 규칙상 두 기사 모두 이제 공개된 정보이므로, 누가 뒀든 실제 번호와 승패를 그대로
        /// 기록합니다.
        /// </summary>
        private void ExecuteMove(Ont.E_PlayerColor p_eMoverColor, int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            Ont.Entity objMoverBefore = m_objSession.GetPieceAt(p_nFromX, p_nFromY)!;
            Ont.Entity? objDefenderBefore = m_objSession.GetPieceAt(p_nToX, p_nToY);

            m_objSession.MovePiece(p_nFromX, p_nFromY, p_nToX, p_nToY);

            AppendLogEntry(p_eMoverColor, objMoverBefore, objDefenderBefore, p_nFromX, p_nFromY, p_nToX, p_nToY);
        }

        private void AppendLogEntry(
            Ont.E_PlayerColor p_eMoverColor, Ont.Entity p_objMoverBefore, Ont.Entity? p_objDefenderBefore,
            int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            string strMoverLabel = ColorLabel(p_eMoverColor);
            string strFrom = $"({p_nFromX},{p_nFromY})";
            string strTo = $"({p_nToX},{p_nToY})";

            if (p_objDefenderBefore is null)
            {
                bool bShowMoverNumber = p_eMoverColor == HUMAN_COLOR;
                string strMoverNumber = bShowMoverNumber ? $"{p_objMoverBefore.mv_strType}번 " : string.Empty;
                LogListBox.Items.Add($"{strMoverLabel} {strMoverNumber}기사 이동: {strFrom} -> {strTo}");
            }
            else
            {
                // 전투 발생 — 이제 두 기사 모두 공개되었으므로(MovePiece가 IsRevealed 처리를 마쳤다) 실제
                // 번호를 그대로 로그에 남겨도 된다.
                bool bMoverWon = m_objSession.GetPieceAt(p_nToX, p_nToY)?.mv_strEntityID == p_objMoverBefore.mv_strEntityID;
                Ont.E_PlayerColor eDefenderColor = Opponent(p_eMoverColor);
                string strWinnerLabel = bMoverWon ? strMoverLabel : ColorLabel(eDefenderColor);

                LogListBox.Items.Add(
                    $"전투! {strMoverLabel}({p_objMoverBefore.mv_strType}) {strFrom} -> {strTo} vs " +
                    $"{ColorLabel(eDefenderColor)}({p_objDefenderBefore.mv_strType}) : {strWinnerLabel} 승리");
            }

            LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        }

        private static string ColorLabel(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == HUMAN_COLOR ? "Black(사용자)" : "White(AI)";
        }

        private static Ont.E_PlayerColor Opponent(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;
        }

        private void UpdateStatusPanel()
        {
            Ont.E_PlayerColor eActiveColor = m_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            string strTurnLabel = eActiveColor == HUMAN_COLOR ? "Black(사용자)" : "White(AI)";
            TurnText.Text = $"현재 차례: {strTurnLabel} (턴 {m_objSession.mv_objCurrentContext.mv_stCurrentState.m_nTurnNumber})";

            (int MyOnBoard, int MyReserve) = CountPieces(HUMAN_COLOR);
            (int OpponentOnBoard, int OpponentReserve) = CountPieces(AI_COLOR);
            PieceCountText.Text =
                $"내 기사: 반상 {MyOnBoard} / 예비 {MyReserve}      상대 기사: 반상 {OpponentOnBoard} / 예비 {OpponentReserve}";
        }

        private (int OnBoard, int Reserve) CountPieces(Ont.E_PlayerColor p_eColor)
        {
            int nOnBoard = 0;
            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    if (m_objSession.GetPieceAt(nX, nY)?.mv_eColor == p_eColor)
                    {
                        nOnBoard++;
                    }
                }
            }

            int nReserve = m_objSession.GetRemainingPieceCount(p_eColor) - nOnBoard;
            return (nOnBoard, nReserve);
        }

        private void ShowGameResult()
        {
            Ont.PlayerState objBlack = m_objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Ont.PlayerState objWhite = m_objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;

            string strResult = objBlack.mv_nScore > objWhite.mv_nScore
                ? "Black(사용자) 승리!"
                : "White(AI) 승리!";

            ResultText.Text = $"대국 종료 — {strResult}";
            SetStatus("새 게임을 시작하려면 '새 게임' 버튼을 누르세요.", Brushes.Gray);
        }

        private void SetStatus(string p_strMessage, Brush p_objColor)
        {
            StatusText.Text = p_strMessage;
            StatusText.Foreground = p_objColor;
        }

        private static (int X, int Y) ScreenToSquare(Point p_objPoint)
        {
            int nX = (int)(p_objPoint.X / CELL_SIZE);
            int nY = BOARD_SIZE - 1 - (int)(p_objPoint.Y / CELL_SIZE);
            return (nX, nY);
        }

        private static bool InBounds(int p_nX, int p_nY)
        {
            return p_nX >= 0 && p_nX < BOARD_SIZE && p_nY >= 0 && p_nY < BOARD_SIZE;
        }

        private static double SquareLeft(int p_nX) => p_nX * CELL_SIZE;

        private static double SquareTop(int p_nY) => (BOARD_SIZE - 1 - p_nY) * CELL_SIZE;

        private void RedrawBoard()
        {
            BoardCanvas.Children.Clear();

            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    DrawSquareBackground(nX, nY);
                }
            }

            if (m_stSelectedSquare is { } stSelected)
            {
                DrawSelectionHighlight(stSelected.X, stSelected.Y);
                foreach ((int X, int Y) in m_lisSelectedPieceDestinations)
                {
                    bool bIsCapture = m_objSession.GetPieceAt(X, Y) is not null;
                    DrawMoveHint(X, Y, bIsCapture);
                }
            }

            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    Ont.Entity? objPiece = m_objSession.GetPieceAt(nX, nY);
                    if (objPiece is not null)
                    {
                        DrawPiece(nX, nY, objPiece);
                    }
                }
            }
        }

        private void DrawSquareBackground(int p_nX, int p_nY)
        {
            bool bIsGoalRow = p_nY == 0 || p_nY == BOARD_SIZE - 1;
            bool bIsLight = (p_nX + p_nY) % 2 == 1;

            Color objBaseColor = bIsLight ? Color.FromRgb(0xDC, 0xE3, 0xEA) : Color.FromRgb(0x7C, 0x8C, 0xA0);
            if (bIsGoalRow)
            {
                // 상대 뒷줄(=내 미션 목표) / 내 뒷줄(=상대 미션 목표)에 살짝 금빛을 얹어 승리 조건과
                // 직결되는 줄임을 눈에 띄게 한다.
                objBaseColor = Color.FromRgb(
                    (byte)Math.Min(255, objBaseColor.R + 30),
                    (byte)Math.Min(255, objBaseColor.G + 20),
                    (byte)Math.Max(0, objBaseColor.B - 30));
            }

            Rectangle objSquare = new Rectangle
            {
                Width = CELL_SIZE,
                Height = CELL_SIZE,
                Fill = new SolidColorBrush(objBaseColor)
            };

            Canvas.SetLeft(objSquare, SquareLeft(p_nX));
            Canvas.SetTop(objSquare, SquareTop(p_nY));
            BoardCanvas.Children.Add(objSquare);
        }

        private void DrawSelectionHighlight(int p_nX, int p_nY)
        {
            Rectangle objHighlight = new Rectangle
            {
                Width = CELL_SIZE,
                Height = CELL_SIZE,
                Fill = new SolidColorBrush(Color.FromArgb(120, 255, 215, 0))
            };

            Canvas.SetLeft(objHighlight, SquareLeft(p_nX));
            Canvas.SetTop(objHighlight, SquareTop(p_nY));
            BoardCanvas.Children.Add(objHighlight);
        }

        private void DrawMoveHint(int p_nX, int p_nY, bool p_bIsCapture)
        {
            double dCenterX = SquareLeft(p_nX) + (CELL_SIZE / 2);
            double dCenterY = SquareTop(p_nY) + (CELL_SIZE / 2);
            double dRadius = p_bIsCapture ? (CELL_SIZE / 2) - 4 : CELL_SIZE / 7;

            Ellipse objHint = new Ellipse
            {
                Width = dRadius * 2,
                Height = dRadius * 2,
                Fill = p_bIsCapture ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(140, 40, 140, 40)),
                Stroke = p_bIsCapture ? new SolidColorBrush(Color.FromArgb(180, 180, 30, 30)) : null,
                StrokeThickness = p_bIsCapture ? 4 : 0
            };

            Canvas.SetLeft(objHint, dCenterX - dRadius);
            Canvas.SetTop(objHint, dCenterY - dRadius);
            BoardCanvas.Children.Add(objHint);
        }

        private void DrawPiece(int p_nX, int p_nY, Ont.Entity p_objPiece)
        {
            bool bIsBlack = p_objPiece.mv_eColor == Ont.E_PlayerColor.Black;
            bool bNumberKnown = p_objPiece.mv_eColor == HUMAN_COLOR || m_objSession.IsRevealed(p_objPiece.mv_strEntityID);
            string strLabel = bNumberKnown ? p_objPiece.mv_strType : "?";

            double dRadius = (CELL_SIZE / 2) - 5;
            double dCenterX = SquareLeft(p_nX) + (CELL_SIZE / 2);
            double dCenterY = SquareTop(p_nY) + (CELL_SIZE / 2);

            Ellipse objDisc = new Ellipse
            {
                Width = dRadius * 2,
                Height = dRadius * 2,
                Fill = bIsBlack ? new SolidColorBrush(Color.FromRgb(0x22, 0x2B, 0x38)) : Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(0x2E, 0x3B, 0x4E)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(objDisc, dCenterX - dRadius);
            Canvas.SetTop(objDisc, dCenterY - dRadius);
            BoardCanvas.Children.Add(objDisc);

            TextBlock objLabel = new TextBlock
            {
                Text = strLabel,
                FontSize = CELL_SIZE * 0.4,
                FontWeight = FontWeights.Bold,
                Foreground = bIsBlack ? Brushes.White : Brushes.Black,
                Width = dRadius * 2,
                Height = dRadius * 2,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(objLabel, dCenterX - dRadius);
            Canvas.SetTop(objLabel, dCenterY - dRadius + (CELL_SIZE * 0.02));
            BoardCanvas.Children.Add(objLabel);
        }

        private const string RULES_TEXT =
@"9x9 반상에서 양쪽 모두 1~9 숫자가 하나씩 적힌 기사 9명을 갖고 시작합니다. 6명은 자기 쪽 배치 줄에 곧바로 나와 있고, 나머지 3명은 예비 상태입니다. 내 기사의 번호는 항상 보이지만, 상대 기사의 번호는 실제로 전투가 벌어지기 전까지 화면에 '?'로 표시됩니다.

이동
기사는 매 턴 인접한 8칸(상하좌우+대각선) 중 한 칸으로 이동합니다. 빈 칸이면 그냥 이동, 상대 기사가 있는 칸이면 전투가 벌어집니다.

전투 (숫자 상성)
· 같은 숫자끼리 부딪히면 공격한 쪽이 이깁니다.
· 숫자 차이가 1이면 더 낮은 숫자가 이깁니다.
· 숫자 차이가 2 이상이면 더 높은 숫자가 이깁니다.
· 예외: 1이 9를 공격하든 9가 1을 공격하든, 항상 1이 이깁니다.
· 히든 토큰: 각 플레이어는 1~5 사이의 비밀 번호를 하나 갖고 시작합니다. 이 번호의 자기 기사로 상대의 8번을 공격하면 원래 상성표와 무관하게 무조건 이깁니다 — 화면 오른쪽의 '내 비밀 정보' 패널에서 확인할 수 있습니다.
전투에서 진 기사는 사라지고, 예비 기사가 있으면 그중 가장 낮은 번호가 자동으로 반상에 보충됩니다. 전투가 벌어지면 이긴 쪽/진 쪽 모두 번호가 서로에게 공개됩니다.

승리 조건
· 임무 승리: 각 플레이어는 비밀 임무 번호(자신의 몇 번 기사가 상대 뒷줄에 닿아야 하는지)를 하나씩 갖습니다. 그 번호의 내 기사가 상대 뒷줄(반상 끝 줄, 화면에서 살짝 금빛으로 표시됨)에 살아서 도착하면 즉시 승리합니다.
· 전멸 승리: 상대 기사 9명을 모두 없애면 승리합니다.

이 구현에서 단순화한 부분
원작은 선공-후공이 번갈아 직접 배치하는 스네이크 드래프트와, 병과(워리어/아처/레인저)별 카드 뽑기로 임무를 정하는 절차가 있습니다. 이 구현은 그 상호작용식 절차 대신 배치와 임무 번호 모두 게임 시작 시 자동으로 무작위 결정합니다 — 이동/전투/승리 조건 같은 핵심 규칙은 그대로입니다.

AI
상대는 반복 탐색(MCTS) 대신 한 수 앞만 내다보는 정직한 기대값 휴리스틱입니다. 아직 공개되지 않은 내 기물을 공격할지 판단할 때, AI는 실제 번호를 훔쳐보지 않고 ""내가 이미 공개한 번호들을 뺀 나머지 숫자 중 하나가 균등하게 있을 것""이라는 정직한 확률 추정만으로 기대 승률을 계산합니다.";
    }
}
