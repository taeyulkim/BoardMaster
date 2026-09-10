namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoScoreCalculatorTests
    {
        public static void CalculateScore_AssignsStoneCountPlusSurroundedTerritory_ForOneSidedWalls()
        {
            // 5(가로) x 3(세로) 보드. x=2 전체가 Black 벽, x=4 전체가 White 벽.
            // 왼쪽(x=0,1)은 Black 벽에만 접해 Black 집(6칸), 가운데(x=3)는 양쪽 벽에 다 접해 공배(0점).
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nWidth: 5, p_nHeight: 3);
            for (int nY = 0; nY < 3; nY++)
            {
                TestFixtures.SetGrid(objContext, 2, nY, Ont.E_PlayerColor.Black);
                TestFixtures.SetGrid(objContext, 4, nY, Ont.E_PlayerColor.White);
            }

            Go.GoScoreCalculator objCalculator = new Go.GoScoreCalculator();
            Go.ST_GoScoreResult stResult = objCalculator.CalculateScore(objContext);

            Assert.AreEqual(9, stResult.m_nBlackScore, "Black = 벽 돌 3개 + 왼쪽 집 6칸 = 9여야 한다");
            Assert.AreEqual(3, stResult.m_nWhiteScore, "White = 벽 돌 3개 + 집 0칸(가운데는 공배) = 3이어야 한다");
        }

        public static void CalculateScore_TreatsRegionTouchingBothColors_AsNeutralDame()
        {
            // 5x5 보드, 서로 반대편 모서리에 돌 하나씩만 있고 그 사이 빈 칸은 전부 하나로 연결되어
            // 양쪽 색에 다 접하므로(공배) 어느 쪽에도 집으로 더해지면 안 된다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 0, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 4, 4, Ont.E_PlayerColor.White);

            Go.GoScoreCalculator objCalculator = new Go.GoScoreCalculator();
            Go.ST_GoScoreResult stResult = objCalculator.CalculateScore(objContext);

            Assert.AreEqual(1, stResult.m_nBlackScore, "돌 하나만 세어져야 하고, 공배는 집으로 더해지면 안 된다");
            Assert.AreEqual(1, stResult.m_nWhiteScore, "마찬가지로 White도 돌 하나만 세어져야 한다");
        }

        public static void CalculateScore_ReturnsZeroForBoth_OnEmptyBoard()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);

            Go.GoScoreCalculator objCalculator = new Go.GoScoreCalculator();
            Go.ST_GoScoreResult stResult = objCalculator.CalculateScore(objContext);

            Assert.AreEqual(0, stResult.m_nBlackScore, "빈 보드는 어느 색에도 접하지 않으므로 0이어야 한다");
            Assert.AreEqual(0, stResult.m_nWhiteScore, "빈 보드는 어느 색에도 접하지 않으므로 0이어야 한다");
        }

        public static void CalculateScore_AssignsWholeEmptyBoard_ToSoleColorPresent()
        {
            // 5x5 보드에 Black 돌 하나뿐이면, 나머지 24칸은 전부 하나로 연결된 채 Black에만 접하므로
            // 전부 Black 집이 되어야 한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.Black);

            Go.GoScoreCalculator objCalculator = new Go.GoScoreCalculator();
            Go.ST_GoScoreResult stResult = objCalculator.CalculateScore(objContext);

            Assert.AreEqual(25, stResult.m_nBlackScore, "돌 1개 + 나머지 24칸 전부 Black 집 = 25여야 한다");
            Assert.AreEqual(0, stResult.m_nWhiteScore, "White는 보드에 아무것도 없으므로 0이어야 한다");
        }
    }
}
