namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 착수 대상 좌표가 보드 범위 안이면서 비어 있는지(플레이어 성도 중립 성도 없는지) 검증합니다.
    /// 패스는 좌표 검사 없이 항상 통과합니다. Go의 Cond_EmptySpace와 같은 이유로, 조립 순서상
    /// Cond_NotSuicide/Cond_NotOpponentTerritory보다 먼저 와야 합니다(그것들의 BFS가 시작 좌표를
    /// 보드 범위 안으로 가정합니다).
    /// </summary>
    public sealed class Cond_EmptyCell : Ont.ICondition
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

            if (p_objAction.mv_stActionData.m_isPass)
            {
                return true;
            }

            int nX = p_objAction.mv_stActionData.m_nX;
            int nY = p_objAction.mv_stActionData.m_nY;
            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;

            if (nX < 0 || nX >= a_nGrid.GetLength(0) || nY < 0 || nY >= a_nGrid.GetLength(1))
            {
                return false;
            }

            return a_nGrid[nX, nY] == GreatKingdomCell.Empty;
        }
    }
}
