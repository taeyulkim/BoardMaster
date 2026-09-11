namespace BoardMaster.Core.Rules.NineKnights
{
    /// <summary>
    /// Chess의 ChessActionCoding과 같은 이유입니다 — ST_ActionData는 좌표 하나만 실어 나르는데
    /// 나인 나이츠 한 수도 출발/도착 두 좌표가 필요해서, 각 좌표를 칸 인덱스(y*9+x) 하나로 압축해
    /// m_nX(출발)/m_nY(도착)에 나눠 담습니다.
    /// </summary>
    internal static class NineKnightsActionCoding
    {
        public static int EncodeSquare(int p_nX, int p_nY)
        {
            return (p_nY * NineKnightsGameFactory.BOARD_SIZE) + p_nX;
        }

        public static (int X, int Y) DecodeSquare(int p_nIndex)
        {
            return (p_nIndex % NineKnightsGameFactory.BOARD_SIZE, p_nIndex / NineKnightsGameFactory.BOARD_SIZE);
        }
    }
}
