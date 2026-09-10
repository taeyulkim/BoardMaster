namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// IActionLogger의 기본 구현체입니다. ActionDispatcher 생성 시 별도 로거를 주입하지 않으면 사용됩니다.
    /// </summary>
    public sealed class ConsoleActionLogger : IActionLogger
    {
        public void LogError(string p_strMessage, Exception p_objException)
        {
            Console.Error.WriteLine($"[Engine ERROR] {p_strMessage}: {p_objException.Message}");
        }
    }
}
