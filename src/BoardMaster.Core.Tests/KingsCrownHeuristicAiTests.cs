namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using KC = BoardMaster.Core.Rules.KingsCrown;
    using AiKc = BoardMaster.Core.AI.KingsCrown;

    internal static class KingsCrownHeuristicAiTests
    {
        public static void ChooseMove_ReturnsLegalMove_OnStandardOpening()
        {
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), new Random(1));

            AiKc.KingsCrownHeuristicAi objAi = new AiKc.KingsCrownHeuristicAi();
            (int ChipValue, int X, int Y)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.Black, new Random(2));

            Assert.IsTrue(stMove.HasValue, "합법수가 있는 상황에서는 반드시 수를 골라야 한다");
            List<(int ChipValue, int X, int Y)> lisLegalMoves = objSession.GetLegalPlacements(Ont.E_PlayerColor.Black);
            Assert.IsTrue(lisLegalMoves.Contains(stMove!.Value), "AI가 고른 수는 실제 합법수 목록에 있어야 한다");
        }

        public static void ChooseMove_PicksImmediateBingo_WhenAvailable()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 1, 0, 0);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 2, 1, 0);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 3, 2, 0);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 4, 3, 0);
            // (4,0)에 5를 놓으면(이웃 (3,0)=Black,4와 같은 색+연속) y=0행이 완성되어 즉시 빙고다.
            // 9는 이와 무관한 다른 칸(예비 후보)에 놓을 수 있는 숫자칩이다.

            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 5, 9 }, new List<int> { 1 });

            AiKc.KingsCrownHeuristicAi objAi = new AiKc.KingsCrownHeuristicAi();
            (int ChipValue, int X, int Y)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.Black, new Random(3));

            Assert.IsTrue(stMove.HasValue, "합법수가 있어야 한다");
            Assert.AreEqual((5, 4, 0), stMove!.Value, "즉시 빙고를 완성하는 수를 골라야 한다");
        }

        public static void ChooseMove_PrefersBuildingOwnLine_OverIsolatedQuietMove()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 1, 0, 0);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 2, 1, 0);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 3, 2, 0);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 4, 1);
            // 4는 (3,0)이나 (4,0) 중 어디에 놓아도 y=0행의 3연속을 4연속으로 늘린다(둘 다 (2,0)/(4,1)의
            // Black과 같은 색+연속이라 합법이다) — 정확히 어느 칸인지보다 "그 줄에 기여하는 숫자칩(4)을
            // 골랐는가"가 이 테스트의 관심사다. 9는 (3,0)/(4,0) 어느 쪽에도 연속되지 않아(2와도, 5와도
            // 이어지지 않음) 그 줄에는 놓을 수 없고, 아무 데도 안 이어진 외딴 칸에만 놓을 수 있는 숫자다.

            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 4, 9 }, new List<int> { 1 });

            AiKc.KingsCrownHeuristicAi objAi = new AiKc.KingsCrownHeuristicAi();
            (int ChipValue, int X, int Y)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.Black, new Random(4));

            Assert.IsTrue(stMove.HasValue, "합법수가 있어야 한다");
            Assert.AreEqual(4, stMove!.Value.ChipValue, "이미 쌓은 자기 줄을 더 늘리는 숫자칩을 외딴 조용한 수보다 우선해야 한다");
            Assert.AreEqual(0, stMove.Value.Y, "그 줄(y=0)에 놓아야 한다");
        }

        public static void ChooseMove_PrefersBlockingOpponent_OverNeutralMove()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 1, 0, 4);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 2, 1, 4);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 3, 2, 4);
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.White, 4, 3, 4);
            // White가 y=4행에 4연속을 쌓아 (4,4)만 채우면 빙고인 상황. Black은 그 자리에 다른 색+
            // 같은 숫자(4) 조건으로 합법적으로 끼어들어 그 줄을 영구 봉쇄할 수 있다. 2는 이와 무관한
            // 외딴 칸(0,0)에 놓을 수 있는 중립적인 수다.

            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 4, 2 }, new List<int> { 9 });

            AiKc.KingsCrownHeuristicAi objAi = new AiKc.KingsCrownHeuristicAi();
            (int ChipValue, int X, int Y)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.Black, new Random(5));

            Assert.IsTrue(stMove.HasValue, "합법수가 있어야 한다");
            Assert.AreEqual((4, 4, 4), stMove!.Value, "상대가 거의 완성한 줄을 봉쇄하는 수를 중립적인 수보다 우선해야 한다");
        }

        public static void ChooseMove_ReturnsNull_WhenNoLegalMoves()
        {
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), new List<int>(), new List<int> { 1 });

            AiKc.KingsCrownHeuristicAi objAi = new AiKc.KingsCrownHeuristicAi();
            (int ChipValue, int X, int Y)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.Black, new Random(6));

            Assert.IsTrue(!stMove.HasValue, "보유한 숫자칩이 없으면 둘 수가 없어야 한다");
        }
    }
}
