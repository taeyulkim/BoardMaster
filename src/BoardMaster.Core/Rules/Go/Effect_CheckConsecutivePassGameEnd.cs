namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 직전 행동과 현재 행동이 둘 다 패스이면 대국을 종료 처리합니다(mv_isGameOver = true).
    /// Action.Execute는 이 Effect 체인을 다 돈 "다음"에 현재 행동을 mv_lisHistory에 기록하므로,
    /// 여기서 보이는 p_objContext.mv_lisHistory의 마지막 항목은 항상 "직전" 행동입니다.
    /// 현재 행동이 패스가 아니면(착수) 검사 없이 바로 통과시켜 연속 패스 카운트를 자연히 리셋합니다.
    /// </summary>
    public sealed class Effect_CheckConsecutivePassGameEnd : Ont.IEffect
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

            if (!p_objAction.mv_stActionData.m_isPass)
            {
                return p_objContext;
            }

            Ont.GameHistory objHistory = p_objContext.mv_lisHistory;
            if (objHistory.Count > 0 && objHistory.Last.m_isPass)
            {
                p_objContext.mv_isGameOver = true;
            }

            return p_objContext;
        }
    }
}
