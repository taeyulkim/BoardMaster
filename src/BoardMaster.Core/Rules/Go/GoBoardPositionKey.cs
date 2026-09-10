namespace BoardMaster.Core.Rules.Go
{
    /// <summary>
    /// int[,] 반상 배치를 위치 기반 슈퍼코 비교에 쓸 문자열 키로 정규화합니다. 셀 값(어느 색 돌이
    /// 있는지)만 보고, 누구 차례인지/몇 수째인지/포로가 몇 개인지는 전혀 고려하지 않습니다 —
    /// "위치(positional)" 슈퍼코의 정의가 원래 그렇습니다(다음 차례까지 같아야 반칙으로 보는
    /// "상황(situational)" 슈퍼코보다 더 엄격합니다).
    /// 정확히 같은 셀 값 나열이라야 같은 문자열이 되므로 충돌 위험이 없습니다(진짜 해시 함수가 아니라
    /// 그리드를 그대로 직렬화한 것이라 그렇습니다).
    /// </summary>
    internal static class GoBoardPositionKey
    {
        public static string Compute(int[,] p_a_nGrid, int p_nWidth, int p_nHeight)
        {
            char[] a_cKey = new char[p_nWidth * p_nHeight];
            int nIndex = 0;

            for (int nY = 0; nY < p_nHeight; nY++)
            {
                for (int nX = 0; nX < p_nWidth; nX++)
                {
                    a_cKey[nIndex] = (char)('0' + p_a_nGrid[nX, nY]);
                    nIndex++;
                }
            }

            return new string(a_cKey);
        }
    }
}
