namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// 종국 이후의 종단(terminal) 단계입니다. IsPhaseCompleted가 항상 false라서 GamePhaseManager는
    /// 여기서 영원히 멈추고, 더 이상 어떤 라운드도 진행되지 않습니다.
    /// </summary>
    public sealed class WarGameOverPhase : OntDyn.IGamePhase
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
