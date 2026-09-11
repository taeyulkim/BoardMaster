namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 5x5 반상의 기하 정보입니다. 가운데 칸(2,2)은 누구도 왕관을 놓을 수 없지만, 빙고 판정에서는
    /// "이미 두 플레이어의 왕관이 놓인 것"으로 간주되는 와일드카드입니다 — Lines에서 이 칸을 지나는
    /// 4개의 줄(가로 1, 세로 1, 대각선 2)은 실제로는 4칸만 채워도 빙고가 됩니다.
    /// </summary>
    internal static class KingsCrownBoardGeometry
    {
        public const int CENTER_X = KingsCrownGameFactory.BOARD_SIZE / 2;
        public const int CENTER_Y = KingsCrownGameFactory.BOARD_SIZE / 2;

        public static bool IsCenter(int p_nX, int p_nY)
        {
            return p_nX == CENTER_X && p_nY == CENTER_Y;
        }

        public static Ont.E_PlayerColor Opponent(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;
        }

        /// <summary>빙고 판정용 12개 줄(가로 5 + 세로 5 + 대각선 2). 각 줄은 5칸의 좌표 목록입니다.</summary>
        public static readonly IReadOnlyList<IReadOnlyList<(int X, int Y)>> Lines = BuildLines();

        private static List<IReadOnlyList<(int X, int Y)>> BuildLines()
        {
            int nSize = KingsCrownGameFactory.BOARD_SIZE;
            List<IReadOnlyList<(int X, int Y)>> lisLines = new List<IReadOnlyList<(int X, int Y)>>();

            for (int nY = 0; nY < nSize; nY++)
            {
                List<(int X, int Y)> lisRow = new List<(int X, int Y)>();
                for (int nX = 0; nX < nSize; nX++)
                {
                    lisRow.Add((nX, nY));
                }
                lisLines.Add(lisRow);
            }

            for (int nX = 0; nX < nSize; nX++)
            {
                List<(int X, int Y)> lisColumn = new List<(int X, int Y)>();
                for (int nY = 0; nY < nSize; nY++)
                {
                    lisColumn.Add((nX, nY));
                }
                lisLines.Add(lisColumn);
            }

            List<(int X, int Y)> lisDiagonalMain = new List<(int X, int Y)>();
            List<(int X, int Y)> lisDiagonalAnti = new List<(int X, int Y)>();
            for (int i = 0; i < nSize; i++)
            {
                lisDiagonalMain.Add((i, i));
                lisDiagonalAnti.Add((i, nSize - 1 - i));
            }
            lisLines.Add(lisDiagonalMain);
            lisLines.Add(lisDiagonalAnti);

            return lisLines;
        }
    }
}
