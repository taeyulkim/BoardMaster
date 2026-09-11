namespace BoardMaster.Core.Rules.Chess
{
    /// <summary>
    /// ST_ActionData는 좌표 하나(m_nX, m_nY)만 실어 나르도록 만들어졌지만, 체스 한 수는 출발/도착
    /// 두 좌표가 필요합니다. 그래서 각 좌표를 "칸 인덱스"(y*8+x) 하나로 압축해 m_nX(출발)/m_nY(도착)에
    /// 나눠 담습니다 — Go가 m_nX/m_nY를 좌표로, GuryongTu가 m_nX를 타일 랭크로 재활용한 것과 같은
    /// 방식으로 기존 구조체를 다른 의미로 재사용한 것뿐입니다.
    /// </summary>
    internal static class ChessActionCoding
    {
        public static int EncodeSquare(int p_nX, int p_nY)
        {
            return (p_nY * ChessGameFactory.BOARD_SIZE) + p_nX;
        }

        public static (int X, int Y) DecodeSquare(int p_nIndex)
        {
            return (p_nIndex % ChessGameFactory.BOARD_SIZE, p_nIndex / ChessGameFactory.BOARD_SIZE);
        }
    }
}
