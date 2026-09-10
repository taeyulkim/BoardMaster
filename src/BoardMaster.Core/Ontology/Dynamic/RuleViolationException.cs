namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// ActionDispatcher 레벨에서 규칙 위반이나 이미 종료된 대국에 대한 행동 요청을 알리는 도메인 예외입니다.
    /// </summary>
    public class RuleViolationException : Exception
    {
        public RuleViolationException(string p_strMessage) : base(p_strMessage)
        {
        }

        public RuleViolationException(string p_strMessage, Exception p_objInnerException)
            : base(p_strMessage, p_objInnerException)
        {
        }
    }
}
