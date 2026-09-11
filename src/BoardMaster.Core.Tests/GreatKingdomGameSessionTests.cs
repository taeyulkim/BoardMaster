namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using GK = BoardMaster.Core.Rules.GreatKingdom;

    internal static class GreatKingdomGameSessionTests
    {
        public static void CurrentPhaseName_StartsAsMainPlay()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());
            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "시작 시점엔 MainPlay 페이즈여야 한다");
        }

        public static void PlaceStone_PlacesStoneAndSwitchesTurn()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            Ont.GameContext objResult = objSession.PlaceStone(0, 0);

            Assert.AreEqual(GK.GreatKingdomCell.Player1, objResult.mv_stCurrentState.m_a_nBoardGrid[0, 0], "착수 좌표에 Player1 성이 있어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_eActiveColor, "착수 후 턴은 후공(White)으로 넘어가야 한다");
        }

        public static void PlaceStone_Throws_OnOccupiedCell()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlaceStone(4, 4), // 중립 성이 있는 정중앙
                "중립 성이 있는 칸에 착수하면 거부되어야 한다");
        }

        public static void GetLegalMoves_ExcludesOpponentCompletedTerritory()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            // Black이 (0,0) 구석을 완전히 감싼다: (1,0),(0,1)에 착수(White는 그 사이 다른 곳에 둔다).
            // 세 수(Black,White,Black) 뒤에는 White 차례이므로, 이 시점의 GetLegalMoves()가 White 기준이다.
            objSession.PlaceStone(1, 0); // Black
            objSession.PlaceStone(8, 8); // White, 무관한 곳
            objSession.PlaceStone(0, 1); // Black

            List<(int X, int Y)> lisWhiteMoves = objSession.GetLegalMoves();
            Assert.IsTrue(!lisWhiteMoves.Contains((0, 0)), "White는 Black의 완성된 영토(0,0)에 착수할 수 없어야 한다");
        }

        public static void PlaceStone_CapturesAndEndsGameImmediately()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            // White(0,1) 하나를 Black이 (1,1)/(0,2)/(0,0)으로 포위해 유일한 활로 (0,0)만 남긴 뒤 잡는다.
            objSession.PlaceStone(1, 1); // Black
            objSession.PlaceStone(0, 1); // White
            objSession.PlaceStone(0, 2); // Black
            objSession.PlaceStone(8, 8); // White, 무관한 곳
            Ont.GameContext objResult = objSession.PlaceStone(0, 0); // Black, 마지막 활로를 메워 포위

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "성이 포위되면 그 즉시 대국이 끝나야 한다");
            Assert.AreEqual((int)GK.GreatKingdomCell.Empty, objResult.mv_stCurrentState.m_a_nBoardGrid[0, 1], "포위된 White 성은 제거되어야 한다");
            Assert.AreEqual(1, objResult.mv_lisPlayers[0].mv_nScore, "포위한 Black(Player1)이 승자여야 한다");
        }

        public static void ConsecutivePasses_EndGameAutomatically_AndFinalizeScoreDeterminesWinner()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            Ont.GameContext objAfterFirstPass = objSession.Pass();
            Assert.IsTrue(!objAfterFirstPass.mv_isGameOver, "패스 한 번만으로는 종국되면 안 된다");

            Ont.GameContext objAfterSecondPass = objSession.Pass();
            Assert.IsTrue(objAfterSecondPass.mv_isGameOver, "연속 두 번째 패스에서 종국 처리되어야 한다");

            // 아무도 착수하지 않았으므로 양쪽 영토는 0 대 0 — 영토 차가 3 미만이라 후공이 승리한다.
            Assert.AreEqual(0, objAfterSecondPass.mv_lisPlayers[0].mv_nScore, "영토 차가 0이면 선공이 패배해야 한다");
            Assert.AreEqual(1, objAfterSecondPass.mv_lisPlayers[1].mv_nScore, "영토 차가 3 미만이면 후공이 승리해야 한다");
        }

        public static void PlaceStone_Throws_AfterGameHasEnded()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());
            objSession.Pass();
            objSession.Pass();

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlaceStone(0, 0),
                "종국된 대국에 착수를 시도하면 거부되어야 한다");
        }

        public static void Clone_ProducesIndependentSession_MutatingCloneDoesNotAffectOriginal()
        {
            GK.GreatKingdomGameSession objOriginal = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            GK.GreatKingdomGameSession objClone = objOriginal.Clone();
            objClone.PlaceStone(0, 0);

            Assert.AreEqual(GK.GreatKingdomCell.Empty, objOriginal.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid[0, 0], "복제본에 둔 수가 원본에 영향을 주면 안 된다");
            Assert.AreEqual(GK.GreatKingdomCell.Player1, objClone.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid[0, 0], "복제본 자신은 정상적으로 착수가 반영되어야 한다");
        }
    }
}
