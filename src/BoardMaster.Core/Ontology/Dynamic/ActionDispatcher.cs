namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// 플레이어의 Action을 접수하는 전역 진입점입니다. 대국 종료 여부를 선결 검사하고,
    /// Action.Execute의 원자적 전이 결과를 받아 실패 시 안전하게 로그를 남긴 뒤
    /// 도메인 예외(RuleViolationException)로 변환하거나 그대로 전파합니다.
    /// 어떤 경로로 실패하든 Action.Execute가 원본 GameContext를 절대 변경하지 않으므로,
    /// 호출자는 예외 발생 후에도 넘겨준 p_objContext를 그대로 안전하게 재사용할 수 있습니다.
    /// </summary>
    public sealed class ActionDispatcher
    {
        private readonly IActionLogger m_objLogger;

        public ActionDispatcher(IActionLogger? p_objLogger = null)
        {
            m_objLogger = p_objLogger ?? new ConsoleActionLogger();
        }

        public GameContext Dispatch(GameContext p_objContext, Action p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            if (p_objContext.mv_isGameOver)
            {
                throw new RuleViolationException("이미 종료된 대국입니다. 추가적인 행동을 취할 수 없습니다.");
            }

            try
            {
                return p_objAction.Execute(p_objContext);
            }
            catch (InvalidOperationException ex)
            {
                string strMessage = $"행동 규칙을 위반하여 거부되었습니다: {p_objAction.mv_strActionType}";
                m_objLogger.LogError(strMessage, ex);
                throw new RuleViolationException(strMessage, ex);
            }
            catch (Exception ex)
            {
                string strMessage = $"상태 전이 연쇄 도중 오류가 발생하여 롤백되었습니다: {p_objAction.mv_strActionType}";
                m_objLogger.LogError(strMessage, ex);
                throw;
            }
        }
    }
}
