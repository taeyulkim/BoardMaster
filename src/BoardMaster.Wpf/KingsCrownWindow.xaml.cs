using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ont = BoardMaster.Core.Ontology;
using OntDyn = BoardMaster.Core.Ontology.Dynamic;
using KC = BoardMaster.Core.Rules.KingsCrown;
using AiKc = BoardMaster.Core.AI.KingsCrown;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core의 KingsCrownGameSession을 그대로 감싸는 킹스 크라운(이세돌, WIZSTONE)
    /// 클라이언트입니다. 이 장르는 반상에 놓인 왕관은 항상 완전히 공개 정보입니다(Nine Knights와
    /// 달리 놓인 뒤에는 번호를 숨길 이유가 없습니다) — 숨겨야 하는 것은 "아직 놓지 않고 보유 중인
    /// 숫자칩"뿐이라, 화면에는 내(Black) 보유 숫자칩만 버튼으로 보여주고 상대(White)의 보유
    /// 숫자칩은 개수만(정확한 값은 절대) 표시합니다.
    ///
    /// 원작 규칙서의 '가져오기'(숫자칩을 공개→획득→비공개 반복하며 모으는, 사실상 기억력
    /// 미니게임) 단계는 KingsCrownGameFactory 문서에 적은 이유로 생략하고, 게임 시작 시 양쪽에
    /// 12개씩 자동으로 무작위 배분합니다 — 그래서 이 창은 '놓기' 단계만 다룹니다.
    /// </summary>
    public partial class KingsCrownWindow : Window
    {
        private const double CELL_SIZE = 98;
        private const int BOARD_SIZE = 5;

        private const Ont.E_PlayerColor HUMAN_COLOR = Ont.E_PlayerColor.Black;
        private const Ont.E_PlayerColor AI_COLOR = Ont.E_PlayerColor.White;

        private readonly AiKc.KingsCrownHeuristicAi m_objAi = new();
        private readonly Random m_objAiRandom = new();

        private KC.KingsCrownGameSession m_objSession = null!;
        private int? m_nSelectedChipValue;
        private List<(int ChipValue, int X, int Y)> m_lisSelectedChipDestinations = new();
        private bool m_bAiThinking;

        public KingsCrownWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void StartNewGame()
        {
            m_objSession = new KC.KingsCrownGameSession(KC.KingsCrownGameFactory.CreateStandardGame());
            m_nSelectedChipValue = null;
            m_lisSelectedChipDestinations = new List<(int ChipValue, int X, int Y)>();
            m_bAiThinking = false;

            ResultText.Text = string.Empty;
            SetStatus("숫자칩을 하나 골라 놓을 칸을 선택하세요.", Brushes.White);

            RedrawBoard();
            RedrawChipPanel();
            UpdateStatusPanel();
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void ViewRulesButton_Click(object sender, RoutedEventArgs e)
        {
            RulesWindow objRulesWindow = new RulesWindow("킹스 크라운 규칙", RULES_TEXT) { Owner = this };
            objRulesWindow.Show();
        }

        private void ChipButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_bAiThinking || m_objSession.CurrentPhaseName != "MainPlay")
            {
                return;
            }

            int nValue = (int)((Button)sender).Tag;
            SelectChip(nValue);
            RedrawBoard();
            RedrawChipPanel();
        }

        private void SelectChip(int p_nValue)
        {
            m_nSelectedChipValue = p_nValue;
            List<(int ChipValue, int X, int Y)> lisAllMoves = m_objSession.GetLegalPlacements(HUMAN_COLOR);
            m_lisSelectedChipDestinations = lisAllMoves.FindAll(m => m.ChipValue == p_nValue);
        }

        private void ClearSelection()
        {
            m_nSelectedChipValue = null;
            m_lisSelectedChipDestinations = new List<(int ChipValue, int X, int Y)>();
        }

        private void BoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (m_bAiThinking || m_objSession.CurrentPhaseName != "MainPlay" || m_nSelectedChipValue is not { } nSelectedValue)
            {
                return;
            }

            Point objClickPoint = e.GetPosition(BoardCanvas);
            (int X, int Y) stClicked = ScreenToSquare(objClickPoint);
            if (!InBounds(stClicked.X, stClicked.Y))
            {
                return;
            }

            if (!m_lisSelectedChipDestinations.Exists(m => m.X == stClicked.X && m.Y == stClicked.Y))
            {
                return;
            }

            TryMakeHumanMove(nSelectedValue, stClicked.X, stClicked.Y);
        }

        private async void TryMakeHumanMove(int p_nChipValue, int p_nX, int p_nY)
        {
            try
            {
                m_objSession.PlaceCrown(p_nChipValue, p_nX, p_nY);
            }
            catch (OntDyn.RuleViolationException ex)
            {
                SetStatus($"불법적인 수입니다: {ex.Message}", Brushes.OrangeRed);
                ClearSelection();
                RedrawBoard();
                RedrawChipPanel();
                return;
            }

            ClearSelection();
            RedrawBoard();
            RedrawChipPanel();
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
            SetStatus("AI(White)가 생각 중입니다...", Brushes.Orange);

            (int ChipValue, int X, int Y)? stMove = await Task.Run(
                () => m_objAi.ChooseMove(m_objSession, AI_COLOR, m_objAiRandom));

            if (stMove is { } stChosenMove)
            {
                m_objSession.PlaceCrown(stChosenMove.ChipValue, stChosenMove.X, stChosenMove.Y);
            }

            m_bAiThinking = false;
            NewGameButton.IsEnabled = true;

            RedrawBoard();
            RedrawChipPanel();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult();
            }
            else
            {
                SetStatus("숫자칩을 하나 골라 놓을 칸을 선택하세요.", Brushes.White);
            }
        }

        private void UpdateStatusPanel()
        {
            Ont.E_PlayerColor eActiveColor = m_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            string strTurnLabel = eActiveColor == HUMAN_COLOR ? "Black(사용자)" : "White(AI)";
            TurnText.Text = $"현재 차례: {strTurnLabel} (턴 {m_objSession.mv_objCurrentContext.mv_stCurrentState.m_nTurnNumber})";
        }

        private void ShowGameResult()
        {
            Ont.PlayerState objBlack = m_objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Ont.PlayerState objWhite = m_objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;

            string strResult = objBlack.mv_nScore > objWhite.mv_nScore ? "Black(사용자) 승리!" : "White(AI) 승리!";

            ResultText.Text = $"대국 종료 — {strResult}";
            SetStatus("새 게임을 시작하려면 '새 게임' 버튼을 누르세요.", Brushes.LightGray);
        }

        private void SetStatus(string p_strMessage, Brush p_objColor)
        {
            StatusText.Text = p_strMessage;
            StatusText.Foreground = p_objColor;
        }

        private void RedrawChipPanel()
        {
            ChipPanel.Children.Clear();

            IReadOnlyList<int> lisHeldChips = m_objSession.GetHeldChips(HUMAN_COLOR);
            List<int> lisSorted = new List<int>(lisHeldChips);
            lisSorted.Sort();

            foreach (int nValue in lisSorted)
            {
                bool bSelected = m_nSelectedChipValue == nValue;
                Button objButton = new Button
                {
                    Content = nValue.ToString(),
                    Tag = nValue,
                    Width = 36,
                    Height = 36,
                    Margin = new Thickness(3),
                    FontWeight = FontWeights.Bold,
                    Background = bSelected ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F)) : new SolidColorBrush(Color.FromRgb(0x2A, 0x2C, 0x3D)),
                    Foreground = bSelected ? Brushes.Black : Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x5F, 0x7D)),
                    IsEnabled = !m_bAiThinking && m_objSession.CurrentPhaseName == "MainPlay"
                };
                objButton.Click += ChipButton_Click;
                ChipPanel.Children.Add(objButton);
            }

            int nOpponentRemaining = m_objSession.GetHeldChips(AI_COLOR).Count;
            TextBlock objOpponentInfo = new TextBlock
            {
                Text = $"상대 남은 숫자칩: {nOpponentRemaining}개 (숫자는 비공개)",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(4, 8, 0, 0),
                Width = 230
            };
            ChipPanel.Children.Add(objOpponentInfo);
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

            foreach ((int ChipValue, int X, int Y) in m_lisSelectedChipDestinations)
            {
                DrawDestinationHint(X, Y);
            }

            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    Ont.Entity? objPiece = m_objSession.GetPieceAt(nX, nY);
                    if (objPiece is not null)
                    {
                        DrawCrown(nX, nY, objPiece.mv_eColor, objPiece.mv_strType);
                    }
                }
            }
        }

        private void DrawSquareBackground(int p_nX, int p_nY)
        {
            bool bIsCenter = p_nX == BOARD_SIZE / 2 && p_nY == BOARD_SIZE / 2;
            Color objColor = bIsCenter
                ? Color.FromRgb(0x3D, 0x33, 0x1A)
                : ((p_nX + p_nY) % 2 == 0 ? Color.FromRgb(0x23, 0x25, 0x34) : Color.FromRgb(0x1A, 0x1C, 0x28));

            Rectangle objSquare = new Rectangle
            {
                Width = CELL_SIZE,
                Height = CELL_SIZE,
                Fill = new SolidColorBrush(objColor),
                Stroke = new SolidColorBrush(Color.FromRgb(0x3A, 0x3F, 0x55)),
                StrokeThickness = 0.5
            };
            Canvas.SetLeft(objSquare, SquareLeft(p_nX));
            Canvas.SetTop(objSquare, SquareTop(p_nY));
            BoardCanvas.Children.Add(objSquare);

            if (bIsCenter)
            {
                TextBlock objMark = new TextBlock
                {
                    Text = "★",
                    FontSize = CELL_SIZE * 0.35,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F)),
                    Width = CELL_SIZE,
                    Height = CELL_SIZE,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(objMark, SquareLeft(p_nX));
                Canvas.SetTop(objMark, SquareTop(p_nY));
                BoardCanvas.Children.Add(objMark);
            }
        }

        private void DrawDestinationHint(int p_nX, int p_nY)
        {
            double dCenterX = SquareLeft(p_nX) + (CELL_SIZE / 2);
            double dCenterY = SquareTop(p_nY) + (CELL_SIZE / 2);
            double dRadius = CELL_SIZE / 6;

            Ellipse objHint = new Ellipse
            {
                Width = dRadius * 2,
                Height = dRadius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(160, 255, 213, 79))
            };
            Canvas.SetLeft(objHint, dCenterX - dRadius);
            Canvas.SetTop(objHint, dCenterY - dRadius);
            BoardCanvas.Children.Add(objHint);
        }

        private void DrawCrown(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor, string p_strNumber)
        {
            bool bIsBlack = p_eColor == Ont.E_PlayerColor.Black;
            double dRadius = (CELL_SIZE / 2) - 8;
            double dCenterX = SquareLeft(p_nX) + (CELL_SIZE / 2);
            double dCenterY = SquareTop(p_nY) + (CELL_SIZE / 2);

            Ellipse objDisc = new Ellipse
            {
                Width = dRadius * 2,
                Height = dRadius * 2,
                Fill = bIsBlack ? new SolidColorBrush(Color.FromRgb(0x1C, 0x1E, 0x2A)) : new SolidColorBrush(Color.FromRgb(0xE8, 0xC7, 0x6A)),
                Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(objDisc, dCenterX - dRadius);
            Canvas.SetTop(objDisc, dCenterY - dRadius);
            BoardCanvas.Children.Add(objDisc);

            TextBlock objLabel = new TextBlock
            {
                Text = p_strNumber,
                FontSize = CELL_SIZE * 0.32,
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
@"5x5 반상에서 양쪽 모두 왕관 12개(1~24 중 무작위로 배분된 숫자칩과 결합)를 갖고 시작합니다. 가운데 칸(★)에는 누구도 왕관을 놓을 수 없지만, 이미 양쪽 모두의 왕관이 놓인 것으로 간주되는 와일드카드입니다 — 그 칸을 지나는 줄(가로 1, 세로 1, 대각선 2)은 4칸만 채우면 빙고입니다.

놓는 규칙
· 아무 왕관도 상하좌우로 인접하지 않은 칸에는 아무 숫자든 자유롭게 놓을 수 있습니다.
· 다른 왕관과 상하좌우로 인접한 칸에는, 그 인접한 왕관 중 하나라도 다음 조건을 만족해야 놓을 수 있습니다 — 같은 색이면서 연속된 숫자, 또는 다른 색이면서 같은 숫자. 여러 왕관과 인접해 있다면 그중 하나만 만족해도 됩니다. (대각선은 인접으로 치지 않습니다.)

승리 조건
· 빙고: 자기 왕관 5개가 가로/세로/대각선으로 한 줄에 놓이면(또는 가운데 칸을 포함해 4개가 한 줄에 놓이면) 즉시 승리합니다.
· 상대가 더 이상 놓을 수 없을 때: 보유한 숫자칩 중 무엇으로도, 반상의 어느 빈 칸에도 놓을 수 없는 상황이 오면 그 즉시 게임이 끝나고 마지막으로 왕관을 놓은 플레이어가 승리합니다.

이 구현에서 단순화한 부분
원작은 시작할 때 주머니에서 숫자칩을 2개씩 공개하고 그중 하나를 가져가기를 반복하며, 가져간 뒤 공개된 칩은 다시 뒤집어 두어 ""어느 자리에 어떤 숫자가 뒤집혀 있는지"" 서로 기억해야 하는 기억력 단계가 있습니다. 이 구현은 그 단계를 생략하고 게임 시작 시 양쪽에 숫자칩 12개씩을 자동으로 무작위 배분합니다 — ""상대가 어떤 숫자를 쥐고 있는지 모른다""는 핵심 은닉 정보는 그대로 유지되며, 이 게임의 핵심 전략인 인접 규칙과 빙고 완성은 전부 그대로 구현했습니다.

AI
상대는 반복 탐색이 아니라 한 수 앞만 내다보는 정직한 휴리스틱입니다. 상대가 어떤 숫자칩을 쥐고 있는지는 절대 들여다보지 않고, 반상에 이미 공개된 왕관 배치만으로 ""내 줄을 얼마나 키우는지""와 ""상대의 줄을 얼마나 막는지""를 계산해 수를 고릅니다.";
    }
}
