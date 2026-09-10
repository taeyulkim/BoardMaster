namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// 게임 생명주기의 한 거시적 단계(예: 준비, 본 플레이, 정산)를 나타냅니다. 장르에 대해 아무것도
    /// 알지 못하는 순수 계약이며, GameContext 하나만으로 진입/갱신/종료/완료판정을 수행합니다.
    /// 실제 착수 검증(ICondition)이나 상태 전이(IEffect)와는 별개 레이어입니다 — 이 인터페이스는
    /// "지금 어떤 거시 단계인가"만 다루고, "이 행동이 지금 합법인가"는 다루지 않습니다.
    /// </summary>
    public interface IGamePhase
    {
        string mv_strPhaseName { get; }

        void OnPhaseEnter(GameContext p_objContext);

        void OnPhaseUpdate(GameContext p_objContext);

        void OnPhaseExit(GameContext p_objContext);

        bool IsPhaseCompleted(GameContext p_objContext);
    }
}
