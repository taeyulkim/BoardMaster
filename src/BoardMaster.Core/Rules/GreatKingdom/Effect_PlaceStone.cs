namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// Go의 Effect_SpawnEntity와 같은 역할입니다 — mv_stActionData 좌표에 착수한 색의 성을 놓습니다.
    /// 격자에는 Ont.E_PlayerColor가 아니라 GreatKingdomCell의 정수 상수를 씁니다. 패스면 통과합니다.
    /// </summary>
    public sealed class Effect_PlaceStone : Ont.IEffect
    {
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
            int nCellValue = GreatKingdomCell.FromPlayerColor(p_objAction.mv_stActionData.m_eColor);

            p_objContext.mv_stCurrentState.m_a_nBoardGrid[nX, nY] = nCellValue;

            return p_objContext;
        }
    }
}
