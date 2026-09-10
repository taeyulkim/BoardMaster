using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Ont = BoardMaster.Core.Ontology;
using WarRules = BoardMaster.Core.Rules.War;

namespace BoardMaster.Wpf
{
    /// <summary>
    /// BoardMaster.Core의 WarGameSession을 그대로 감싸는 War(전쟁 카드 게임) 클라이언트입니다.
    /// War는 플레이어가 선택할 게 없는 게임이라(WarGameSession 문서 참고) MainWindow처럼 클릭으로
    /// 착수하는 상호작용이 아니라, "한 라운드"나 "자동 재생"으로 진행 자체를 구경하는 형태입니다.
    /// WarGameSession.LastRoundResult(이 세션을 위해 새로 추가한 프로퍼티)가 없으면 방금 라운드에서
    /// 어떤 카드가 공개됐는지 알 방법이 없었습니다 — Table Zone은 라운드가 끝나자마자 비워지기
    /// 때문입니다. 이 창은 순수한 시각화 계층입니다 — 게임 규칙은 전혀 재구현하지 않았습니다.
    /// </summary>
    public partial class WarWindow : Window
    {
        private const int AUTO_PLAY_INTERVAL_MS = 500;

        private readonly DispatcherTimer m_objAutoPlayTimer;
        private readonly List<string> m_lisLog = new();

        private WarRules.WarGameSession m_objSession = null!;
        private bool m_bAutoPlaying;
        private int m_nRoundCount;

        public WarWindow()
        {
            InitializeComponent();

            m_objAutoPlayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AUTO_PLAY_INTERVAL_MS) };
            m_objAutoPlayTimer.Tick += AutoPlayTimer_Tick;

            StartNewGame();
        }

        private void StartNewGame()
        {
            StopAutoPlay();

            Random objRandom = new Random();
            Ont.GameContext objContext = WarRules.WarGameFactory.CreateStandardGame(objRandom);
            m_objSession = new WarRules.WarGameSession(objContext, objRandom);
            m_nRoundCount = 0;

            m_lisLog.Clear();
            LogListBox.Items.Clear();
            RoundResultText.Text = string.Empty;
            BlackCardText.Text = string.Empty;
            WhiteCardText.Text = string.Empty;
            ResetCardBorderHighlight();
            PlayRoundButton.IsEnabled = true;
            AutoPlayButton.IsEnabled = true;
            SetStatus("\"한 라운드\" 또는 \"자동 재생 시작\"으로 진행하세요.", Brushes.White);

            UpdateDeckCounts();
        }

        private void PlayRoundButton_Click(object sender, RoutedEventArgs e) => PlayOneRound();

        private void AutoPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_bAutoPlaying)
            {
                StopAutoPlay();
            }
            else
            {
                StartAutoPlay();
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();

        private void StartAutoPlay()
        {
            if (m_objSession.CurrentPhaseName != "MainPlay")
            {
                return;
            }

            m_bAutoPlaying = true;
            AutoPlayButton.Content = "자동 재생 정지";
            PlayRoundButton.IsEnabled = false;
            m_objAutoPlayTimer.Start();
        }

        private void StopAutoPlay()
        {
            m_objAutoPlayTimer.Stop();
            m_bAutoPlaying = false;
            AutoPlayButton.Content = "자동 재생 시작";
            PlayRoundButton.IsEnabled = m_objSession is null || m_objSession.CurrentPhaseName == "MainPlay";
        }

        private void AutoPlayTimer_Tick(object? sender, EventArgs e)
        {
            PlayOneRound();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                StopAutoPlay();
            }
        }

        private void PlayOneRound()
        {
            if (m_objSession.CurrentPhaseName != "MainPlay")
            {
                return;
            }

            m_objSession.PlayRound();
            m_nRoundCount++;

            RenderLastRound();
            UpdateDeckCounts();
            AppendLogLine();

            if (m_objSession.CurrentPhaseName == "GameOver")
            {
                ShowGameOver();
            }
        }

        private void RenderLastRound()
        {
            WarRules.WarRoundResult? objResult = m_objSession.LastRoundResult;
            ResetCardBorderHighlight();

            if (objResult is null)
            {
                return;
            }

            BlackCardText.Text = objResult.BlackCardRank is null ? "-" : RankToLabel(objResult.BlackCardRank);
            WhiteCardText.Text = objResult.WhiteCardRank is null ? "-" : RankToLabel(objResult.WhiteCardRank);

            if (objResult.BlackCardRank is null || objResult.WhiteCardRank is null)
            {
                RoundResultText.Text = "한쪽이 낼 카드가 없습니다.";
            }
            else if (objResult.WasWar)
            {
                RoundResultText.Text = "전쟁! 무승부 — 카드가 WarPool에 쌓입니다.";
            }
            else if (objResult.Winner == Ont.E_PlayerColor.Black)
            {
                RoundResultText.Text = "흑 승리!";
                BlackCardBorder.BorderBrush = Brushes.Gold;
                BlackCardBorder.BorderThickness = new Thickness(3);
            }
            else if (objResult.Winner == Ont.E_PlayerColor.White)
            {
                RoundResultText.Text = "백 승리!";
                WhiteCardBorder.BorderBrush = Brushes.Gold;
                WhiteCardBorder.BorderThickness = new Thickness(3);
            }
        }

        private void ResetCardBorderHighlight()
        {
            BlackCardBorder.BorderBrush = Brushes.Black;
            BlackCardBorder.BorderThickness = new Thickness(2);
            WhiteCardBorder.BorderBrush = Brushes.Black;
            WhiteCardBorder.BorderThickness = new Thickness(2);
        }

        private void UpdateDeckCounts()
        {
            BlackDeckCountText.Text = $"{m_objSession.CountCardsOwnedBy(Ont.E_PlayerColor.Black)}장";
            WhiteDeckCountText.Text = $"{m_objSession.CountCardsOwnedBy(Ont.E_PlayerColor.White)}장";
        }

        private void AppendLogLine()
        {
            WarRules.WarRoundResult? objResult = m_objSession.LastRoundResult;
            if (objResult is null)
            {
                return;
            }

            string strBlack = objResult.BlackCardRank is null ? "-" : RankToLabel(objResult.BlackCardRank);
            string strWhite = objResult.WhiteCardRank is null ? "-" : RankToLabel(objResult.WhiteCardRank);
            string strOutcome = objResult.WasWar
                ? "전쟁!"
                : objResult.Winner switch
                {
                    Ont.E_PlayerColor.Black => "흑 승",
                    Ont.E_PlayerColor.White => "백 승",
                    _ => "-"
                };

            string strLine = $"{m_nRoundCount,4}. 흑 {strBlack,-2} vs 백 {strWhite,-2} → {strOutcome}";
            m_lisLog.Add(strLine);
            LogListBox.Items.Add(strLine);
            LogListBox.ScrollIntoView(strLine);
        }

        private void ShowGameOver()
        {
            int nBlackCount = m_objSession.CountCardsOwnedBy(Ont.E_PlayerColor.Black);
            int nWhiteCount = m_objSession.CountCardsOwnedBy(Ont.E_PlayerColor.White);
            string strWinner = nBlackCount > nWhiteCount ? "흑" : "백";

            SetStatus($"게임 종료 — {strWinner} 승리! (총 {m_nRoundCount}라운드)", Brushes.Gold);
            PlayRoundButton.IsEnabled = false;
            AutoPlayButton.IsEnabled = false;
        }

        private void SetStatus(string p_strMessage, Brush p_objColor)
        {
            StatusText.Text = p_strMessage;
            StatusText.Foreground = p_objColor;
        }

        /// <summary>랭크 문자열("2".."14")을 표준 카드 표기(J/Q/K/A 또는 숫자)로 바꿉니다.</summary>
        private static string RankToLabel(string p_strRank)
        {
            return p_strRank switch
            {
                "11" => "J",
                "12" => "Q",
                "13" => "K",
                "14" => "A",
                _ => p_strRank
            };
        }
    }
}
