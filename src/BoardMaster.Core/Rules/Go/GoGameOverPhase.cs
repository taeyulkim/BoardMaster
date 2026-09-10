namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// 종국 이후의 종단(terminal) 단계입니다. 이 단계로 넘어오는 시점엔 이미 Effect_FinalizeScore가
    /// PlayerState.mv_nScore를 채워 둔 상태입니다. IsPhaseCompleted가 항상 false를 반환하므로
    /// GamePhaseManager는 이 단계에서 영원히 멈추고, 더 이상 어떤 착수/패스도 허용되지 않습니다.
    /// </summary>
    public sealed class GoGameOverPhase : OntDyn.IGamePhase
    {
        public string mv_strPhaseName => "GameOver";

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
            return false;
        }
    }
}
