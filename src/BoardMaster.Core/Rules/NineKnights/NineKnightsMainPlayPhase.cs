namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>수가 계속 진행되는 유일한 거시 단계입니다. 다른 장르와 구조가 동일합니다.</summary>
    public sealed class NineKnightsMainPlayPhase : OntDyn.IGamePhase
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
