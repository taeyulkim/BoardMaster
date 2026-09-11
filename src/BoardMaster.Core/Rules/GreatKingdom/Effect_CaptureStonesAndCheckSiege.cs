namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 방금 놓인 성 기준 상하좌우 인접한 상대 그룹 중 활로가 0이 된 그룹을 찾아 제거합니다
    /// (Go의 Effect_CaptureStones와 같은 로직, GreatKingdomGroupScanner 기반). 그레이트 킹덤은 여기서
    /// 바둑과 완전히 갈라집니다 — 한 성이라도 포위되어 잡히면 그 즉시 대국이 끝나고 잡은 쪽이
    /// 승리합니다(계가까지 가지 않습니다). 포획과 "포위해서 즉시 승리"가 이 장르에서는 사실상
    /// 하나의 사건이라(잡히는 순간 = 지는 순간), 이 효과 하나가 포획과 승패 판정을 함께 담당합니다 —
    /// War/Chess의 "따로 나뉜 정산 Effect" 패턴과 다른 점이지만, 여기서는 두 사건을 인위적으로
    /// 갈라놓을 이유가 없습니다.
    /// </summary>
    public sealed class Effect_CaptureStonesAndCheckSiege : Ont.IEffect
    {
        private readonly GreatKingdomGroupScanner m_objScanner = new GreatKingdomGroupScanner();
        private int[] m_a_nGroupMemberScratch = Array.Empty<int>();

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

            if (p_objAction.mv_stActionData.m_isPass)
            {
                return p_objContext;
            }

            int nX = p_objAction.mv_stActionData.m_nX;
            int nY = p_objAction.mv_stActionData.m_nY;
            Ont.E_PlayerColor eMoverColor = p_objAction.mv_stActionData.m_eColor;
            int nOpponentCellValue = GreatKingdomCell.Opponent(GreatKingdomCell.FromPlayerColor(eMoverColor));

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            EnsureScratchCapacity(nWidth * nHeight);

            int nCapturedCount = 0;
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX - 1, nY, nOpponentCellValue);
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX + 1, nY, nOpponentCellValue);
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX, nY - 1, nOpponentCellValue);
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX, nY + 1, nOpponentCellValue);

            if (nCapturedCount > 0)
            {
                p_objContext.mv_isGameOver = true;

                Ont.PlayerState? objWinner = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == eMoverColor);
                if (objWinner is not null)
                {
                    objWinner.mv_nScore = 1;
                }
            }

            return p_objContext;
        }

        private int TryCaptureNeighbor(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, int p_nOpponentCellValue)
        {
            if (p_nX < 0 || p_nX >= p_nWidth || p_nY < 0 || p_nY >= p_nHeight)
            {
                return 0;
            }

            if (p_a_nGrid[p_nX, p_nY] != p_nOpponentCellValue)
            {
                return 0;
            }

            int nLiberties = m_objScanner.CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY, p_nOpponentCellValue, -1, -1, GreatKingdomCell.Empty);

            if (nLiberties > 0)
            {
                return 0;
            }

            int nMemberCount = m_objScanner.CollectGroupMembers(
                p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY, p_nOpponentCellValue, -1, -1, GreatKingdomCell.Empty, m_a_nGroupMemberScratch);

            for (int i = 0; i < nMemberCount; i++)
            {
                int nIndex = m_a_nGroupMemberScratch[i];
                int nMemberX = nIndex % p_nWidth;
                int nMemberY = nIndex / p_nWidth;
                p_a_nGrid[nMemberX, nMemberY] = GreatKingdomCell.Empty;
            }

            return nMemberCount;
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
