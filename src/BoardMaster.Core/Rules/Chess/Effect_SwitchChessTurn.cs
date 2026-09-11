namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 활성 턴 색상을 상대편으로 전환하고 턴 번호를 1 올립니다. Go의 Effect_SwitchTurn과 로직이
    /// 사실상 같지만, 장르끼리 서로 참조하지 않는다는 War/GuryongTu의 원칙(NFR-2)을 그대로 따라
    /// 이 장르 전용으로 다시 둡니다.
    /// </summary>
    public sealed class Effect_SwitchChessTurn : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            Ont.ST_BoardState stCurrent = p_objContext.mv_stCurrentState;
            Ont.E_PlayerColor eNextColor = stCurrent.m_eActiveColor == Ont.E_PlayerColor.White
                ? Ont.E_PlayerColor.Black
                : Ont.E_PlayerColor.White;

            p_objContext.mv_stCurrentState = new Ont.ST_BoardState(
                stCurrent.m_nTurnNumber + 1,
                eNextColor,
                stCurrent.m_a_nBoardGrid,
                stCurrent.m_nBlackPrisoners,
                stCurrent.m_nWhitePrisoners);

            return p_objContext;
        }
    }
}
