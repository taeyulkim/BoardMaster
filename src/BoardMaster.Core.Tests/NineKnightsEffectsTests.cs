namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using NK = BoardMaster.Core.Rules.NineKnights;

    internal static class NineKnightsEffectsTests
    {
        private static DomainAction MoveAction(int p_nFromX, int p_nFromY, int p_nToX, int p_nToY, Ont.E_PlayerColor p_eColor)
        {
            return new DomainAction(
                "Action_MoveOrAttack",
                new Ont.ST_ActionData(
                    NK.NineKnightsActionCoding.EncodeSquare(p_nFromX, p_nFromY),
                    NK.NineKnightsActionCoding.EncodeSquare(p_nToX, p_nToY),
                    false,
                    p_eColor));
        }

        // ----- Cond_LegalNineKnightsMove -----

        public static void Cond_LegalMove_ReturnsTrue_ForAdjacentEmptyCell()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);

            DomainAction objAction = MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black);
            Assert.IsTrue(new NK.Cond_LegalNineKnightsMove().IsSatisfied(objContext, objAction), "인접한 빈 칸으로는 이동할 수 있어야 한다");
        }

        public static void Cond_LegalMove_ReturnsFalse_ForNonAdjacentCell()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);

            DomainAction objAction = MoveAction(4, 4, 6, 4, Ont.E_PlayerColor.Black);
            Assert.IsTrue(!new NK.Cond_LegalNineKnightsMove().IsSatisfied(objContext, objAction), "두 칸 이상 떨어진 곳으로는 이동할 수 없어야 한다");
        }

        public static void Cond_LegalMove_ReturnsFalse_WhenDestinationHasOwnPiece()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 3, 5, 5);

            DomainAction objAction = MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black);
            Assert.IsTrue(!new NK.Cond_LegalNineKnightsMove().IsSatisfied(objContext, objAction), "자기 기물이 있는 칸으로는 이동할 수 없어야 한다");
        }

        public static void Cond_LegalMove_ReturnsTrue_WhenDestinationHasEnemyPiece()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 3, 5, 5);

            DomainAction objAction = MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black);
            Assert.IsTrue(new NK.Cond_LegalNineKnightsMove().IsSatisfied(objContext, objAction), "적 기물이 있는 칸으로는 이동(공격)할 수 있어야 한다");
        }

        // ----- Effect_MovePieceAndResolveCombat -----

        public static void Effect_MoveAndCombat_MovesToEmptyCell()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4);

            new NK.Effect_MovePieceAndResolveCombat(1).Apply(objContext, MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black));

            Assert.IsTrue(NK.NineKnightsZoneQuery.FindPieceAt(objContext, 5, 5) is not null, "빈 칸으로 이동한 기사가 도착 칸에 있어야 한다");
            Assert.IsTrue(NK.NineKnightsZoneQuery.FindPieceAt(objContext, 4, 4) is null, "출발 칸은 비어야 한다");
        }

        public static void Effect_MoveAndCombat_AttackerWins_RemovesDefender()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 7, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 3, 5, 5); // 7 vs 3: 차이 4(인접 아님) -> 높은 쪽인 7이 승리.

            new NK.Effect_MovePieceAndResolveCombat(1).Apply(objContext, MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black));

            Ont.Entity objAtDestination = NK.NineKnightsZoneQuery.FindPieceAt(objContext, 5, 5)!;
            Assert.AreEqual("7", objAtDestination.mv_strType, "이긴 공격자(7)가 도착 칸에 있어야 한다");
            Assert.IsTrue(NK.NineKnightsZoneQuery.FindPieceAt(objContext, 4, 4) is null, "공격자의 원래 자리는 비어야 한다");
        }

        public static void Effect_MoveAndCombat_AttackerLoses_RemovesAttacker_DefenderStays()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 3, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 7, 5, 5); // 3 vs 7: 차이 4, 높은쪽(7) 승리 -> 공격자(3) 패배.

            new NK.Effect_MovePieceAndResolveCombat(1).Apply(objContext, MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black));

            Ont.Entity objAtDestination = NK.NineKnightsZoneQuery.FindPieceAt(objContext, 5, 5)!;
            Assert.AreEqual("7", objAtDestination.mv_strType, "방어에 성공한 White(7)가 그 자리에 그대로 있어야 한다");
            Assert.IsTrue(NK.NineKnightsZoneQuery.FindPieceAt(objContext, 4, 4) is null, "패배한 공격자의 원래 자리는 비어야 한다(제거됨)");
        }

        public static void Effect_MoveAndCombat_SummonsLowestReserve_AfterCapture()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 7, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 3, 5, 5); // Black이 이겨 White(3)이 제거된다.
            NineKnightsTestFixtures.PlaceReserve(objContext, Ont.E_PlayerColor.White, 9);
            NineKnightsTestFixtures.PlaceReserve(objContext, Ont.E_PlayerColor.White, 2); // 예비 중 가장 낮은 번호.

            new NK.Effect_MovePieceAndResolveCombat(1).Apply(objContext, MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black));

            int nWhiteDeploymentRow = NK.NineKnightsBoardGeometry.DeploymentRow(Ont.E_PlayerColor.White);
            bool bFoundSummonedTwo = false;
            for (int nX = 0; nX < NK.NineKnightsGameFactory.BOARD_SIZE; nX++)
            {
                Ont.Entity? objPiece = NK.NineKnightsZoneQuery.FindPieceAt(objContext, nX, nWhiteDeploymentRow);
                if (objPiece is not null && objPiece.mv_strType == "2")
                {
                    bFoundSummonedTwo = true;
                }
            }

            Assert.IsTrue(bFoundSummonedTwo, "White가 기사를 잃으면 예비 중 가장 낮은 번호(2)가 자기 배치 줄에 자동 소환되어야 한다");
            Assert.AreEqual(1, NK.NineKnightsZoneQuery.FindReservePieces(objContext, Ont.E_PlayerColor.White).Count, "소환된 만큼 예비는 한 명 줄어야 한다(9만 남음)");
        }

        public static void Effect_MoveAndCombat_DoesNotSummon_WhenNoReserveLeft()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 7, 4, 4);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 3, 5, 5);

            new NK.Effect_MovePieceAndResolveCombat(1).Apply(objContext, MoveAction(4, 4, 5, 5, Ont.E_PlayerColor.Black));

            int nWhiteDeploymentRow = NK.NineKnightsBoardGeometry.DeploymentRow(Ont.E_PlayerColor.White);
            for (int nX = 0; nX < NK.NineKnightsGameFactory.BOARD_SIZE; nX++)
            {
                Assert.IsTrue(NK.NineKnightsZoneQuery.FindPieceAt(objContext, nX, nWhiteDeploymentRow) is null, "예비가 없으면 배치 줄에 아무것도 소환되면 안 된다");
            }
        }

        // ----- Effect_CheckMissionAndEliminationWin -----

        public static void Effect_CheckMissionWin_DeclaresWinner_WhenPieceReachesBackRowWithMatchingMission()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 7); // 다음 수로 White 뒷줄(y=8)에 도달 가능.

            new NK.Effect_CheckMissionAndEliminationWin(5).Apply(objContext, MoveAction(4, 7, 4, 8, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objContext.mv_isGameOver, "임무 번호와 일치하는 기사가 상대 뒷줄에 닿으면 즉시 종국되어야 한다");
            Assert.AreEqual(1, objContext.mv_lisPlayers[0].mv_nScore, "이동한 플레이어(Black)가 승자여야 한다");
        }

        public static void Effect_CheckMissionWin_DoesNotDeclareWinner_WhenNumberDoesNotMatchMission()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 7);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 1, 0, 0); // White가 하나라도 남아있어야 전멸승 분기가 끼어들지 않는다.

            new NK.Effect_CheckMissionAndEliminationWin(9).Apply(objContext, MoveAction(4, 7, 4, 8, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objContext.mv_isGameOver, "임무 번호(9)와 다른 기사(5)가 도착해도 승리하면 안 된다");
        }

        public static void Effect_CheckMissionWin_DoesNotDeclareWinner_WhenNotOnBackRow()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 6);
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, 1, 0, 0); // White가 하나라도 남아있어야 전멸승 분기가 끼어들지 않는다.

            new NK.Effect_CheckMissionAndEliminationWin(5).Apply(objContext, MoveAction(4, 6, 4, 7, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objContext.mv_isGameOver, "뒷줄에 도달하지 못했으면 번호가 맞아도 승리하면 안 된다");
        }

        public static void Effect_CheckEliminationWin_DeclaresWinner_WhenOpponentHasNoPiecesLeft()
        {
            Ont.GameContext objContext = NineKnightsTestFixtures.CreateEmptyBoardContext();
            NineKnightsTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, 5, 4, 4); // White는 반상/예비 어디에도 없다 — 완전 전멸 상태를 흉내낸다.

            new NK.Effect_CheckMissionAndEliminationWin(1 /* 임무 무관 */).Apply(objContext, MoveAction(4, 4, 4, 5, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objContext.mv_isGameOver, "상대가 완전히 전멸했으면 그 즉시 종국되어야 한다");
            Assert.AreEqual(1, objContext.mv_lisPlayers[0].mv_nScore, "전멸시킨 쪽(Black)이 승자여야 한다");
        }
    }
}
