namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// ActionDispatcher가 실행 중 발생한 오류를 기록할 때 쓰는 추상 로깅 창구입니다.
    /// 콘솔/파일/테스트용 스파이 등으로 자유롭게 교체 주입할 수 있도록 인터페이스로 분리했습니다.
    /// </summary>
    public interface IActionLogger
    {
        void LogError(string p_strMessage, Exception p_objException);
    }
}
