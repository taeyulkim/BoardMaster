namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 두 가지 승리 조건을 확인합니다: (1) 방금 놓은 왕관으로 빙고가 완성됐다 — 즉시 승리.
    /// (2) 상대가 다음 차례에 자신이 보유한 숫자칩 중 어느 것으로도 어느 빈 칸에도 놓을 수 없다
    /// (규칙서: "더 이상 왕관을 놓을 수 없는 상황이 오면 마지막으로 왕관을 놓은 플레이어가
    /// 승리") — 즉시 승리. 상대가 보유한 숫자칩 목록은 GameContext에 없는 세션 전용 비밀
    /// 상태라 KingsCrownGameSession이 착수 시점의 스냅샷을 생성자로 주입합니다(상대의 목록
    /// 자체는 이번 착수로 바뀌지 않으므로 스냅샷이어도 정확합니다) — Nine Knights의 비밀 임무
    /// 번호 주입과 같은 위치입니다.
    /// </summary>
    public sealed class Effect_CheckBingoAndStalemateWin : Ont.IEffect
    {
        private readonly IReadOnlyList<int> m_lisOpponentHeldChips;

        public Effect_CheckBingoAndStalemateWin(IReadOnlyList<int> p_lisOpponentHeldChips)
        {
            m_lisOpponentHeldChips = p_lisOpponentHeldChips;
        }

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            Ont.E_PlayerColor ePlacerColor = p_objAction.mv_stActionData.m_eColor;

            if (KingsCrownWinRules.HasBingo(p_objContext, ePlacerColor))
            {
                DeclareWinner(p_objContext, ePlacerColor);
                return p_objContext;
            }

            Ont.E_PlayerColor eOpponentColor = KingsCrownBoardGeometry.Opponent(ePlacerColor);
            if (!HasAnyLegalPlacement(p_objContext, eOpponentColor, m_lisOpponentHeldChips))
            {
                DeclareWinner(p_objContext, ePlacerColor);
            }

            return p_objContext;
        }

        private static bool HasAnyLegalPlacement(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, IReadOnlyList<int> p_lisHeldChips)
        {
            HashSet<int> setDistinctValues = new HashSet<int>(p_lisHeldChips);
            foreach (int nNumber in setDistinctValues)
            {
                for (int nY = 0; nY < KingsCrownGameFactory.BOARD_SIZE; nY++)
                {
                    for (int nX = 0; nX < KingsCrownGameFactory.BOARD_SIZE; nX++)
                    {
                        if (KingsCrownPlacementRules.CanPlace(p_objContext, nX, nY, p_eColor, nNumber))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void DeclareWinner(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eWinnerColor)
        {
            p_objContext.mv_isGameOver = true;

            Ont.PlayerState? objWinner = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == p_eWinnerColor);
            if (objWinner is not null)
            {
                objWinner.mv_nScore = 1;
            }
        }
    }
}
