namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoGameSessionTests
    {
        public static void PlayStone_PlacesStoneAndSwitchesTurn_OnLegalMove()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Ont.GameContext objResult = objSession.PlayStone(2, 2);

            Assert.AreEqual((int)Ont.E_PlayerColor.Black, objResult.mv_stCurrentState.m_a_nBoardGrid[2, 2], "착수 좌표에 Black 돌이 놓여야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_eActiveColor, "착수 후 턴은 White로 넘어가야 한다");
            Assert.IsTrue(ReferenceEquals(objResult, objSession.mv_objCurrentContext), "세션의 현재 컨텍스트는 반환값과 같아야 한다");
        }

        public static void PlayStone_CapturesOpponentGroup_AndUpdatesBoardAndPrisonerCount()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("White", Ont.E_PlayerColor.White));

            // White(1,0)의 유일한 활로가 (0,0). White(0,1)은 (0,2)라는 다른 활로가 남아있다.
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);
            Ont.GameContext objResult = objSession.PlayStone(0, 0);

            Assert.AreEqual((int)Ont.E_PlayerColor.Black, objResult.mv_stCurrentState.m_a_nBoardGrid[0, 0], "착수한 칸에는 Black 돌이 놓여야 한다");
            Assert.AreEqual((int)Ont.E_PlayerColor.None, objResult.mv_stCurrentState.m_a_nBoardGrid[1, 0], "활로가 0이 된 White(1,0)은 제거되어야 한다");
            Assert.AreEqual((int)Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_a_nBoardGrid[0, 1], "다른 활로가 남은 White(0,1)은 제거되면 안 된다");
            Assert.AreEqual(1, objResult.mv_stCurrentState.m_nBlackPrisoners, "ST_BoardState의 Black 포로 수가 1이어야 한다");
            Assert.AreEqual(1, objResult.mv_lisPlayers[0].mv_nPrisonerCount, "Black PlayerState의 포로 수도 1이어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_eActiveColor, "착수 후 턴은 White로 넘어가야 한다");

            // 세션에 넘긴 원본 objContext는 절대 변경되지 않아야 한다 (Action.Execute의 원자적 전이).
            Assert.AreEqual((int)Ont.E_PlayerColor.White, objContext.mv_stCurrentState.m_a_nBoardGrid[1, 0], "원본 컨텍스트는 여전히 캡처 이전 상태를 유지해야 한다");
            Assert.AreEqual(0, objContext.mv_lisPlayers[0].mv_nPrisonerCount, "원본 컨텍스트의 PlayerState는 오염되면 안 된다");
        }

        public static void PlayStone_Throws_RuleViolationException_OnOccupiedCell()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.White);

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlayStone(2, 2),
                "이미 돌이 있는 칸에 착수하면 RuleViolationException이 발생해야 한다");
        }

        public static void PlayStone_Throws_RuleViolationException_OnSuicide()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlayStone(0, 0),
                "자충수는 RuleViolationException으로 거부되어야 한다");
        }

        public static void PlayStone_LeavesSessionContextUnchanged_OnRejectedMove()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.White);

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);
            Ont.GameContext objContextBeforeAttempt = objSession.mv_objCurrentContext;

            Assert.Throws<OntDyn.RuleViolationException>(() => objSession.PlayStone(2, 2), "점유 칸 착수는 거부되어야 한다");

            Assert.IsTrue(
                ReferenceEquals(objContextBeforeAttempt, objSession.mv_objCurrentContext),
                "실패한 착수는 세션의 현재 컨텍스트를 바꾸면 안 된다");
        }

        public static void Pass_SwitchesTurnWithoutModifyingBoard()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_nTurnNumber: 1, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Ont.GameContext objResult = objSession.Pass();

            Assert.AreEqual(Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_eActiveColor, "패스해도 턴은 넘어가야 한다");
            Assert.AreEqual(2, objResult.mv_stCurrentState.m_nTurnNumber, "턴 번호가 증가해야 한다");
        }

        public static void ConsecutivePasses_EndGameAutomatically_AndFinalizeScore()
        {
            // Black이 (2,2)에 착수 -> White 패스(직전이 착수라 종국 아님) -> Black 패스(직전도 패스라 종국).
            // 종국 시점 보드엔 Black 돌 하나뿐이므로 나머지 24칸이 전부 Black 집 -> Black 25, White 0.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("White", Ont.E_PlayerColor.White));

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.PlayStone(2, 2);
            Ont.GameContext objAfterFirstPass = objSession.Pass();
            Assert.IsTrue(!objAfterFirstPass.mv_isGameOver, "착수 다음의 패스 한 번만으로는 종국되면 안 된다");

            Ont.GameContext objAfterSecondPass = objSession.Pass();

            Assert.IsTrue(objAfterSecondPass.mv_isGameOver, "연속 두 번째 패스에서 자동으로 종국 처리되어야 한다");
            Assert.AreEqual(25, objAfterSecondPass.mv_lisPlayers[0].mv_nScore, "종국 시 Black 점수가 자동으로 계산되어야 한다(돌 1 + 집 24)");
            Assert.AreEqual(0, objAfterSecondPass.mv_lisPlayers[1].mv_nScore, "White 점수는 0이어야 한다");
        }

        public static void PlayStone_Throws_RuleViolationException_AfterGameHasEnded()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.Pass();
            objSession.Pass(); // 연속 패스로 종국

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlayStone(0, 0),
                "종국된 대국에 착수를 시도하면 RuleViolationException이 발생해야 한다");
        }

        /// <summary>
        /// 5x5 보드에 White(2,3)이 (1,2)/(3,2)/(2,1) White와 함께 흑돌 (2,2)를 포위하고,
        /// (1,3)/(3,3)/(2,4) Black이 White(2,3) 자체를 외톨이로 가둬 유일한 활로가 (2,2)가 되도록 만든다.
        /// White가 (2,3)에 두면 Black(2,2) 하나를 따내는 전형적인 단순패 모양이 된다.
        /// </summary>
        private static Ont.GameContext CreateKoSetupContext()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.Black); // 곧 패로 잡힐 흑돌
            TestFixtures.SetGrid(objContext, 1, 2, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 3, 2, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 1, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 1, 3, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 3, 3, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 2, 4, Ont.E_PlayerColor.Black);
            return objContext;
        }

        public static void PlayStone_Throws_RuleViolationException_OnImmediateKoRecapture()
        {
            Ont.GameContext objContext = CreateKoSetupContext();
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Ont.GameContext objAfterCapture = objSession.PlayStone(2, 3); // White가 흑돌 하나를 패로 따낸다
            Assert.AreEqual((int)Ont.E_PlayerColor.None, objAfterCapture.mv_stCurrentState.m_a_nBoardGrid[2, 2], "흑돌이 패로 잡혀 빈 칸이 되어야 한다");

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlayStone(2, 2),
                "직전 수로 단수가 된 외톨이 돌을 즉시 되따내는 것은 패 규칙 위반이어야 한다");
        }

        public static void PlayStone_AllowsKoRecapture_AfterIntervalMoveOnBothSides()
        {
            Ont.GameContext objContext = CreateKoSetupContext();
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.PlayStone(2, 3); // White가 패를 따내며 Black 차례가 된다
            objSession.PlayStone(0, 0); // Black은 패 대신 다른 곳(패 감행 수)에 둔다
            objSession.PlayStone(4, 4); // White도 다른 곳에 둔다

            // 이제 직전 수는 White(4,4)이고 (2,2)와 인접하지 않으므로, Black이 (2,2)를 다시 두는 것은
            // "직전 수를 즉시 되따내기"가 아니라 허용되어야 한다.
            Ont.GameContext objResult = objSession.PlayStone(2, 2);

            Assert.AreEqual((int)Ont.E_PlayerColor.Black, objResult.mv_stCurrentState.m_a_nBoardGrid[2, 2], "한 수씩 주고받은 뒤엔 패 자리를 다시 둘 수 있어야 한다");
        }

        public static void GetLegalMoves_ReturnsAllCells_OnEmptyBoard()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            List<(int X, int Y)> lisMoves = objSession.GetLegalMoves();

            Assert.AreEqual(9, lisMoves.Count, "3x3 빈 보드는 9칸 모두 합법수여야 한다");
        }

        public static void GetLegalMoves_ExcludesOccupiedAndSuicideAndKoCells()
        {
            Ont.GameContext objContext = CreateKoSetupContext();
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.PlayStone(2, 3); // White가 패를 따내 (2,2)가 패 금지 좌표가 된다

            List<(int X, int Y)> lisMoves = objSession.GetLegalMoves();

            Assert.IsTrue(!lisMoves.Contains((1, 2)), "이미 돌이 있는 칸은 후보에서 빠져야 한다");
            Assert.IsTrue(!lisMoves.Contains((2, 2)), "방금 패로 금지된 좌표는 후보에서 빠져야 한다");
        }

        public static void GetLegalMoves_ReturnsEmptyList_WhenGameOver()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.Pass();
            objSession.Pass(); // 연속 패스로 종국

            Assert.AreEqual(0, objSession.GetLegalMoves().Count, "종국된 대국은 합법수가 없어야 한다");
        }

        public static void Clone_ProducesIndependentSession_MutatingCloneDoesNotAffectOriginal()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objOriginal = new Go.GoGameSession(objContext);

            Go.GoGameSession objClone = objOriginal.Clone();
            objClone.PlayStone(2, 2);

            Assert.AreEqual(
                (int)Ont.E_PlayerColor.None,
                objOriginal.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid[2, 2],
                "복제본에 둔 수가 원본 세션에 영향을 주면 안 된다");
            Assert.AreEqual(
                (int)Ont.E_PlayerColor.Black,
                objClone.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid[2, 2],
                "복제본 자신은 정상적으로 착수가 반영되어야 한다");
        }

        public static void Clone_PreservesForbiddenKoPoint()
        {
            Ont.GameContext objContext = CreateKoSetupContext();
            Go.GoGameSession objOriginal = new Go.GoGameSession(objContext);
            objOriginal.PlayStone(2, 3); // White가 패를 따내 (2,2)가 금지된다

            Go.GoGameSession objClone = objOriginal.Clone();

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objClone.PlayStone(2, 2),
                "복제본도 원본과 동일한 패 금지 상태를 물려받아야 한다");
        }

        public static void CurrentPhaseName_TransitionsToGameOver_AfterConsecutivePasses()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "시작 시점엔 MainPlay 페이즈여야 한다");

            objSession.Pass();
            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "패스 한 번만으로는 아직 종국이 아니므로 MainPlay를 유지해야 한다");

            objSession.Pass();
            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "연속 2패스 후엔 GameOver 페이즈로 전이되어야 한다");
        }

        public static void PlayStone_Throws_WithPhaseSpecificMessage_AfterGameOver()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.Pass();
            objSession.Pass();

            OntDyn.RuleViolationException objException = Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlayStone(0, 0),
                "종국 후 착수는 거부되어야 한다");

            Assert.IsTrue(objException.Message.Contains("GameOver"), "예외 메시지가 페이즈 게이트(EnsureMainPlayPhase)에서 나온 것이어야 한다");
        }

        public static void GoGameSession_OpenedWithAlreadyFinishedContext_StartsInGameOverPhase()
        {
            // GoKifuSerializer.Replay처럼 이미 종국된 GameContext로 세션을 여는 경우를 흉내낸다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            objContext.mv_isGameOver = true;

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "이미 종료된 컨텍스트로 열면 곧바로 GameOver 페이즈여야 한다");
            Assert.Throws<OntDyn.RuleViolationException>(() => objSession.Pass(), "이 상태에서는 패스도 거부되어야 한다");
        }
    }
}
