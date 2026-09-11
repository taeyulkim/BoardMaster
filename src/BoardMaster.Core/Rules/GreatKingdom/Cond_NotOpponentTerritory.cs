namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 그레이트 킹덤이 바둑과 가장 다른 규칙입니다: 이미 완성된 상대의 영토(한 색에만 접하고 중립
    /// 성에는 접하지 않는 빈 영역)에는 아예 착수할 수 없습니다. 바둑이었다면 그런 착수는 보통
    /// 자충수라 Cond_NotSuicide 하나로도 막히지만, 상대 그룹의 활로가 여러 개 남아 있는 큰 영토
    /// 한복판이라면 자충수가 아닐 수도 있습니다 — 그런 경우까지 이 조건이 별도로 막습니다.
    /// </summary>
    public sealed class Cond_NotOpponentTerritory : Ont.ICondition
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
            int nMyCellValue = GreatKingdomCell.FromPlayerColor(p_objAction.mv_stActionData.m_eColor);
            int nOpponentCellValue = GreatKingdomCell.Opponent(nMyCellValue);

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            GreatKingdomTerritoryScanner.RegionResult stRegion =
                GreatKingdomTerritoryScanner.ClassifyRegionContaining(a_nGrid, nWidth, nHeight, nX, nY);

            return stRegion.OwnerCellValue != nOpponentCellValue;
        }
    }
}
