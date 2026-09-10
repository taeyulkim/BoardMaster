namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// 바둑의 실제 착수/패스가 허용되는 유일한 거시 단계입니다. 연속 2회 패스로 Effect_
    /// CheckConsecutivePassGameEnd가 GameContext.mv_isGameOver를 true로 만들면 완료됩니다.
    /// 실제 착수 검증/상태 전이는 이미 GoGameSession(ActionDispatcher, ICondition, IEffect)이
    /// 전부 처리하므로, 이 단계는 "지금 착수를 받아도 되는 거시 상태인가"만 판단하고 별도 로직은
    /// 갖지 않습니다.
    /// </summary>
    public sealed class GoMainPlayPhase : OntDyn.IGamePhase
    {
        public string mv_strPhaseName => "MainPlay";

        public void OnPhaseEnter(Ont.GameContext p_objContext)
        {
        }

        public void OnPhaseUpdate(Ont.GameContext p_objContext)
        {
        }

        public void OnPhaseExit(Ont.GameContext p_objContext)
        {
        }

        public bool IsPhaseCompleted(Ont.GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            return p_objContext.mv_isGameOver;
        }
    }
}
