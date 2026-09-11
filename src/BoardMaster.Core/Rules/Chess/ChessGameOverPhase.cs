namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// 체크메이트/스테일메이트 이후의 종단(terminal) 단계입니다. IsPhaseCompleted가 항상 false라서
    /// GamePhaseManager는 여기서 영원히 멈춥니다.
    /// </summary>
    public sealed class ChessGameOverPhase : OntDyn.IGamePhase
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
