namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoEffectsTests
    {
        public static void Effect_SpawnEntity_WritesStoneColorAtCoordinate()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 3, false, Ont.E_PlayerColor.White));

            Go.Effect_SpawnEntity objEffect = new Go.Effect_SpawnEntity();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual((int)Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_a_nBoardGrid[2, 3], "지정 좌표에 착수 색상이 기록되어야 한다");
        }

        public static void Effect_SpawnEntity_DoesNothing_WhenPass()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(2, 3, true, Ont.E_PlayerColor.White));

            Go.Effect_SpawnEntity objEffect = new Go.Effect_SpawnEntity();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual((int)Ont.E_PlayerColor.None, objResult.mv_stCurrentState.m_a_nBoardGrid[2, 3], "패스는 보드에 아무것도 쓰면 안 된다");
        }

        public static void Effect_SwitchTurn_FlipsActiveColor_AndIncrementsTurnNumber()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_nTurnNumber: 1, p_eActiveColor: Ont.E_PlayerColor.Black);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Go.Effect_SwitchTurn objEffect = new Go.Effect_SwitchTurn();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual(Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_eActiveColor, "Black 다음은 White여야 한다");
            Assert.AreEqual(2, objResult.mv_stCurrentState.m_nTurnNumber, "턴 번호가 1 증가해야 한다");

            Ont.GameContext objResult2 = objEffect.Apply(objResult, objAction);
            Assert.AreEqual(Ont.E_PlayerColor.Black, objResult2.mv_stCurrentState.m_eActiveColor, "White 다음은 다시 Black이어야 한다");
        }

        public static void Effect_CaptureStones_RemovesZeroLibertyGroup_AndSyncsPrisonerCounts()
        {
            // Cond_NotSuicideTests의 캡처 시나리오와 동일한 배치. 이번엔 Black(0,0)이 "이미 놓인 상태"에서
            // Effect_CaptureStones만 단독으로 돌려, White(1,0) 그룹 제거와 포로 수 갱신을 검증한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("White", Ont.E_PlayerColor.White));

            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 0, 0, Ont.E_PlayerColor.Black); // Effect_SpawnEntity가 이미 실행된 것으로 가정

            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Go.Effect_CaptureStones objEffect = new Go.Effect_CaptureStones();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual((int)Ont.E_PlayerColor.None, objResult.mv_stCurrentState.m_a_nBoardGrid[1, 0], "활로 0이 된 White(1,0)은 제거되어야 한다");
            Assert.AreEqual((int)Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_a_nBoardGrid[0, 1], "다른 활로가 남은 White(0,1)은 그대로 남아야 한다");
            Assert.AreEqual(1, objResult.mv_stCurrentState.m_nBlackPrisoners, "ST_BoardState의 Black 포로 수가 1이어야 한다");
            Assert.AreEqual(0, objResult.mv_stCurrentState.m_nWhitePrisoners, "White 포로 수는 변하지 않아야 한다");
            Assert.AreEqual(1, objResult.mv_lisPlayers[0].mv_nPrisonerCount, "Black PlayerState의 포로 수도 동기화되어야 한다");
        }

        public static void Effect_CaptureStones_DoesNothing_WhenNoGroupReachesZeroLiberties()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 3, 2, Ont.E_PlayerColor.White); // White가 다른 활로를 여전히 갖고 있음

            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Go.Effect_CaptureStones objEffect = new Go.Effect_CaptureStones();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual((int)Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_a_nBoardGrid[3, 2], "활로가 남은 그룹은 제거되면 안 된다");
            Assert.AreEqual(0, objResult.mv_stCurrentState.m_nBlackPrisoners, "포로가 없어야 한다");
        }

        public static void Effect_CaptureStones_DoesNothing_WhenPass()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));

            Go.Effect_CaptureStones objEffect = new Go.Effect_CaptureStones();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual((int)Ont.E_PlayerColor.White, objResult.mv_stCurrentState.m_a_nBoardGrid[1, 0], "패스는 보드를 건드리면 안 된다");
        }

        public static void Effect_CheckConsecutivePassGameEnd_SetsGameOver_WhenPreviousAndCurrentBothPass()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.AppendHistory(new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black)); // 직전 행동 = 패스

            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.White));

            Go.Effect_CheckConsecutivePassGameEnd objEffect = new Go.Effect_CheckConsecutivePassGameEnd();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.IsTrue(objResult.mv_isGameOver, "직전/현재 행동이 둘 다 패스이면 종국 처리되어야 한다");
        }

        public static void Effect_CheckConsecutivePassGameEnd_DoesNotEndGame_WhenPreviousWasPlacement()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.AppendHistory(new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black)); // 직전 행동 = 착수

            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.White));

            Go.Effect_CheckConsecutivePassGameEnd objEffect = new Go.Effect_CheckConsecutivePassGameEnd();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.IsTrue(!objResult.mv_isGameOver, "직전 행동이 착수였다면 이번 패스만으로는 종국되면 안 된다");
        }

        public static void Effect_CheckConsecutivePassGameEnd_DoesNotEndGame_WhenCurrentActionIsPlacement()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.AppendHistory(new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black)); // 직전 행동 = 패스

            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.White));

            Go.Effect_CheckConsecutivePassGameEnd objEffect = new Go.Effect_CheckConsecutivePassGameEnd();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.IsTrue(!objResult.mv_isGameOver, "현재 행동이 착수라면 직전이 패스였어도 종국되면 안 된다");
        }

        public static void Effect_CheckConsecutivePassGameEnd_DoesNotEndGame_WhenHistoryIsEmpty()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5); // 이력 없음 (첫 수)

            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));

            Go.Effect_CheckConsecutivePassGameEnd objEffect = new Go.Effect_CheckConsecutivePassGameEnd();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.IsTrue(!objResult.mv_isGameOver, "첫 수부터 패스여도 직전 행동이 없으므로 종국되면 안 된다");
        }

        public static void Effect_FinalizeScore_AssignsScores_WhenGameOver()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("White", Ont.E_PlayerColor.White));
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.Black);
            objContext.mv_isGameOver = true;

            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.White));

            Go.Effect_FinalizeScore objEffect = new Go.Effect_FinalizeScore();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual(25, objResult.mv_lisPlayers[0].mv_nScore, "Black 점수는 돌 1개 + 나머지 전체 집 24칸 = 25여야 한다");
            Assert.AreEqual(0, objResult.mv_lisPlayers[1].mv_nScore, "White 점수는 0이어야 한다");
        }

        public static void Effect_FinalizeScore_DoesNothing_WhenGameNotOver()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.Black);
            // objContext.mv_isGameOver는 기본값 false 그대로 둔다.

            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.White));

            Go.Effect_FinalizeScore objEffect = new Go.Effect_FinalizeScore();
            Ont.GameContext objResult = objEffect.Apply(objContext, objAction);

            Assert.AreEqual(0, objResult.mv_lisPlayers[0].mv_nScore, "게임이 끝나지 않았으면 점수를 채우면 안 된다");
        }
    }
}
