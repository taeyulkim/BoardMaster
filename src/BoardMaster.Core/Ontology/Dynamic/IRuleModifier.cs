namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// 코어 규칙 조립 로직을 건드리지 않고, 특정 예외 상황이나 규칙 변형에서만 Action의 조건/효과
    /// 목록을 끼워 넣거나(추가) 대체하는(제거/교체) 조립 단위입니다. 요구사항 FR-4.2("수정자
    /// 패턴")의 구현입니다.
    ///
    /// 기존에도 ICondition/IEffect를 리스트에 조립하는 것 자체가 장르별 규칙을 갈아끼우는 메커니즘
    /// 이었습니다(Go와 War가 서로 다른 조건/효과 목록을 쓰는 것처럼) — 하지만 그건 항상 오케스트레이터
    /// (GoGameSession 등)의 조립 메서드 안에 "하드코딩"되어 있었습니다. IRuleModifier는 그 조립이
    /// 끝난 뒤에 실행되는 별도 단계를 추가해서, 오케스트레이터의 기본 조립 코드는 그대로 둔 채
    /// 호출자가 생성 시점에 원하는 변형만 주입할 수 있게 합니다(예: 슈퍼코를 끄고 단순패만 적용하는
    /// 변형 룰셋 — GoNoSuperkoModifier 참고).
    /// </summary>
    public interface IRuleModifier
    {
        string mv_strModifierName { get; }

        /// <summary>
        /// 오케스트레이터가 기본 조건/효과를 전부 조립한 뒤의 Action을 넘겨받습니다. 필요한 만큼
        /// p_objAction.mv_lisConditions / mv_lisEffects를 추가·제거·교체하세요. 아무것도 바꾸지
        /// 않으려면 그냥 아무 일도 하지 않으면 됩니다.
        /// </summary>
        void Apply(Action p_objAction, GameContext p_objContext);
    }
}
