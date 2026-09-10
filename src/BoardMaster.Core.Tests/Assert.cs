namespace BoardMaster.Core.Tests
{
    /// <summary>
    /// 외부 NuGet 패키지(xUnit 등) 없이도 이 검증 하네스가 동작할 수 있도록 만든 최소 단언 헬퍼입니다.
    /// 실패 시 예외를 던지며, 각 시나리오 메서드가 이 예외를 통해 성공/실패를 보고합니다.
    /// </summary>
    internal static class Assert
    {
        public static void IsTrue(bool p_bCondition, string p_strMessage)
        {
            if (!p_bCondition)
            {
                throw new Exception($"Assertion failed: {p_strMessage}");
            }
        }

        public static void AreEqual<T>(T p_objExpected, T p_objActual, string p_strMessage)
        {
            if (!EqualityComparer<T>.Default.Equals(p_objExpected, p_objActual))
            {
                throw new Exception($"Assertion failed: {p_strMessage}. Expected=[{p_objExpected}] Actual=[{p_objActual}]");
            }
        }

        public static TException Throws<TException>(Action p_fnAction, string p_strMessage) where TException : Exception
        {
            try
            {
                p_fnAction();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Assertion failed: {p_strMessage}. Expected [{typeof(TException).Name}] but caught [{ex.GetType().Name}]: {ex.Message}");
            }

            throw new Exception($"Assertion failed: {p_strMessage}. Expected [{typeof(TException).Name}] but no exception was thrown");
        }
    }
}
