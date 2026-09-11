namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using NK = BoardMaster.Core.Rules.NineKnights;

    internal static class NineKnightsGameSessionTests
    {
        public static void CurrentPhaseName_StartsAsMainPlay()
        {
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(NK.NineKnightsGameFactory.CreateStandardGame(new Random(1)), new Random(2));
            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "시작 시점엔 MainPlay 페이즈여야 한다");
        }

        public static void GetMissionNumber_And_HiddenNumber_AreWithinValidRanges()
        {
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(NK.NineKnightsGameFactory.CreateStandardGame(new Random(1)), new Random(2));

            int nMission = objSession.GetMissionNumber(Ont.E_PlayerColor.Black);
            int nHidden = objSession.GetHiddenNumber(Ont.E_PlayerColor.Black);

            Assert.IsTrue(nMission is >= 1 and <= 9, "임무 번호는 1~9여야 한다");
            Assert.IsTrue(nHidden is >= 1 and <= 5, "히든 토큰 번호는 1~5여야 한다");
        }

        public static void MovePiece_MovesToEmptyCell_AndSwitchesTurn()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, new Random(1));

            Ont.GameContext objResult = objSession.MovePiece(4, 4, 5, 5);

            Assert.IsTrue(objSession.GetPieceAt(5, 5) is not null, "빈 칸으로 이동한 기사가 도착 칸에 있어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_eActiveColor, "이동 후 턴이 넘어가야 한다");
        }

        public static void MovePiece_RevealsBothCombatants_OnAttack()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 7, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 3, 5, 5);
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, new Random(1));

            Assert.IsTrue(!objSession.IsRevealed("Black_Knight_7"), "전투 전에는 아직 공개되면 안 된다");

            objSession.MovePiece(4, 4, 5, 5);

            Assert.IsTrue(objSession.IsRevealed("Black_Knight_7"), "전투에 참여한 공격자는 공개되어야 한다");
            Assert.IsTrue(objSession.IsRevealed("White_Knight_3"), "전투에 참여한 방어자는 승패와 무관하게 공개되어야 한다");
        }

        public static void MovePiece_DoesNotReveal_WhenNoCombatOccurs()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, new Random(1));

            objSession.MovePiece(4, 4, 5, 5);

            Assert.IsTrue(!objSession.IsRevealed("Black_Knight_5"), "전투 없이 이동만 했으면 공개되면 안 된다");
        }

        public static void MovePiece_Throws_WhenMovingOpponentsPiece()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 5, 4, 4);
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, new Random(1));

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.MovePiece(4, 4, 5, 5),
                "Black 차례에 White 기물을 움직이려 하면 거부되어야 한다");
        }

        public static void GetAllLegalMoves_ReturnsMovesForAllOwnPieces()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 0, 0); // 구석 -> 이동 후보 3개(King무브, 모서리)
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 6, 8, 8); // 반대쪽 구석 -> 이동 후보 3개
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, new Random(1));

            List<(int FromX, int FromY, int ToX, int ToY)> lisMoves = objSession.GetAllLegalMoves(Ont.E_PlayerColor.Black);

            Assert.AreEqual(6, lisMoves.Count, "구석 두 기물 합쳐 3+3=6개의 합법수가 있어야 한다");
        }

        public static void MovePiece_Throws_AfterGameHasEnded()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4); // White가 없어 다음 수로 전멸승 조건을 만든다.
            NK.NineKnightsGameSession objSession = new NK.NineKnightsGameSession(objContext, new Random(1));

            objSession.MovePiece(4, 4, 4, 5); // 상대가 전멸 상태이므로 이 수로 즉시 종국된다.
            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "상대가 전멸했으므로 종국되어야 한다");

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.MovePiece(4, 5, 4, 6),
                "종국된 대국에서 기사를 움직이려 하면 거부되어야 한다");
        }
    }
}
