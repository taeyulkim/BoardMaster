namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using GT = BoardMaster.Core.Rules.GuryongTu;

    internal static class GuryongTuEffectsTests
    {
        public static void Cond_TileInHand_ReturnsTrue_WhenRankStillInHand()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            DomainAction objAction = new DomainAction("Action_CommitTile", new Ont.ST_ActionData(5, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(new GT.Cond_TileInHand().IsSatisfied(objContext, objAction), "아직 낸 적 없는 랭크는 커밋 가능해야 한다");
        }

        public static void Cond_TileInHand_ReturnsFalse_WhenAlreadyCommittedThisRound()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(5, 0, false, Ont.E_PlayerColor.Black)));

            DomainAction objSecondAttempt = new DomainAction("Action_CommitTile", new Ont.ST_ActionData(3, 0, false, Ont.E_PlayerColor.Black));
            Assert.IsTrue(!new GT.Cond_TileInHand().IsSatisfied(objContext, objSecondAttempt), "이미 이번 라운드에 커밋했으면 다시 낼 수 없어야 한다");
        }

        public static void Cond_TileInHand_ReturnsFalse_WhenRankAlreadyUsedInPastRound()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(5, 0, false, Ont.E_PlayerColor.Black)));
            Ont.Entity objTile = objContext.mv_lisEntities.Find(e => e.mv_strEntityID == "Black_5")!;
            objTile.mv_objLocatedZone = objContext.mv_dicZones[GT.GuryongTuZoneId.Discard]; // 이미 정산되어 치워진 상태를 흉내낸다.

            DomainAction objReattempt = new DomainAction("Action_CommitTile", new Ont.ST_ActionData(5, 0, false, Ont.E_PlayerColor.Black));
            Assert.IsTrue(!new GT.Cond_TileInHand().IsSatisfied(objContext, objReattempt), "이미 다 써버린 랭크는 다시 낼 수 없어야 한다");
        }

        public static void Effect_CommitTileToPending_MovesOnlyTheSpecifiedTile()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(7, 0, false, Ont.E_PlayerColor.White)));

            Ont.Entity objMoved = objContext.mv_lisEntities.Find(e => e.mv_strEntityID == "White_7")!;
            Assert.AreEqual(GT.GuryongTuZoneId.PendingWhite, objMoved.mv_objLocatedZone.mv_strZoneID, "지정한 타일만 Pending으로 옮겨져야 한다");

            Ont.Entity objUntouched = objContext.mv_lisEntities.Find(e => e.mv_strEntityID == "White_3")!;
            Assert.AreEqual(GT.GuryongTuZoneId.HandWhite, objUntouched.mv_objLocatedZone.mv_strZoneID, "다른 타일은 Hand에 그대로 남아야 한다");
        }

        public static void Effect_ResolveComparisonAndScore_DoesNothing_WhenOnlyOneSideCommitted()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(5, 0, false, Ont.E_PlayerColor.Black)));

            new GT.Effect_ResolveComparisonAndScore().Apply(objContext, new DomainAction("Action_ResolveRound", default));

            Ont.Entity objStillPending = objContext.mv_lisEntities.Find(e => e.mv_strEntityID == "Black_5")!;
            Assert.AreEqual(GT.GuryongTuZoneId.PendingBlack, objStillPending.mv_objLocatedZone.mv_strZoneID, "상대가 안 냈으면 정산되면 안 된다");
            Assert.AreEqual(0, objContext.mv_lisPlayers[0].mv_nScore, "점수도 변하면 안 된다");
        }

        public static void Effect_ResolveComparisonAndScore_HigherRankWins_AndDiscardsBothTiles()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(7, 0, false, Ont.E_PlayerColor.Black)));
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(3, 0, false, Ont.E_PlayerColor.White)));

            new GT.Effect_ResolveComparisonAndScore().Apply(objContext, new DomainAction("Action_ResolveRound", default));

            Ont.PlayerState objBlack = objContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Ont.PlayerState objWhite = objContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;
            Assert.AreEqual(1, objBlack.mv_nScore, "더 높은 랭크(7)를 낸 Black이 1점을 얻어야 한다");
            Assert.AreEqual(0, objWhite.mv_nScore, "White는 점수를 얻지 못해야 한다");

            Assert.AreEqual(GT.GuryongTuZoneId.Discard, objContext.mv_lisEntities.Find(e => e.mv_strEntityID == "Black_7")!.mv_objLocatedZone.mv_strZoneID, "이긴 타일도 Discard로 치워져야 한다");
            Assert.AreEqual(GT.GuryongTuZoneId.Discard, objContext.mv_lisEntities.Find(e => e.mv_strEntityID == "White_3")!.mv_objLocatedZone.mv_strZoneID, "진 타일도 Discard로 치워져야 한다");
        }

        public static void Effect_ResolveComparisonAndScore_TieGrantsNoPoints()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(4, 0, false, Ont.E_PlayerColor.Black)));
            new GT.Effect_CommitTileToPending().Apply(
                objContext, new DomainAction("Action_CommitTile", new Ont.ST_ActionData(4, 0, false, Ont.E_PlayerColor.White)));

            new GT.Effect_ResolveComparisonAndScore().Apply(objContext, new DomainAction("Action_ResolveRound", default));

            Assert.AreEqual(0, objContext.mv_lisPlayers[0].mv_nScore, "동점이면 Black도 점수를 얻지 못해야 한다");
            Assert.AreEqual(0, objContext.mv_lisPlayers[1].mv_nScore, "동점이면 White도 점수를 얻지 못해야 한다");
        }

        public static void DetermineWinner_OneAlwaysBeatsNine_AsTheSpecialException()
        {
            Assert.AreEqual((int)Ont.E_PlayerColor.Black, (int)GT.Effect_ResolveComparisonAndScore.DetermineWinner(1, 9)!.Value, "Black이 1, White가 9면 1을 낸 Black이 이겨야 한다(특수 규칙)");
            Assert.AreEqual((int)Ont.E_PlayerColor.White, (int)GT.Effect_ResolveComparisonAndScore.DetermineWinner(9, 1)!.Value, "Black이 9, White가 1이면 1을 낸 White가 이겨야 한다(특수 규칙)");
        }

        public static void DetermineWinner_OtherwiseHigherRankWins()
        {
            Assert.AreEqual((int)Ont.E_PlayerColor.White, (int)GT.Effect_ResolveComparisonAndScore.DetermineWinner(2, 8)!.Value, "1-9 예외가 아니면 그냥 숫자가 큰 쪽이 이겨야 한다");
            Assert.IsTrue(GT.Effect_ResolveComparisonAndScore.DetermineWinner(6, 6) is null, "같은 숫자는 무승부(null)여야 한다");
        }

        public static void Effect_CheckGuryongTuGameOver_EndsGame_WhenBothHandsEmpty()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();
            Ont.Zone objDiscard = objContext.mv_dicZones[GT.GuryongTuZoneId.Discard];
            foreach (Ont.Entity objTile in objContext.mv_lisEntities)
            {
                objTile.mv_objLocatedZone = objDiscard; // 9라운드가 전부 끝난 상태를 흉내낸다.
            }

            new GT.Effect_CheckGuryongTuGameOver().Apply(objContext, new DomainAction("Action_ResolveRound", default));

            Assert.IsTrue(objContext.mv_isGameOver, "양쪽 Hand가 모두 비면 종국 처리되어야 한다");
        }

        public static void Effect_CheckGuryongTuGameOver_DoesNotEndGame_WhenTilesRemain()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();

            new GT.Effect_CheckGuryongTuGameOver().Apply(objContext, new DomainAction("Action_ResolveRound", default));

            Assert.IsTrue(!objContext.mv_isGameOver, "타일이 남아있으면 종국되면 안 된다");
        }
    }
}
