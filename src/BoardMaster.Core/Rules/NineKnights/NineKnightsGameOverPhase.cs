namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>종국 이후의 종단(terminal) 단계입니다. 다른 장르와 구조가 동일합니다.</summary>
    public sealed class NineKnightsGameOverPhase : OntDyn.IGamePhase
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
