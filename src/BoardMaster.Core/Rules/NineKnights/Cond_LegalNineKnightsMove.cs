namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 모든 기사는 체스의 King처럼 상하좌우/대각선으로 정확히 한 칸만 이동합니다. 출발 칸에 자기
    /// 기사가 있어야 하고, 도착 칸은 보드 안이면서 자기 기물이 없어야 합니다(비어 있거나 상대
    /// 기물이 있으면 합법 — 후자는 전투로 이어집니다).
    /// </summary>
    public sealed class Cond_LegalNineKnightsMove : Ont.ICondition
    {
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

            (int nFromX, int nFromY) = NineKnightsActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            (int nToX, int nToY) = NineKnightsActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            if (nToX < 0 || nToX >= NineKnightsGameFactory.BOARD_SIZE || nToY < 0 || nToY >= NineKnightsGameFactory.BOARD_SIZE)
            {
                return false;
            }

            int nDx = Math.Abs(nToX - nFromX);
            int nDy = Math.Abs(nToY - nFromY);
            if (nDx > 1 || nDy > 1 || (nDx == 0 && nDy == 0))
            {
                return false; // King처럼 정확히 한 칸만 움직일 수 있다(제자리는 이동이 아니다).
            }

            Ont.Entity? objMover = NineKnightsZoneQuery.FindPieceAt(p_objContext, nFromX, nFromY);
            if (objMover is null || objMover.mv_eColor != p_objAction.mv_stActionData.m_eColor)
            {
                return false;
            }

            Ont.Entity? objTarget = NineKnightsZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            return objTarget is null || objTarget.mv_eColor != objMover.mv_eColor;
        }
    }
}
