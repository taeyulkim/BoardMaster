namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using NK = BoardMaster.Core.Rules.NineKnights;
    using AiNk = BoardMaster.Core.AI.NineKnights;

    internal static class NineKnightsHeuristicAiTests
    {
        public static void ChooseMove_ReturnsLegalMove_OnStandardOpening()
        {
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(NK.NineKnightsGameFactory.CreateStandardGame(new Random(1)), new Random(2));

            AiNk.NineKnightsHeuristicAi objAi = new AiNk.NineKnightsHeuristicAi();
            (int FromX, int FromY, int ToX, int ToY)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.Black, new Random(3));

            Assert.IsTrue(stMove.HasValue, "합법수가 있는 상황에서는 반드시 수를 골라야 한다");

            List<(int FromX, int FromY, int ToX, int ToY)> lisLegalMoves = objSession.GetAllLegalMoves(Ont.E_PlayerColor.Black);
            Assert.IsTrue(lisLegalMoves.Contains(stMove!.Value), "AI가 고른 수는 실제 합법수 목록에 있어야 한다");
        }

        public static void ChooseMove_PicksGuaranteedWinningCapture_OverQuietMoves()
        {
            // 1단계: White(4)가 Black(3)을 공격하지만 진다(차이 1, 낮은 쪽 승 -> 3 승) — Black(3)이
            // 반상에 살아남은 채로 "공개"된다. 2단계: Black이 무관한 수를 하나 둔다. 3단계(검증 대상):
            // White(7)이 이제 공개된 Black(3)을 공격하면 확정 승리(차이 4, 높은 쪽 승)다 — 다른
            // 조용한 후보(White(2)를 그냥 움직이는 것)보다 이 확정 승리 수를 골라야 한다.
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext(Ont.E_PlayerColor.White);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 4, 5, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 3, 5, 5);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 9, 0, 0);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 7, 5, 6);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 2, 8, 8);

            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, 1, 1, 1, 1);
            objSession.MovePiece(5, 4, 5, 5); // White(4) 공격 -> 패배, Black(3) 생존+공개
            objSession.MovePiece(0, 0, 0, 1); // Black(9)의 무관한 수

            AiNk.NineKnightsHeuristicAi objAi = new AiNk.NineKnightsHeuristicAi();
            (int FromX, int FromY, int ToX, int ToY)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.White, new Random(5));

            Assert.IsTrue(stMove.HasValue, "합법수가 있어야 한다");
            Assert.AreEqual((5, 6, 5, 5), stMove!.Value, "이미 공개되어 확정 승리가 보장된 공격을 골라야 한다");
        }

        public static void ChooseMove_PrefersMissionWinningMove_WhenAvailable()
        {
            // White(미션=5)의 5번 기사가 바로 다음 수로 상대(Black) 뒷줄(y=0)에 도달할 수 있다 —
            // 이 수가 다른 어떤 수보다도 압도적으로 높은 점수를 받아야 한다.
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext(Ont.E_PlayerColor.White);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 5, 4, 1); // 다음 수로 (4,0) 도달 가능
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 2, 8, 8); // 다른 후보(의미 없는 수)

            // Player2(White)의 미션 번호를 5로 못박는다.
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, 1, 5, 1, 1);

            AiNk.NineKnightsHeuristicAi objAi = new AiNk.NineKnightsHeuristicAi();
            (int FromX, int FromY, int ToX, int ToY)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.White, new Random(7));

            Assert.IsTrue(stMove.HasValue, "합법수가 있어야 한다");
            // (4,1)에서 뒷줄(y=0)로 가는 칸은 (3,0)/(4,0)/(5,0) 세 개 모두 동등하게 즉시 승리다 —
            // 그래서 정확한 도착 칸을 못박지 않고, "미션 기사가 뒷줄에 도달했는가"만 확인한다.
            Assert.AreEqual(4, stMove!.Value.FromX, "미션 번호와 일치하는 기사(4,1)를 움직여야 한다");
            Assert.AreEqual(1, stMove.Value.FromY, "미션 번호와 일치하는 기사(4,1)를 움직여야 한다");
            Assert.AreEqual(0, stMove.Value.ToY, "상대 뒷줄(y=0)에 도달하는 수를 골라야 한다");
        }

        public static void ChooseMove_AvoidsUnfavorableAttack_WhenSaferMoveExists()
        {
            // Black(7)은 아직 공개되지 않았지만, White(2)가 "상대는 1~9 중 아무 번호나 균등하게
            // 있을 수 있다"는 정직한 가정으로 기대 승률을 계산하면 2는 대부분의 상대(4~9)에게 지므로
            // (이길 수 있는 건 동률인 2와 인접한 3뿐, 9개 중 2개) 기대값이 뚜렷하게 불리하다.
            // 안전한 조용한 수가 있으면 그쪽을 골라야 한다. Black에게 다른 기사(9, 예비)를 하나 더
            // 둔다 — 안 그러면 이 Black(7)이 상대의 마지막 한 명이 되어 "확률은 낮아도 이기면
            // 즉시 전멸승"이라는 정당한 도박이 되어버려, 정작 확인하려던 "일반적인 상황에서의 기대값
            // 회피" 판단이 아니게 된다.
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext(Ont.E_PlayerColor.White);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 2, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 7, 5, 5);
            NineKnightsTestFixtures.PlaceReserve(objContext, Ont.E_PlayerColor.Black, 9);

            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, 1, 1, 1, 1);

            AiNk.NineKnightsHeuristicAi objAi = new AiNk.NineKnightsHeuristicAi();
            (int FromX, int FromY, int ToX, int ToY)? stMove = objAi.ChooseMove(objSession, Ont.E_PlayerColor.White, new Random(11));

            Assert.IsTrue(stMove.HasValue, "합법수가 있어야 한다");
            Assert.IsTrue(!(stMove!.Value.ToX == 5 && stMove.Value.ToY == 5), "기대 승률이 뚜렷하게 불리한 공격(White 2 -> Black 7)을 고르면 안 된다");
        }
    }
}
