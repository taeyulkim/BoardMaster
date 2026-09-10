namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 위치 기반 슈퍼코(positional superko) 규칙 검증기입니다. 이 착수(+그로 인한 모든 포획)를 실제로
    /// 반영했을 때 나오는 반상 배치가 이 대국에서 이전에 한 번이라도 나온 적 있는 배치와 완전히 같다면
    /// 거부합니다. Cond_NotKoRecapture(단순패)는 "바로 직전 한 수"만 되돌리는 걸 막지만, 이건 삼패
    /// (triple ko) 같은 더 긴 순환 반복까지 전부 막습니다 — 단순패가 막는 경우는 전부 이것의 부분집합
    /// 이지만, 단순패 쪽이 좌표 비교 한 번으로 끝나는 훨씬 싼 검사라 조립 순서상 먼저 두고 이건 나중에
    /// 돌립니다(GoGameSession 참고).
    ///
    /// 반상 배치 이력은 GoGameSession이 소유한 HashSet&lt;string&gt;을 참조로 그대로 주입받아 씁니다
    /// (GameContext에는 바둑 전용 상태를 얹지 않는다는 기존 원칙을 여기서도 유지합니다). GoGameSession은
    /// 매 착수 성공 직후 새 반상 배치의 키를 이 집합에 추가하므로, 이 조건이 보는 집합은 항상
    /// "이번 수 이전까지의 모든 배치"입니다.
    ///
    /// 성능: 후보 하나를 검사할 때마다 격자 복제 + BFS 포획 시뮬레이션 + 문자열 키 계산을 전부
    /// 새로 합니다(Cond_NotSuicide처럼 재사용 버퍼로 zero-alloc을 강제하지 않았습니다). 반상 전체를
    /// 훑는 GoGameSession.GetLegalMoves()에서 매 빈 칸마다 이 검사가 돌면 O(width*height) 후보 ×
    /// O(width*height) 시뮬레이션이 되므로, 큰 보드에서 MCTS가 이 조건을 자주 거치면 병목이 될 수
    /// 있는 지점으로 표시해 둡니다.
    /// </summary>
    public sealed class Cond_NotSuperko : Ont.ICondition
    {
        private readonly HashSet<string> m_setVisitedPositionKeys;
        private readonly GoGroupScanner m_objScanner = new GoGroupScanner();

        public Cond_NotSuperko(HashSet<string> p_setVisitedPositionKeys)
        {
            m_setVisitedPositionKeys = p_setVisitedPositionKeys ?? throw new ArgumentNullException(nameof(p_setVisitedPositionKeys));
        }

        public bool IsSatisfied(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            if (p_objAction.mv_stActionData.m_isPass)
            {
                return true; // 패스는 반상을 바꾸지 않으므로 슈퍼코와 무관하다.
            }

            int nX = p_objAction.mv_stActionData.m_nX;
            int nY = p_objAction.mv_stActionData.m_nY;
            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            int[,] a_nSimulatedGrid = SimulateResultingBoard(a_nGrid, nWidth, nHeight, nX, nY, eColor);
            string strResultingKey = GoBoardPositionKey.Compute(a_nSimulatedGrid, nWidth, nHeight);

            return !m_setVisitedPositionKeys.Contains(strResultingKey);
        }

        /// <summary>
        /// 실제 격자를 건드리지 않고, 복제본 위에 이 착수를 두었을 때(+인접 상대 그룹 포획까지 반영한)
        /// 최종 반상 배치를 계산합니다. Effect_CaptureStones와 같은 4방향 독립 판정 로직을 쓰되,
        /// 실제 상태가 아니라 이 메서드가 만든 복제본에만 반영합니다.
        /// </summary>
        private int[,] SimulateResultingBoard(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            int[,] a_nSimulated = (int[,])p_a_nGrid.Clone();
            a_nSimulated[p_nX, p_nY] = (int)p_eColor;

            Ont.E_PlayerColor eOpponentColor =
                (p_eColor == Ont.E_PlayerColor.Black) ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;

            RemoveIfCaptured(a_nSimulated, p_nWidth, p_nHeight, p_nX - 1, p_nY, eOpponentColor);
            RemoveIfCaptured(a_nSimulated, p_nWidth, p_nHeight, p_nX + 1, p_nY, eOpponentColor);
            RemoveIfCaptured(a_nSimulated, p_nWidth, p_nHeight, p_nX, p_nY - 1, eOpponentColor);
            RemoveIfCaptured(a_nSimulated, p_nWidth, p_nHeight, p_nX, p_nY + 1, eOpponentColor);

            return a_nSimulated;
        }

        private void RemoveIfCaptured(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, Ont.E_PlayerColor p_eOpponentColor)
        {
            if (p_nX < 0 || p_nX >= p_nWidth || p_nY < 0 || p_nY >= p_nHeight)
            {
                return;
            }

            if ((Ont.E_PlayerColor)p_a_nGrid[p_nX, p_nY] != p_eOpponentColor)
            {
                return;
            }

            int nLiberties = m_objScanner.CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY, p_eOpponentColor, -1, -1, Ont.E_PlayerColor.None);

            if (nLiberties > 0)
            {
                return;
            }

            int[] a_nMemberScratch = new int[p_nWidth * p_nHeight];
            int nMemberCount = m_objScanner.CollectGroupMembers(
                p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY, p_eOpponentColor, -1, -1, Ont.E_PlayerColor.None, a_nMemberScratch);

            for (int i = 0; i < nMemberCount; i++)
            {
                int nIndex = a_nMemberScratch[i];
                int nMemberX = nIndex % p_nWidth;
                int nMemberY = nIndex / p_nWidth;
                p_a_nGrid[nMemberX, nMemberY] = (int)Ont.E_PlayerColor.None;
            }
        }
    }
}
