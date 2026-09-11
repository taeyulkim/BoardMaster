namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// Go의 Effect_CheckConsecutivePassGameEnd와 같은 규칙입니다: 직전 행동과 현재 행동이 둘 다
    /// 패스이면 대국을 종료 처리합니다. Effect_FinalizeScore가 이 뒤에서 영토 계가로 승자를 정합니다
    /// (포위로 인한 즉시 승리는 Effect_CaptureStonesAndCheckSiege가 이미 처리했으므로, 이 경로까지
    /// 오는 건 "아무도 잡히지 않고 양쪽이 패스로 물러난" 경우뿐입니다).
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
