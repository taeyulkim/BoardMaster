namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using GK = BoardMaster.Core.Rules.GreatKingdom;

    internal static class GreatKingdomGameFactoryTests
    {
        public static void CreateStandardGame_PlacesNeutralCastleAtCenter()
        {
            Ont.GameContext objContext = GK.GreatKingdomGameFactory.CreateStandardGame();
            int[,] a_nGrid = objContext.mv_stCurrentState.m_a_nBoardGrid;

            Assert.AreEqual(GK.GreatKingdomCell.Neutral, a_nGrid[4, 4], "9x9 보드의 정중앙(4,4)에 중립 성이 있어야 한다");
        }

        public static void CreateStandardGame_BoardIsOtherwiseEmpty()
        {
            Ont.GameContext objContext = GK.GreatKingdomGameFactory.CreateStandardGame();
            int[,] a_nGrid = objContext.mv_stCurrentState.m_a_nBoardGrid;

            int nOccupiedCount = 0;
            for (int nY = 0; nY < GK.GreatKingdomGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < GK.GreatKingdomGameFactory.BOARD_SIZE; nX++)
                {
                    if (a_nGrid[nX, nY] != GK.GreatKingdomCell.Empty)
                    {
                        nOccupiedCount++;
                    }
                }
            }

            Assert.AreEqual(1, nOccupiedCount, "중립 성 하나를 빼면 나머지는 전부 비어 있어야 한다");
        }

        public static void CreateStandardGame_Player1MovesFirst()
        {
            Ont.GameContext objContext = GK.GreatKingdomGameFactory.CreateStandardGame();
            Assert.AreEqual(Ont.E_PlayerColor.Black, objContext.mv_stCurrentState.m_eActiveColor, "선공(Player1)이 먼저 둔다");
        }
    }
}
