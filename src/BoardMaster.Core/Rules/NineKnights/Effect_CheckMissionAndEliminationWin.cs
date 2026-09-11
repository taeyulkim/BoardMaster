namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 두 가지 승리 조건을 확인합니다: (1) 방금 이동한 기사가 전투에서 살아남아 상대 뒷줄에
    /// 도착했고, 그 기사의 번호가 이동한 플레이어 자신의 비밀 임무 번호와 같다 — 즉시 승리.
    /// (2) 상대가 반상/예비를 통틀어 기사가 한 명도 안 남았다(완전 전멸) — 즉시 승리.
    /// 임무 번호는 GameContext에 없는 비밀 값이라 NineKnightsGameSession이 생성자로 주입합니다.
    /// </summary>
    public sealed class Effect_CheckMissionAndEliminationWin : Ont.IEffect
    {
        private readonly int m_nMoverMissionNumber;

        public Effect_CheckMissionAndEliminationWin(int p_nMoverMissionNumber)
        {
            m_nMoverMissionNumber = p_nMoverMissionNumber;
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

            Ont.E_PlayerColor eMoverColor = p_objAction.mv_stActionData.m_eColor;
            (int nToX, int nToY) = NineKnightsActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            Ont.Entity? objPieceAtDestination = NineKnightsZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            if (objPieceAtDestination is not null && objPieceAtDestination.mv_eColor == eMoverColor)
            {
                int nOpponentBackRow = NineKnightsBoardGeometry.OpponentBackRow(eMoverColor);
                if (nToY == nOpponentBackRow && int.Parse(objPieceAtDestination.mv_strType) == m_nMoverMissionNumber)
                {
                    DeclareWinner(p_objContext, eMoverColor);
                    return p_objContext;
                }
            }

            Ont.E_PlayerColor eOpponentColor = NineKnightsBoardGeometry.Opponent(eMoverColor);
            if (NineKnightsZoneQuery.IsEliminated(p_objContext, eOpponentColor))
            {
                DeclareWinner(p_objContext, eMoverColor);
            }

            return p_objContext;
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
