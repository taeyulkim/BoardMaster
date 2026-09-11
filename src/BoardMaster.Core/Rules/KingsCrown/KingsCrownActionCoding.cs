namespace BoardMaster.Core.Rules.KingsCrown
{
    /// <summary>
    /// Ont.ST_ActionData는 정수 슬롯을 m_nX/m_nY 두 개만 제공합니다. 이 장르의 "놓기" 행동은
    /// (칸 좌표, 숫자칩 값) 두 가지만 있으면 충분해서, Chess/나인 나이츠처럼 좌표 두 쌍(출발+도착)을
    /// 하나의 정수로 뭉칠 필요 없이 m_nX에는 EncodeSquare(칸), m_nY에는 숫자칩 값을 그대로 담습니다.
    /// </summary>
    internal static class KingsCrownActionCoding
    {
        public static int EncodeSquare(int p_nX, int p_nY)
        {
            return (p_nY * KingsCrownGameFactory.BOARD_SIZE) + p_nX;
        }

        public static (int X, int Y) DecodeSquare(int p_nEncoded)
        {
            int nSize = KingsCrownGameFactory.BOARD_SIZE;
            return (p_nEncoded % nSize, p_nEncoded / nSize);
        }
    }
}
