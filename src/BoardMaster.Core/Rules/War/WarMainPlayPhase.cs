namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// 라운드가 계속 진행되는 유일한 거시 단계입니다. Go의 GoMainPlayPhase와 구조가 거의 동일합니다 —
    /// 의도적입니다: 이 IGamePhase 구현이 완전히 다른 장르에서도 동일한 GamePhaseManager로
    /// 아무 문제 없이 돌아간다는 걸 보여주기 위한 것이라, 굳이 다르게 짤 이유가 없었습니다.
    /// </summary>
    public sealed class WarMainPlayPhase : OntDyn.IGamePhase
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
