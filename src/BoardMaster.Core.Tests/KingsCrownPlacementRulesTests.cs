namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using KC = BoardMaster.Core.Rules.KingsCrown;

    internal static class KingsCrownPlacementRulesTests
    {
        public static void CanPlace_EmptyBoard_IsolatedCell_AnyNumberAllowed()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();

            Assert.IsTrue(KC.KingsCrownPlacementRules.CanPlace(objContext, 0, 0, Ont.E_PlayerColor.Black, 7), "이웃이 없는 칸에는 아무 숫자나 놓을 수 있어야 한다");
        }

        public static void CanPlace_CenterCell_AlwaysFalse()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();

            Assert.IsTrue(!KC.KingsCrownPlacementRules.CanPlace(objContext, 2, 2, Ont.E_PlayerColor.Black, 12), "가운데 칸에는 누구도 왕관을 놓을 수 없다");
        }

        public static void CanPlace_OccupiedCell_False()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 9, 1, 1);

            Assert.IsTrue(!KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 1, Ont.E_PlayerColor.Black, 5), "이미 왕관이 있는 칸에는 놓을 수 없다");
        }

        public static void CanPlace_AdjacentSameColorConsecutiveNumber_True()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 1, 1);

            Assert.IsTrue(KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 2, Ont.E_PlayerColor.Black, 6), "같은 색이면서 연속된 숫자(5->6)는 인접한 칸에 놓을 수 있어야 한다");
            Assert.IsTrue(KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 2, Ont.E_PlayerColor.Black, 4), "같은 색이면서 연속된 숫자(5->4)도 인접한 칸에 놓을 수 있어야 한다");
        }

        public static void CanPlace_AdjacentSameColorNonConsecutiveNumber_False()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 1, 1);

            Assert.IsTrue(!KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 2, Ont.E_PlayerColor.Black, 7), "같은 색이어도 연속되지 않은 숫자는 인접한 칸에 놓을 수 없어야 한다");
        }

        public static void CanPlace_AdjacentDifferentColorSameNumber_True()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 1, 1);

            Assert.IsTrue(KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 2, Ont.E_PlayerColor.White, 5), "다른 색이면서 같은 숫자는 인접한 칸에 놓을 수 있어야 한다");
        }

        public static void CanPlace_AdjacentDifferentColorDifferentNumber_False()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 1, 1);

            Assert.IsTrue(!KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 2, Ont.E_PlayerColor.White, 6), "다른 색이면서 다른 숫자는 인접한 칸에 놓을 수 없어야 한다");
        }

        public static void CanPlace_DiagonalNeighbor_NotTreatedAsAdjacent()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 1, 1);

            // (0,0)은 (1,1)과 대각선 관계일 뿐 상하좌우 인접이 아니므로, (0,0)의 실제 상하좌우
            // 이웃인 (1,0)/(0,1)이 모두 비어 있는 한 아무 숫자나 놓을 수 있어야 한다.
            Assert.IsTrue(KC.KingsCrownPlacementRules.CanPlace(objContext, 0, 0, Ont.E_PlayerColor.White, 12), "대각선 인접은 인접 규칙에 포함되지 않아야 한다");
        }

        public static void CanPlace_MultipleNeighbors_SatisfyingAnyOneSuffices()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 0, 1); // (1,1)의 왼쪽 이웃
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 9, 2, 1); // (1,1)의 오른쪽 이웃

            // White,5를 (1,1)에 놓으면: 왼쪽 이웃(Black,5)과는 "다른 색+같은 숫자"로 만족하지만,
            // 오른쪽 이웃(White,9)과는 "같은 색인데 연속 아님(5,9)"이라 불만족 — 하나만 만족해도 충분.
            Assert.IsTrue(KC.KingsCrownPlacementRules.CanPlace(objContext, 1, 1, Ont.E_PlayerColor.White, 5), "여러 이웃 중 하나의 조건만 만족해도 놓을 수 있어야 한다");
        }

        public static void CanPlace_OutOfBounds_False()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();

            Assert.IsTrue(!KC.KingsCrownPlacementRules.CanPlace(objContext, -1, 0, Ont.E_PlayerColor.Black, 5), "반상 밖 좌표는 놓을 수 없어야 한다");
            Assert.IsTrue(!KC.KingsCrownPlacementRules.CanPlace(objContext, 5, 0, Ont.E_PlayerColor.Black, 5), "반상 밖 좌표는 놓을 수 없어야 한다");
        }
    }
}
