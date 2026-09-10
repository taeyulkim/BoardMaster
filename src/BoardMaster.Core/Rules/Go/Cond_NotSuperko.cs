namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 위치 기반 슈퍼코(positional superko) 규칙 검증기입니다. 이 착수(+그로 인한 모든 포획)를 실제로
    /// 반영했을 때 나오는 반상 배치가 이 대국에서 이전에 한 번이라도 나온 적 있는 배치와 완전히 같다면
    /// 거부합니다. Cond_NotKoRecapture(단순패)는 "바로 직전 한 수"만 되돌리는 걸 막지만, 이건 삼패
    /// (triple ko) 같은 더 긴 순환 반복까지 전부 막습니다.
    ///
    /// 성능: Zobrist 해시(GoZobristTable)로 결과 배치를 계산합니다. 격자를 복제하거나 전체를 훑어
    /// 문자열로 직렬화하지 않고, 현재 배치의 해시(생성자로 주입받음)에 "바뀌는 칸"만 XOR합니다 —
    /// 착수한 칸 하나 + (포획이 있다면) 포획된 돌들. 포획 여부/좌표는 Cond_NotSuicide와 똑같은
    /// GoGroupScanner의 가상 치환(override) BFS로 찾으므로, 여기서도 격자를 절대 복제하지 않습니다.
    /// 대부분의 후보 수는 아무것도 포획하지 않으므로 사실상 O(1)입니다(예전 구현은 후보 하나마다
    /// O(width*height) 복제+직렬화였습니다).
    ///
    /// 반상 배치 이력은 GoGameSession이 소유한 HashSet&lt;ulong&gt;을 참조로 그대로 주입받아 씁니다
    /// (GameContext에는 바둑 전용 상태를 얹지 않는다는 기존 원칙을 여기서도 유지합니다). "현재 배치의
    /// 해시"는 매 착수마다 값이 바뀌므로 참조가 아니라 생성 시점의 값으로 고정해서 받습니다 —
    /// GoGameSession은 실제로 착수가 성공할 때마다 이 조건 인스턴스를 새로 만들어 최신 해시를
    /// 넘깁니다(Cond_NotKoRecapture가 매 수마다 새로 만들어지는 것과 같은 이유).
    /// </summary>
    public sealed class Cond_NotSuperko : Ont.ICondition
    {
        private readonly ulong m_ulCurrentPositionHash;
        private readonly HashSet<ulong> m_setVisitedPositionHashes;
        private readonly GoGroupScanner m_objScanner = new GoGroupScanner();
        private int[] m_a_nGroupMemberScratch = Array.Empty<int>();

        public Cond_NotSuperko(ulong p_ulCurrentPositionHash, HashSet<ulong> p_setVisitedPositionHashes)
        {
            m_ulCurrentPositionHash = p_ulCurrentPositionHash;
            m_setVisitedPositionHashes = p_setVisitedPositionHashes ?? throw new ArgumentNullException(nameof(p_setVisitedPositionHashes));
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

            ulong ulResultingHash = ComputeResultingHash(a_nGrid, nWidth, nHeight, nX, nY, eColor);

            return !m_setVisitedPositionHashes.Contains(ulResultingHash);
        }

        /// <summary>
        /// 실제 격자를 전혀 건드리지 않고(복제도 하지 않고), 현재 해시에 이 착수로 바뀌는 칸만
        /// XOR해서 결과 배치의 해시를 계산합니다.
        /// </summary>
        private ulong ComputeResultingHash(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            ulong ulHash = m_ulCurrentPositionHash ^ GoZobristTable.GetValue(p_nX, p_nY, p_eColor);

            Ont.E_PlayerColor eOpponentColor =
                (p_eColor == Ont.E_PlayerColor.Black) ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;

            ulHash = XorOutIfCaptured(ulHash, p_a_nGrid, p_nWidth, p_nHeight, p_nX - 1, p_nY, eOpponentColor, p_nX, p_nY, p_eColor);
            ulHash = XorOutIfCaptured(ulHash, p_a_nGrid, p_nWidth, p_nHeight, p_nX + 1, p_nY, eOpponentColor, p_nX, p_nY, p_eColor);
            ulHash = XorOutIfCaptured(ulHash, p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY - 1, eOpponentColor, p_nX, p_nY, p_eColor);
            ulHash = XorOutIfCaptured(ulHash, p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY + 1, eOpponentColor, p_nX, p_nY, p_eColor);

            return ulHash;
        }

        private ulong XorOutIfCaptured(
            ulong p_ulHash,
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nNeighborX,
            int p_nNeighborY,
            Ont.E_PlayerColor p_eOpponentColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor)
        {
            if (p_nNeighborX < 0 || p_nNeighborX >= p_nWidth || p_nNeighborY < 0 || p_nNeighborY >= p_nHeight)
            {
                return p_ulHash;
            }

            if ((Ont.E_PlayerColor)p_a_nGrid[p_nNeighborX, p_nNeighborY] != p_eOpponentColor)
            {
                return p_ulHash;
            }

            int nLiberties = m_objScanner.CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nNeighborX, p_nNeighborY, p_eOpponentColor,
                p_nOverrideX, p_nOverrideY, p_eOverrideColor);

            if (nLiberties > 0)
            {
                return p_ulHash;
            }

            EnsureScratchCapacity(p_nWidth * p_nHeight);
            int nMemberCount = m_objScanner.CollectGroupMembers(
                p_a_nGrid, p_nWidth, p_nHeight, p_nNeighborX, p_nNeighborY, p_eOpponentColor,
                p_nOverrideX, p_nOverrideY, p_eOverrideColor, m_a_nGroupMemberScratch);

            for (int i = 0; i < nMemberCount; i++)
            {
                int nIndex = m_a_nGroupMemberScratch[i];
                int nMemberX = nIndex % p_nWidth;
                int nMemberY = nIndex / p_nWidth;
                p_ulHash ^= GoZobristTable.GetValue(nMemberX, nMemberY, p_eOpponentColor);
            }

            return p_ulHash;
        }

        private void EnsureScratchCapacity(int p_nCellCount)
        {
            if (m_a_nGroupMemberScratch.Length < p_nCellCount)
            {
                m_a_nGroupMemberScratch = new int[p_nCellCount];
            }
        }
    }
}
