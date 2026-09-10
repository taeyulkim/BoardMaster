namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 방금 놓인 돌(mv_stActionData 좌표) 기준 상하좌우 인접한 상대 그룹 중 활로가 0이 된 그룹을 찾아
    /// 보드에서 제거하고, 잡은 만큼 포로 수를 반영합니다. 이 효과는 Effect_SpawnEntity 다음에 실행되어야
    /// 합니다(실제로 돌이 격자에 반영된 뒤에야 상대 그룹의 활로가 정확히 0인지 알 수 있기 때문).
    /// 패스(mv_isPass)면 아무 것도 하지 않습니다.
    ///
    /// 포로 수는 두 곳에 동기화해 기록합니다: PlayerState.mv_nPrisonerCount(플레이어별 값)와
    /// ST_BoardState.m_nBlackPrisoners/m_nWhitePrisoners(경량 스냅샷 값). 두 필드가 같은 의미를 갖고
    /// 있으면서도 서로 자동으로 맞춰지지 않으므로, 캡처가 일어날 때마다 이 효과가 둘 다 갱신해야
    /// AI 스냅샷(ST_BoardState)과 플레이어 조회(PlayerState) 양쪽 어디서 읽어도 값이 어긋나지 않습니다.
    /// </summary>
    public sealed class Effect_CaptureStones : Ont.IEffect
    {
        private readonly GoGroupScanner m_objScanner = new GoGroupScanner();
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
            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;
            Ont.E_PlayerColor eOpponentColor =
                (eColor == Ont.E_PlayerColor.Black) ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            EnsureScratchCapacity(nWidth * nHeight);

            int nCapturedCount = 0;
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX - 1, nY, eOpponentColor);
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX + 1, nY, eOpponentColor);
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX, nY - 1, eOpponentColor);
            nCapturedCount += TryCaptureNeighbor(a_nGrid, nWidth, nHeight, nX, nY + 1, eOpponentColor);

            if (nCapturedCount > 0)
            {
                ApplyPrisonerCount(p_objContext, eColor, nCapturedCount);
            }

            return p_objContext;
        }

        private int TryCaptureNeighbor(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, Ont.E_PlayerColor p_eOpponentColor)
        {
            if (p_nX < 0 || p_nX >= p_nWidth || p_nY < 0 || p_nY >= p_nHeight)
            {
                return 0;
            }

            if ((Ont.E_PlayerColor)p_a_nGrid[p_nX, p_nY] != p_eOpponentColor)
            {
                return 0;
            }

            int nLiberties = m_objScanner.CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY, p_eOpponentColor,
                -1, -1, Ont.E_PlayerColor.None);

            if (nLiberties > 0)
            {
                return 0;
            }

            int nMemberCount = m_objScanner.CollectGroupMembers(
                p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY, p_eOpponentColor,
                -1, -1, Ont.E_PlayerColor.None, m_a_nGroupMemberScratch);

            for (int i = 0; i < nMemberCount; i++)
            {
                int nIndex = m_a_nGroupMemberScratch[i];
                int nMemberX = nIndex % p_nWidth;
                int nMemberY = nIndex / p_nWidth;
                p_a_nGrid[nMemberX, nMemberY] = (int)Ont.E_PlayerColor.None;
            }

            return nMemberCount;
        }

        private static void ApplyPrisonerCount(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eCapturingColor, int p_nCapturedCount)
        {
            Ont.ST_BoardState stCurrent = p_objContext.mv_stCurrentState;
            Ont.ST_BoardState stNext = new Ont.ST_BoardState(
                stCurrent.m_nTurnNumber,
                stCurrent.m_eActiveColor,
                stCurrent.m_a_nBoardGrid,
                stCurrent.m_nBlackPrisoners + (p_eCapturingColor == Ont.E_PlayerColor.Black ? p_nCapturedCount : 0),
                stCurrent.m_nWhitePrisoners + (p_eCapturingColor == Ont.E_PlayerColor.White ? p_nCapturedCount : 0));
            p_objContext.mv_stCurrentState = stNext;

            for (int i = 0; i < p_objContext.mv_lisPlayers.Count; i++)
            {
                Ont.PlayerState objPlayer = p_objContext.mv_lisPlayers[i];
                if (objPlayer.mv_eColor == p_eCapturingColor)
                {
                    objPlayer.mv_nPrisonerCount += p_nCapturedCount;
                    break;
                }
            }
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
