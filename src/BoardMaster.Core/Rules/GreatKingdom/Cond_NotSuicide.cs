namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// Go의 Cond_NotSuicide와 완전히 같은 규칙입니다: 착수 후 내 그룹이 활로를 하나도 못 남기면
    /// 원칙적으로 금지되지만, 그 수로 상대 그룹을 활로 0으로 만들어 따낼 수 있다면 예외로 허용합니다.
    /// GreatKingdomGroupScanner에 위임하며, 중립 성은 어느 쪽 그룹에도 속하지 않고 활로도 아니므로
    /// 자연히 "막힌 벽"처럼 취급됩니다.
    /// </summary>
    public sealed class Cond_NotSuicide : Ont.ICondition
    {
        private readonly GreatKingdomGroupScanner m_objScanner = new GreatKingdomGroupScanner();

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
            int nMyCellValue = GreatKingdomCell.FromPlayerColor(p_objAction.mv_stActionData.m_eColor);
            int nOpponentCellValue = GreatKingdomCell.Opponent(nMyCellValue);

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            int nMyLiberties = m_objScanner.CalculateLiberties(a_nGrid, nWidth, nHeight, nX, nY, nMyCellValue, nX, nY, nMyCellValue);
            if (nMyLiberties > 0)
            {
                return true;
            }

            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX - 1, nY, nOpponentCellValue, nX, nY, nMyCellValue)) return true;
            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX + 1, nY, nOpponentCellValue, nX, nY, nMyCellValue)) return true;
            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX, nY - 1, nOpponentCellValue, nX, nY, nMyCellValue)) return true;
            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX, nY + 1, nOpponentCellValue, nX, nY, nMyCellValue)) return true;

            return false;
        }

        private bool CapturesOpponentGroup(
            int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nNeighborX, int p_nNeighborY, int p_nOpponentCellValue,
            int p_nOverrideX, int p_nOverrideY, int p_nOverrideCellValue)
        {
            if (p_nNeighborX < 0 || p_nNeighborX >= p_nWidth || p_nNeighborY < 0 || p_nNeighborY >= p_nHeight)
            {
                return false;
            }

            int nNeighborValue = (p_nNeighborX == p_nOverrideX && p_nNeighborY == p_nOverrideY)
                ? p_nOverrideCellValue
                : p_a_nGrid[p_nNeighborX, p_nNeighborY];

            if (nNeighborValue != p_nOpponentCellValue)
            {
                return false;
            }

            int nOpponentLiberties = m_objScanner.CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nNeighborX, p_nNeighborY, p_nOpponentCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue);

            return nOpponentLiberties == 0;
        }
    }
}
