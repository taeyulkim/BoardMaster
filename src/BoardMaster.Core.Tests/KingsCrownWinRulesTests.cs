namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using KC = BoardMaster.Core.Rules.KingsCrown;

    internal static class KingsCrownWinRulesTests
    {
        public static void HasBingo_FiveInARow_NotThroughCenter_True()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            for (int nY = 0; nY < 5; nY++)
            {
                KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, nY + 1, 0, nY);
            }

            Assert.IsTrue(KC.KingsCrownWinRules.HasBingo(objContext, Ont.E_PlayerColor.Black), "가운데를 지나지 않는 줄도 5칸이 다 채워지면 빙고여야 한다");
        }

        public static void HasBingo_FourInARowThroughCenter_True()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            // 가운데 행(y=2)은 가운데 칸(2,2)을 지나므로 나머지 4칸만 채우면 빙고여야 한다.
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 1, 0, 2);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 2, 1, 2);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 3, 3, 2);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 4, 4, 2);

            Assert.IsTrue(KC.KingsCrownWinRules.HasBingo(objContext, Ont.E_PlayerColor.Black), "가운데 칸을 포함한 줄은 4칸만 채워도 빙고여야 한다");
        }

        public static void HasBingo_IncompleteLine_False()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            for (int nY = 0; nY < 4; nY++) // (0,4)는 비워둔다.
            {
                KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, nY + 1, 0, nY);
            }

            Assert.IsTrue(!KC.KingsCrownWinRules.HasBingo(objContext, Ont.E_PlayerColor.Black), "한 칸이라도 비어 있으면 빙고가 아니어야 한다");
        }

        public static void HasBingo_OpponentColorBreaksLine_False()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            for (int nY = 0; nY < 4; nY++)
            {
                KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, nY + 1, 0, nY);
            }
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 9, 0, 4);

            Assert.IsTrue(!KC.KingsCrownWinRules.HasBingo(objContext, Ont.E_PlayerColor.Black), "줄 안에 상대 왕관이 하나라도 있으면 빙고가 아니어야 한다");
        }

        public static void WouldCompleteBingo_DetectsHypotheticalWin_WithoutMutatingContext()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            for (int nY = 0; nY < 4; nY++) // (0,4)는 비워둔다 — 이 칸에 놓는다고 가정할 것이다.
            {
                KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, nY + 1, 0, nY);
            }
            int nEntityCountBefore = objContext.mv_lisEntities.Count;

            bool bWouldComplete = KC.KingsCrownWinRules.WouldCompleteBingo(objContext, 0, 4, Ont.E_PlayerColor.Black);

            Assert.IsTrue(bWouldComplete, "마지막 한 칸에 놓는다고 가정하면 빙고가 완성되어야 한다");
            Assert.AreEqual(nEntityCountBefore, objContext.mv_lisEntities.Count, "가정 판정은 실제로 반상을 바꾸지 않아야 한다");
        }
    }
}
