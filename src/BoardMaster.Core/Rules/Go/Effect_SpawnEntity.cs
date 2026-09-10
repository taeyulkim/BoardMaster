namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// mv_stActionData가 가리키는 좌표에 착수한 색상의 돌을 놓는 상태 전이 효과입니다. 패스(mv_isPass)면
    /// 아무것도 쓰지 않고 그대로 통과시킵니다.
    /// Action.Execute가 이 효과에 넘기는 GameContext는 이미 원본과 격리된 복제본이므로, 여기서
    /// mv_stCurrentState.m_a_nBoardGrid를 직접 써도 원본 상태는 오염되지 않습니다.
    /// </summary>
    public sealed class Effect_SpawnEntity : Ont.IEffect
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
            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;

            p_objContext.mv_stCurrentState.m_a_nBoardGrid[nX, nY] = (int)eColor;

            return p_objContext;
        }
    }
}
