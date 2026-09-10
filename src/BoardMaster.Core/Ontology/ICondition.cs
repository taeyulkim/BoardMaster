namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// Action에 조립되어 실행 전 규칙 위반 여부를 검증하는 조건 필터입니다.
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// 해당 행동이 도메인 규칙 및 특정 게임 예외 사항을 위반하지 않는지 사전 검증합니다.
        /// </summary>
        bool IsSatisfied(GameContext p_objContext, Action p_objAction);
    }
}
