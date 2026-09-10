namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 활성 턴 색상을 상대편으로 전환하고 턴 번호를 1 올립니다. 착수/패스 모두 공통으로 필요한 효과이므로
    /// mv_isPass 여부와 무관하게 항상 실행됩니다.
    /// </summary>
    public sealed class Effect_SwitchTurn : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            Ont.E_PlayerColor eCurrentColor = p_objContext.mv_stCurrentState.m_eActiveColor;
            Ont.E_PlayerColor eNextColor =
                (eCurrentColor == Ont.E_PlayerColor.Black) ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;

            Ont.ST_BoardState stCurrent = p_objContext.mv_stCurrentState;
            Ont.ST_BoardState stNext = new Ont.ST_BoardState(
                stCurrent.m_nTurnNumber + 1,
                eNextColor,
                stCurrent.m_a_nBoardGrid,
                stCurrent.m_nBlackPrisoners,
                stCurrent.m_nWhitePrisoners);

            p_objContext.mv_stCurrentState = stNext;

            return p_objContext;
        }
    }
}
