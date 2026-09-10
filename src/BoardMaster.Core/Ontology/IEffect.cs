namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// Action에 조립되어 검증 통과 후 GameContext에 상태 전이를 반영하는 효과입니다.
    /// </summary>
    public interface IEffect
    {
        /// <summary>
        /// 검증된 행동의 결과를 게임 상태 컨텍스트에 반영한 새로운 컨텍스트를 반환합니다.
        /// </summary>
        GameContext Apply(GameContext p_objContext, Action p_objAction);
    }
}
