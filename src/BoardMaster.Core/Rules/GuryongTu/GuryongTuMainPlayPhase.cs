namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// 라운드가 계속 진행되는 유일한 거시 단계입니다. Go/War와 구조가 동일합니다 — 같은
    /// GamePhaseManager가 세 번째로 다른 장르에서도 그대로 돌아간다는 걸 보여주는 지점입니다.
    /// </summary>
    public sealed class GuryongTuMainPlayPhase : OntDyn.IGamePhase
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
