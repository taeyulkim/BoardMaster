using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ont = BoardMaster.Core.Ontology;
using GT = BoardMaster.Core.Rules.GuryongTu;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core의 GuryongTuGameSession을 그대로 감싸는 구룡투(九龍鬪) 클라이언트입니다.
    /// Go(반상)/War(자동 진행 카드)에 이은 세 번째 장르로, 이번엔 "동시 비공개 선택"을 보여줍니다 —
    /// 사용자가 타일을 클릭하면 곧바로 AI(균등 무작위 선택)도 자기 타일을 하나 커밋해서, 마치 두
    /// 선택이 동시에 이뤄진 것처럼 두 카드를 함께 공개합니다. AI는 GoMctsSearcher 같은 탐색이
    /// 아니라 순수 무작위입니다 — 이 게임은 완전한 은닉 정보 동시 게임이라 "최적 전략"이 혼합
    /// 전략(내쉬 균형)이고, 이 프로젝트의 다른 AI들과 마찬가지로 "강한 AI"가 목적이 아니라
    /// 엔진이 이 장르도 감당하는지 보여주는 게 목적이라 균등 무작위로 충분합니다.
    /// </summary>
    public partial class GuryongTuWindow : Window
    {
        private readonly Random m_objAiRandom = new();

        private GT.GuryongTuGameSession m_objSession = null!;
        private int m_nRoundCount;

        public GuryongTuWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void StartNewGame()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            m_objSession = new GT.GuryongTuGameSession(objContext);
            m_nRoundCount = 0;

            LogListBox.Items.Clear();
            RoundResultText.Text = string.Empty;
            BlackCardText.Text = string.Empty;
            WhiteCardText.Text = string.Empty;
            ResetCardBorderHighlight();
            SetStatus("타일을 하나 클릭해서 첫 라운드를 시작하세요.", Brushes.White);

            BuildTileButtons();
            UpdateScoreText();
        }

        private void BuildTileButtons()
        {
            TileButtonPanel.Children.Clear();

            for (int nRank = 1; nRank <= GT.GuryongTuGameFactory.TILE_COUNT; nRank++)
            {
                Button objButton = new Button
                {
                    Content = nRank.ToString(),
                    Margin = new Thickness(2),
                    Tag = nRank
                };
                objButton.Click += TileButton_Click;
                TileButtonPanel.Children.Add(objButton);
            }

            RefreshTileButtonStates();
        }

        private void RefreshTileButtonStates()
        {
            List<int> lisRemaining = m_objSession.GetRemainingTiles(Ont.E_PlayerColor.Black);
            bool bCanPlay = m_objSession.CurrentPhaseName == "MainPlay"
                && !m_objSession.HasCommittedThisRound(Ont.E_PlayerColor.Black);

            foreach (Button objButton in TileButtonPanel.Children.OfType<Button>())
            {
                int nRank = (int)objButton.Tag;
                objButton.IsEnabled = bCanPlay && lisRemaining.Contains(nRank);
            }
        }

        private void TileButton_Click(object sender, RoutedEventArgs e)
        {
            int nRank = (int)((Button)sender).Tag;
            PlayRound(nRank);
        }

        /// <summary>
        /// 사용자가 낸 직후 곧바로 AI의 무작위 선택도 커밋합니다 — AI는 사용자가 이번에 무엇을
        /// 냈는지 전혀 참조하지 않고 순수하게 자기 남은 타일 중 하나를 균등 무작위로 고르므로,
        /// 두 커밋이 코드상 순차적이어도 "블라인드"라는 전제는 깨지지 않습니다.
        /// </summary>
        private void PlayRound(int p_nBlackRank)
        {
            m_objSession.CommitTile(Ont.E_PlayerColor.Black, p_nBlackRank);

            List<int> lisWhiteRemaining = m_objSession.GetRemainingTiles(Ont.E_PlayerColor.White);
            int nWhiteRank = lisWhiteRemaining[m_objAiRandom.Next(lisWhiteRemaining.Count)];
            m_objSession.CommitTile(Ont.E_PlayerColor.White, nWhiteRank);

            m_nRoundCount++;
            RenderLastRound();
            AppendLogLine();
            UpdateScoreText();
            RefreshTileButtonStates();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameOver();
            }
            else
            {
                SetStatus("타일을 하나 클릭해서 다음 라운드를 진행하세요.", Brushes.White);
            }
        }

        private void RenderLastRound()
        {
            GT.GuryongTuRoundResult? objResult = m_objSession.LastRoundResult;
            ResetCardBorderHighlight();

            if (objResult is null)
            {
                return;
            }

            BlackCardText.Text = objResult.BlackRank.ToString();
            WhiteCardText.Text = objResult.WhiteRank.ToString();

            if (objResult.Winner == Ont.E_PlayerColor.Black)
            {
                RoundResultText.Text = "승리!";
                BlackCardBorder.BorderBrush = Brushes.Gold;
                BlackCardBorder.BorderThickness = new Thickness(3);
            }
            else if (objResult.Winner == Ont.E_PlayerColor.White)
            {
                RoundResultText.Text = "패배...";
                WhiteCardBorder.BorderBrush = Brushes.Gold;
                WhiteCardBorder.BorderThickness = new Thickness(3);
            }
            else
            {
                RoundResultText.Text = "무승부";
            }
        }

        private void ResetCardBorderHighlight()
        {
            BlackCardBorder.BorderBrush = Brushes.Black;
            BlackCardBorder.BorderThickness = new Thickness(2);
            WhiteCardBorder.BorderBrush = Brushes.Black;
            WhiteCardBorder.BorderThickness = new Thickness(2);
        }

        private void AppendLogLine()
        {
            GT.GuryongTuRoundResult? objResult = m_objSession.LastRoundResult;
            if (objResult is null)
            {
                return;
            }

            string strOutcome = objResult.Winner switch
            {
                Ont.E_PlayerColor.Black => "나 승",
                Ont.E_PlayerColor.White => "상대 승",
                _ => "무승부"
            };

            string strLine = $"{m_nRoundCount,2}. 나 {objResult.BlackRank} vs 상대 {objResult.WhiteRank} -> {strOutcome}";
            LogListBox.Items.Add(strLine);
            LogListBox.ScrollIntoView(strLine);
        }

        private void UpdateScoreText()
        {
            int nMyWins = m_objSession.CountRoundsWon(Ont.E_PlayerColor.Black);
            int nOpponentWins = m_objSession.CountRoundsWon(Ont.E_PlayerColor.White);
            int nTies = m_nRoundCount - nMyWins - nOpponentWins;

            ScoreText.Text = $"내 승: {nMyWins}   상대 승: {nOpponentWins}   무승부: {nTies}   라운드: {m_nRoundCount}/{GT.GuryongTuGameFactory.TILE_COUNT}";
        }

        private void ShowGameOver()
        {
            int nMyWins = m_objSession.CountRoundsWon(Ont.E_PlayerColor.Black);
            int nOpponentWins = m_objSession.CountRoundsWon(Ont.E_PlayerColor.White);
            string strResult = nMyWins > nOpponentWins ? "당신 승리!" : nOpponentWins > nMyWins ? "상대 승리!" : "무승부!";

            SetStatus($"게임 종료 — {strResult} ({nMyWins} : {nOpponentWins})", Brushes.Gold);
        }

        private void SetStatus(string p_strMessage, Brush p_objColor)
        {
            StatusText.Text = p_strMessage;
            StatusText.Foreground = p_objColor;
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();

        private void ViewRulesButton_Click(object sender, RoutedEventArgs e)
        {
            RulesWindow objRulesWindow = new RulesWindow("구룡투 규칙", RULES_TEXT) { Owner = this };
            objRulesWindow.Show();
        }

        private const string RULES_TEXT =
@"타일: 1부터 9까지 숫자가 하나씩 적힌 타일을 두 사람 모두 각자 9개씩 갖고 시작합니다(같은 숫자 세트를 양쪽이 하나씩 소유).

한 라운드
양쪽이 자기 타일 중 하나를 골라 비공개로 냅니다(블라인드). 둘 다 내면 동시에 공개해 비교합니다 — 큰 숫자를 낸 쪽이 그 라운드를 가져가 1점을 얻습니다. 숫자가 같으면 무승부이며 아무도 점수를 얻지 못합니다.

특수 규칙: 1은 9를 이긴다
가장 작은 숫자인 1이 가장 큰 숫자인 9를 이기는 유일한 예외입니다 — 그 외에는 항상 큰 숫자가 이깁니다.

진행
한 번 낸 타일은 다시 낼 수 없으므로, 9라운드 동안 양쪽 모두 1~9를 정확히 한 번씩 내게 됩니다.

최종 승리
9라운드가 끝난 뒤 더 많이 이긴 쪽이 승리합니다. 이긴 라운드 수가 같으면 이 구현에서는 무승부로 처리합니다.

AI
상대는 정교한 탐색이 아니라 남은 타일 중 하나를 균등 무작위로 고릅니다 — 이 게임은 완전한 은닉 정보 동시 게임이라 ""최적 전략""이 애초에 특정 수가 아니라 확률적으로 섞어 내는 것(혼합 전략)이기 때문입니다.";
    }
}
