namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 착수 대상 좌표가 보드 범위 안이면서 비어 있는지 검증합니다. 패스(mv_isPass)는 좌표 검사 없이
    /// 항상 통과합니다.
    ///
    /// 조립 순서 주의: Action에 조건을 등록할 때 이 조건을 Cond_NotSuicide보다 먼저 넣어야 합니다.
    /// Cond_NotSuicide의 BFS는 시작 좌표가 보드 범위 안이라고 가정하므로, 범위 밖 좌표를 먼저 걸러내지
    /// 않으면 배열 인덱스 예외가 날 수 있습니다.
    /// </summary>
    public sealed class Cond_EmptySpace : Ont.ICondition
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

            return a_nGrid[nX, nY] == (int)Ont.E_PlayerColor.None;
        }
    }
}
