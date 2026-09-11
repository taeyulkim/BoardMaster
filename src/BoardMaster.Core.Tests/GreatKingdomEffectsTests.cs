namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using GK = BoardMaster.Core.Rules.GreatKingdom;

    internal static class GreatKingdomEffectsTests
    {
        // ----- Cond_NotOpponentTerritory -----

        public static void Cond_NotOpponentTerritory_ReturnsFalse_ForOpponentsCompletedTerritory()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 3, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 1, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 3, GK.GreatKingdomCell.Player1);
            // (2,2)는 사방이 Player1로만 둘러싸인 완성된 영토.

            DomainAction objAction = new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.White));
            Assert.IsTrue(!new GK.Cond_NotOpponentTerritory().IsSatisfied(objContext, objAction), "상대(Player1)의 완성된 영토에는 Player2가 착수할 수 없어야 한다");
        }

        public static void Cond_NotOpponentTerritory_ReturnsTrue_ForOwnCompletedTerritory()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 3, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 1, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 3, GK.GreatKingdomCell.Player1);

            DomainAction objAction = new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));
            Assert.IsTrue(new GK.Cond_NotOpponentTerritory().IsSatisfied(objContext, objAction), "자기 자신의 완성된 영토에는 착수를 막을 이유가 없어야 한다");
        }

        public static void Cond_NotOpponentTerritory_ReturnsTrue_WhenRegionTouchesNeutralCastle()
        {
            // (2,2)의 네 이웃 중 셋(1,2)/(3,2)/(2,1)은 Player1이지만 나머지 하나(2,3)는 중립 성이라,
            // 이 영역은 "완성된 Player1 영토"가 아니다(중립이 섞이면 누구의 영토도 아님).
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 3, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 1, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 3, GK.GreatKingdomCell.Neutral);

            DomainAction objAction = new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.White));
            Assert.IsTrue(new GK.Cond_NotOpponentTerritory().IsSatisfied(objContext, objAction), "중립 성에 접한 영역은 누구의 완성된 영토도 아니므로 막히면 안 된다");
        }

        public static void Cond_NotOpponentTerritory_ReturnsTrue_ForPass()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            DomainAction objAction = new DomainAction("Action_Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));
            Assert.IsTrue(new GK.Cond_NotOpponentTerritory().IsSatisfied(objContext, objAction), "패스는 항상 통과해야 한다");
        }

        // ----- Cond_NotSuicide -----

        public static void Cond_NotSuicide_ReturnsFalse_ForPureSuicideWithNoCapture()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 0, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 1, GK.GreatKingdomCell.Player2);

            DomainAction objAction = new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));
            Assert.IsTrue(!new GK.Cond_NotSuicide().IsSatisfied(objContext, objAction), "활로도 없고 따낼 것도 없는 자충수는 금지되어야 한다");
        }

        public static void Cond_NotSuicide_ReturnsTrue_WhenPlacementHasOpenLiberties()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            DomainAction objAction = new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));
            Assert.IsTrue(new GK.Cond_NotSuicide().IsSatisfied(objContext, objAction), "빈 반상 한복판은 활로가 있으므로 합법이어야 한다");
        }

        public static void Cond_NotSuicide_ReturnsTrue_WhenPlacementCapturesAdjacentGroup()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 0, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 1, GK.GreatKingdomCell.Player1);

            DomainAction objAction = new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));
            Assert.IsTrue(new GK.Cond_NotSuicide().IsSatisfied(objContext, objAction), "인접 상대 그룹을 활로 0으로 만들어 따내는 수는 자충수가 아니어야 한다");
        }

        // ----- Effect_PlaceStone -----

        public static void Effect_PlaceStone_WritesCorrectCellValue_ForBothPlayers()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            new GK.Effect_PlaceStone().Apply(objContext, new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(1, 1, false, Ont.E_PlayerColor.Black)));
            new GK.Effect_PlaceStone().Apply(objContext, new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.White)));

            Assert.AreEqual(GK.GreatKingdomCell.Player1, objContext.mv_stCurrentState.m_a_nBoardGrid[1, 1], "Black은 Player1 값으로 기록되어야 한다");
            Assert.AreEqual(GK.GreatKingdomCell.Player2, objContext.mv_stCurrentState.m_a_nBoardGrid[2, 2], "White는 Player2 값으로 기록되어야 한다");
        }

        public static void Effect_PlaceStone_DoesNothing_WhenPass()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            new GK.Effect_PlaceStone().Apply(objContext, new DomainAction("Action_Pass", new Ont.ST_ActionData(1, 1, true, Ont.E_PlayerColor.Black)));

            Assert.AreEqual(GK.GreatKingdomCell.Empty, objContext.mv_stCurrentState.m_a_nBoardGrid[1, 1], "패스는 반상을 건드리면 안 된다");
        }

        // ----- Effect_CaptureStonesAndCheckSiege -----

        public static void Effect_CaptureStonesAndCheckSiege_EndsGame_WithMoverAsWinner()
        {
            // White(1,0)의 유일한 활로가 (0,0) — Black이 (0,0)에 두면 그 자리에서 즉시 포위 승리.
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 0, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 1, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 0, GK.GreatKingdomCell.Player1); // Effect_PlaceStone이 이미 실행된 것으로 가정

            new GK.Effect_CaptureStonesAndCheckSiege().Apply(objContext, new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black)));

            Assert.AreEqual(GK.GreatKingdomCell.Empty, objContext.mv_stCurrentState.m_a_nBoardGrid[1, 0], "활로 0이 된 White(1,0)은 제거되어야 한다");
            Assert.IsTrue(objContext.mv_isGameOver, "성이 하나라도 포위되면 그 즉시 대국이 끝나야 한다");
            Assert.AreEqual(1, objContext.mv_lisPlayers[0].mv_nScore, "잡은 쪽(Black/Player1)이 승자여야 한다");
            Assert.AreEqual(0, objContext.mv_lisPlayers[1].mv_nScore, "잡힌 쪽(White/Player2)은 패자여야 한다");
        }

        public static void Effect_CaptureStonesAndCheckSiege_DoesNothing_WhenNoGroupReachesZeroLiberties()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 3, 2, GK.GreatKingdomCell.Player2); // 다른 활로가 남아있음

            new GK.Effect_CaptureStonesAndCheckSiege().Apply(objContext, new DomainAction("Action_PlaceStone", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black)));

            Assert.AreEqual(GK.GreatKingdomCell.Player2, objContext.mv_stCurrentState.m_a_nBoardGrid[3, 2], "활로가 남은 그룹은 제거되면 안 된다");
            Assert.IsTrue(!objContext.mv_isGameOver, "아무도 포위되지 않았으면 대국이 끝나면 안 된다");
        }

        // ----- Effect_CheckConsecutivePassGameEnd -----

        public static void Effect_CheckConsecutivePassGameEnd_SetsGameOver_WhenPreviousAndCurrentBothPass()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            objContext.AppendHistory(new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));

            DomainAction objAction = new DomainAction("Action_Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.White));
            Ont.GameContext objResult = new GK.Effect_CheckConsecutivePassGameEnd().Apply(objContext, objAction);

            Assert.IsTrue(objResult.mv_isGameOver, "직전/현재 행동이 둘 다 패스이면 종국 처리되어야 한다");
        }

        public static void Effect_CheckConsecutivePassGameEnd_DoesNotEndGame_WhenPreviousWasPlacement()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            objContext.AppendHistory(new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            DomainAction objAction = new DomainAction("Action_Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.White));
            Ont.GameContext objResult = new GK.Effect_CheckConsecutivePassGameEnd().Apply(objContext, objAction);

            Assert.IsTrue(!objResult.mv_isGameOver, "직전 행동이 착수였다면 이번 패스만으로는 종국되면 안 된다");
        }

        // ----- Effect_FinalizeScore -----

        public static void Effect_FinalizeScore_DoesNothing_WhenGameNotOver()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            new GK.Effect_FinalizeScore().Apply(objContext, new DomainAction("Action_Pass", default));

            Assert.AreEqual(0, objContext.mv_lisPlayers[0].mv_nScore, "게임이 안 끝났으면 점수를 건드리면 안 된다");
        }

        public static void Effect_FinalizeScore_DoesNothing_WhenSiegeAlreadyDecidedWinner()
        {
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            objContext.mv_isGameOver = true;
            objContext.mv_lisPlayers[1].mv_nScore = 1; // White가 이미 포위로 승리한 상태

            new GK.Effect_FinalizeScore().Apply(objContext, new DomainAction("Action_PlaceStone", default));

            Assert.AreEqual(0, objContext.mv_lisPlayers[0].mv_nScore, "포위로 이미 승자가 정해졌으면 영토 계가로 덮어쓰면 안 된다");
            Assert.AreEqual(1, objContext.mv_lisPlayers[1].mv_nScore, "기존 승자 표시가 그대로 유지되어야 한다");
        }

        public static void Effect_FinalizeScore_Player1Wins_WhenTerritoryDifferenceIsAtLeastThree()
        {
            // Player1의 완전히 분리된 1칸짜리 영토 3개(diff=3) — 핸디캡 기준을 정확히 채운다.
            // White 돌을 하나 멀찍이 놓아두는 게 중요하다 — 안 그러면 반상의 나머지 빈 공간
            // 전체가 "White에는 전혀 안 닿는다"는 이유로 전부 Player1 영토가 되어 버려서,
            // 이 테스트가 실제로는 "3개 차이"가 아니라 훨씬 큰 차이를 테스트하게 된다.
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            objContext.mv_isGameOver = true;
            GreatKingdomTestFixtures.SetCell(objContext, 2, 2, GK.GreatKingdomCell.Player2); // 나머지 공간을 무주공산으로 만든다.
            GreatKingdomTestFixtures.SetCell(objContext, 1, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 1, GK.GreatKingdomCell.Player1); // (0,0) 영토
            GreatKingdomTestFixtures.SetCell(objContext, 3, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 4, 1, GK.GreatKingdomCell.Player1); // (4,0) 영토
            GreatKingdomTestFixtures.SetCell(objContext, 1, 4, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 3, GK.GreatKingdomCell.Player1); // (0,4) 영토

            new GK.Effect_FinalizeScore().Apply(objContext, new DomainAction("Action_Pass", default));

            Assert.AreEqual(1, objContext.mv_lisPlayers[0].mv_nScore, "영토 차가 3 이상이면 선공(Player1)이 승리해야 한다");
            Assert.AreEqual(0, objContext.mv_lisPlayers[1].mv_nScore, "후공은 패배해야 한다");
        }

        public static void Effect_FinalizeScore_Player2Wins_WhenTerritoryDifferenceIsLessThanThree()
        {
            // Player1의 영토가 2칸 더 많아도(diff=2 < 3) 핸디캡을 못 채우면 후공이 승리한다.
            // 위 테스트와 같은 이유로 White 돌 하나를 둬서 나머지 빈 공간이 자동으로 Player1
            // 영토가 되지 않게 한다.
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5);
            objContext.mv_isGameOver = true;
            GreatKingdomTestFixtures.SetCell(objContext, 2, 2, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 1, GK.GreatKingdomCell.Player1); // (0,0) 영토
            GreatKingdomTestFixtures.SetCell(objContext, 3, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 4, 1, GK.GreatKingdomCell.Player1); // (4,0) 영토

            new GK.Effect_FinalizeScore().Apply(objContext, new DomainAction("Action_Pass", default));

            Assert.AreEqual(0, objContext.mv_lisPlayers[0].mv_nScore, "영토가 더 많아도 3 이상 차이가 안 나면 선공은 패배해야 한다");
            Assert.AreEqual(1, objContext.mv_lisPlayers[1].mv_nScore, "이 경우 후공(Player2)이 승리해야 한다");
        }
    }
}
