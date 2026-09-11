using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ont = BoardMaster.Core.Ontology;
using OntDyn = BoardMaster.Core.Ontology.Dynamic;
using Chess = BoardMaster.Core.Rules.Chess;
using AiChess = BoardMaster.Core.AI.Chess;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core의 ChessGameSession을 그대로 감싸는 체스 클라이언트입니다. Go/War/GuryongTu에
    /// 이은 네 번째 장르로, 이번엔 "기물마다 완전히 다른 이동 규칙"을 보여줍니다. 화면 갱신과
    /// 클릭 처리 외에는 규칙을 전혀 재구현하지 않았습니다 — 합법수 판정, 체크/체크메이트/스테일메이트,
    /// 캐슬링, 앙파상, 승진은 전부 ChessGameSession/ChessMoveGenerator의 몫입니다.
    ///
    /// AI(Black)는 GoMctsSearcher와 같은 구조의 ChessMctsSearcher(정책/가치망 없는 순수 UCT,
    /// 균등 무작위 롤아웃)를 씁니다. Go와 달리 체크메이트/스테일메이트가 아니면 롤아웃이 자연히
    /// 안 끝날 수 있어서(쓰리폴드/50수 규칙 없음), 한도에 걸리면 간단한 기물 점수 우세로 대신
    /// 추정합니다(ChessMctsSearcher 문서 참고). 난이도별 반복 횟수는 실제 응답 시간을 재서 골랐습니다
    /// (Release 빌드 기준 300회 ~1초 / 800회 ~3초 / 2000회 ~8초).
    /// </summary>
    public partial class ChessWindow : Window
    {
        private const double CELL_SIZE = 58;
        private const int BOARD_SIZE = 8;
        private const int DEFAULT_AI_ITERATIONS = 300; // 초급 — DifficultyComboBox의 SelectedIndex="0"과 짝을 맞춘 값.

        private readonly AiChess.ChessMctsSearcher m_objAiSearcher = new();
        private readonly Random m_objAiRandom = new();

        private Chess.ChessGameSession m_objSession = null!;
        private (int X, int Y)? m_stSelectedSquare;
        private List<Chess.ChessMove> m_lisSelectedPieceMoves = new();
        private bool m_bAiThinking;
        private int m_nAiSearchIterations = DEFAULT_AI_ITERATIONS;

        public ChessWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void StartNewGame()
        {
            m_objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());
            m_stSelectedSquare = null;
            m_lisSelectedPieceMoves = new List<Chess.ChessMove>();
            m_bAiThinking = false;

            ResultText.Text = string.Empty;
            CandidateListBox.ItemsSource = null;
            SetStatus("White(사용자) 차례입니다. 기물을 클릭해 선택하세요.", Brushes.DarkGreen);

            RedrawBoard();
            UpdateStatusPanel();
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

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void ViewRulesButton_Click(object sender, RoutedEventArgs e)
        {
            RulesWindow objRulesWindow = new RulesWindow("체스 규칙", RULES_TEXT) { Owner = this };
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

            if (m_stSelectedSquare is { } stSelected)
            {
                Chess.ChessMove? stChosenMove = m_lisSelectedPieceMoves.Find(m => m.ToX == stClicked.X && m.ToY == stClicked.Y) is { } stFound
                    ? stFound
                    : null;

                if (stChosenMove is not null)
                {
                    TryMakeHumanMove(stSelected.X, stSelected.Y, stClicked.X, stClicked.Y);
                    return;
                }
            }

            // 선택 안 된 상태에서 클릭했거나, 합법 목적지가 아닌 다른 칸을 클릭했다 — 그 칸에 내
            // (White) 기물이 있으면 새로 선택하고, 아니면 선택을 해제한다.
            Ont.Entity? objPiece = m_objSession.GetPieceAt(stClicked.X, stClicked.Y);
            if (objPiece is not null && objPiece.mv_eColor == Ont.E_PlayerColor.White)
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
            m_lisSelectedPieceMoves = m_objSession.GetLegalMoves(p_nX, p_nY);
        }

        private void ClearSelection()
        {
            m_stSelectedSquare = null;
            m_lisSelectedPieceMoves = new List<Chess.ChessMove>();
        }

        private async void TryMakeHumanMove(int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            try
            {
                m_objSession.MovePiece(p_nFromX, p_nFromY, p_nToX, p_nToY);
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
            DifficultyComboBox.IsEnabled = false;
            SetStatus($"AI(Black)가 생각 중입니다... (탐색 {m_nAiSearchIterations}회)", Brushes.DarkOrange);

            int nIterationsForThisMove = m_nAiSearchIterations;
            AiChess.ChessMctsSearchResult objSearchResult = await Task.Run(
                () => m_objAiSearcher.Search(m_objSession, nIterationsForThisMove, m_objAiRandom));

            if (objSearchResult.BestMove is { } stMove)
            {
                m_objSession.MovePiece(stMove.FromX, stMove.FromY, stMove.ToX, stMove.ToY);
            }

            PopulateCandidatePanel(objSearchResult);

            m_bAiThinking = false;
            NewGameButton.IsEnabled = true;
            DifficultyComboBox.IsEnabled = true;

            RedrawBoard();
            UpdateStatusPanel();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameResult();
            }
            else
            {
                SetStatus("White(사용자) 차례입니다. 기물을 클릭해 선택하세요.", Brushes.DarkGreen);
            }
        }

        /// <summary>
        /// 직전 AI 탐색에서 루트가 실제로 펼쳐본 후보 수들을 방문 횟수 순으로 보여줍니다(이미
        /// ChessMctsSearcher.Search가 방문 횟수 내림차순으로 정렬해서 반환합니다).
        /// </summary>
        private void PopulateCandidatePanel(AiChess.ChessMctsSearchResult p_objResult)
        {
            const int MAX_DISPLAYED = 10;

            List<string> lisLines = new();
            int nCount = Math.Min(p_objResult.CandidateMoves.Count, MAX_DISPLAYED);

            for (int i = 0; i < nCount; i++)
            {
                AiChess.ChessMctsCandidateStat objCandidate = p_objResult.CandidateMoves[i];
                bool bIsChosen = p_objResult.BestMove.HasValue && objCandidate.Move == p_objResult.BestMove.Value;
                string strMoveLabel = $"{ToAlgebraic(objCandidate.Move.FromX, objCandidate.Move.FromY)}-{ToAlgebraic(objCandidate.Move.ToX, objCandidate.Move.ToY)}";
                string strMarker = bIsChosen ? "▶" : " ";

                lisLines.Add($"{strMarker} {strMoveLabel,-6} 방문 {objCandidate.VisitCount,4}회  승률 {objCandidate.WinRate * 100,5:0.0}%");
            }

            CandidateListBox.ItemsSource = lisLines;
        }

        private static string ToAlgebraic(int p_nX, int p_nY)
        {
            return $"{(char)('a' + p_nX)}{p_nY + 1}";
        }

        private void UpdateStatusPanel()
        {
            Ont.E_PlayerColor eActiveColor = m_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            string strTurnLabel = eActiveColor == Ont.E_PlayerColor.White ? "White(사용자)" : "Black(AI)";
            TurnText.Text = $"현재 차례: {strTurnLabel} (턴 {m_objSession.mv_objCurrentContext.mv_stCurrentState.m_nTurnNumber})";

            CheckText.Text = m_objSession.IsInCheck(eActiveColor) ? $"{strTurnLabel} 체크!" : string.Empty;
        }

        private void ShowGameResult()
        {
            Ont.PlayerState objWhite = m_objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;
            Ont.PlayerState objBlack = m_objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;

            string strResult = objWhite.mv_nScore > objBlack.mv_nScore
                ? "체크메이트 — White(사용자) 승리!"
                : objBlack.mv_nScore > objWhite.mv_nScore
                    ? "체크메이트 — Black(AI) 승리!"
                    : "스테일메이트 — 무승부!";

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
                foreach (Chess.ChessMove stMove in m_lisSelectedPieceMoves)
                {
                    DrawMoveHint(stMove.ToX, stMove.ToY, stMove.IsCapture || stMove.IsEnPassantCapture);
                }
            }

            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    Ont.Entity? objPiece = m_objSession.GetPieceAt(nX, nY);
                    if (objPiece is not null)
                    {
                        DrawPiece(nX, nY, objPiece.mv_eColor, objPiece.mv_strType);
                    }
                }
            }
        }

        private void DrawSquareBackground(int p_nX, int p_nY)
        {
            bool bIsLight = (p_nX + p_nY) % 2 == 1;
            Rectangle objSquare = new Rectangle
            {
                Width = CELL_SIZE,
                Height = CELL_SIZE,
                Fill = bIsLight ? new SolidColorBrush(Color.FromRgb(0xF0, 0xD9, 0xB5)) : new SolidColorBrush(Color.FromRgb(0xB5, 0x88, 0x63))
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

        private void DrawPiece(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor, string p_strPieceType)
        {
            TextBlock objGlyph = new TextBlock
            {
                Text = GetGlyph(p_eColor, p_strPieceType),
                FontSize = CELL_SIZE * 0.72,
                Foreground = p_eColor == Ont.E_PlayerColor.White ? Brushes.White : Brushes.Black,
                Width = CELL_SIZE,
                Height = CELL_SIZE,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // White 기물은 채움만으로는 밝은 칸 위에서 잘 안 보이므로 검은 테두리를 덧대 대비를 준다.
            if (p_eColor == Ont.E_PlayerColor.White)
            {
                objGlyph.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 0,
                    ShadowDepth = 1,
                    Opacity = 0.9
                };
            }

            Canvas.SetLeft(objGlyph, SquareLeft(p_nX));
            Canvas.SetTop(objGlyph, SquareTop(p_nY) - (CELL_SIZE * 0.06));
            BoardCanvas.Children.Add(objGlyph);
        }

        private static string GetGlyph(Ont.E_PlayerColor p_eColor, string p_strPieceType)
        {
            bool bWhite = p_eColor == Ont.E_PlayerColor.White;
            return p_strPieceType switch
            {
                Chess.ChessPieceType.King => bWhite ? "♔" : "♚",
                Chess.ChessPieceType.Queen => bWhite ? "♕" : "♛",
                Chess.ChessPieceType.Rook => bWhite ? "♖" : "♜",
                Chess.ChessPieceType.Bishop => bWhite ? "♗" : "♝",
                Chess.ChessPieceType.Knight => bWhite ? "♘" : "♞",
                Chess.ChessPieceType.Pawn => bWhite ? "♙" : "♟",
                _ => "?"
            };
        }

        private const string RULES_TEXT =
@"이 클라이언트는 표준 체스 규칙을 그대로 구현합니다 — 킹/퀸/룩/비숍/나이트/폰의 이동 규칙, 캐슬링, 앙파상, 체크/체크메이트/스테일메이트까지 전부 포함합니다.

기물 이동
· 폰: 앞으로 한 칸(시작 위치면 두 칸), 대각선 앞의 적만 포획. 마지막 랭크에 도달하면 자동으로 퀸으로 승진합니다.
· 나이트: L자로 이동하며 다른 기물을 뛰어넘습니다.
· 비숍/룩/퀸: 각각 대각선/직선/양쪽 방향으로 막힐 때까지 미끄러지듯 이동합니다.
· 킹: 인접한 한 칸, 또는 조건이 맞으면 캐슬링(King과 Rook이 한 번도 움직이지 않았고, 그 사이가 비어 있고, King이 체크 상태가 아니며 지나가는 칸도 공격받지 않을 때).

특수 규칙
· 앙파상: 상대 폰이 시작 위치에서 두 칸 전진해 내 폰 옆을 지나쳤다면, 바로 다음 수에 한해 그 폰을 대각선으로 잡을 수 있습니다.
· 자기 King이 체크에 노출되는 수는 애초에 둘 수 없습니다(핀에 걸린 기물이 대표적인 예).

종국
· 체크메이트: 체크 상태에서 벗어날 수 있는 수가 하나도 없으면 그 자리에서 패배합니다.
· 스테일메이트: 체크는 아니지만 둘 수 있는 합법수가 하나도 없으면 무승부입니다.
· 이 구현은 체크메이트/스테일메이트만 종국 조건으로 봅니다 — 쓰리폴드 반복이나 50수 규칙 같은 무승부 조건은 생략했습니다.

AI
상대(Black)는 정책/가치망 없는 순수 MCTS(UCT, 균등 무작위 롤아웃)입니다. 체크메이트/체크가 아니면 게임이 안 끝나서 롤아웃이 도중에 끊길 수 있는데, 이때는 간단한 기물 점수 우세(폰1/나이트3/비숍3/룩5/퀸9)로 대신 판단합니다. 난이도는 탐색 반복 횟수만 다릅니다.";
    }
}
