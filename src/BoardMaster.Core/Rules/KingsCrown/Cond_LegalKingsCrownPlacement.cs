namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>왕관 놓기의 합법성을 KingsCrownPlacementRules.CanPlace 하나로 판정합니다.</summary>
    public sealed class Cond_LegalKingsCrownPlacement : Ont.ICondition
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

            (int nX, int nY) = KingsCrownActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            int nNumber = p_objAction.mv_stActionData.m_nY;
            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;

            if (nNumber < 1 || nNumber > KingsCrownGameFactory.MAX_NUMBER)
            {
                return false;
            }

            return KingsCrownPlacementRules.CanPlace(p_objContext, nX, nY, eColor, nNumber);
        }
    }
}
