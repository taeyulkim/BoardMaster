namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using War = BoardMaster.Core.Rules.War;

    internal static class WarEffectsTests
    {
        private static Ont.GameContext CreateMinimalWarContext()
        {
            Ont.ST_BoardState stState = new Ont.ST_BoardState(1, Ont.E_PlayerColor.None, new int[1, 1], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stState);

            objContext.mv_dicZones.Add(War.WarZoneId.DeckBlack, new Ont.Zone(War.WarZoneId.DeckBlack, 0, 0, Ont.E_VisibilityType.Hidden));
            objContext.mv_dicZones.Add(War.WarZoneId.DeckWhite, new Ont.Zone(War.WarZoneId.DeckWhite, 0, 0, Ont.E_VisibilityType.Hidden));
            objContext.mv_dicZones.Add(War.WarZoneId.PileBlack, new Ont.Zone(War.WarZoneId.PileBlack, 0, 0, Ont.E_VisibilityType.Hidden));
            objContext.mv_dicZones.Add(War.WarZoneId.PileWhite, new Ont.Zone(War.WarZoneId.PileWhite, 0, 0, Ont.E_VisibilityType.Hidden));
            objContext.mv_dicZones.Add(War.WarZoneId.Table, new Ont.Zone(War.WarZoneId.Table, 0, 0, Ont.E_VisibilityType.Public));
            objContext.mv_dicZones.Add(War.WarZoneId.WarPool, new Ont.Zone(War.WarZoneId.WarPool, 0, 0, Ont.E_VisibilityType.Public));

            return objContext;
        }

        private static Ont.Entity AddCard(Ont.GameContext p_objContext, string p_strId, int p_nRank, Ont.E_PlayerColor p_eColor, string p_strZoneId)
        {
            Ont.Entity objCard = new Ont.Entity(p_strId, p_eColor, p_nRank.ToString(), p_objContext.mv_dicZones[p_strZoneId]);
            p_objContext.mv_lisEntities.Add(objCard);
            return objCard;
        }

        private static DomainAction DummyAction()
        {
            return new DomainAction("Action_PlayRound", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));
        }

        public static void Effect_DrawTopCards_MovesOneCardPerPlayer_ToTable()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            AddCard(objContext, "B1", 5, Ont.E_PlayerColor.Black, War.WarZoneId.DeckBlack);
            AddCard(objContext, "B2", 7, Ont.E_PlayerColor.Black, War.WarZoneId.DeckBlack);
            AddCard(objContext, "W1", 9, Ont.E_PlayerColor.White, War.WarZoneId.DeckWhite);

            War.Effect_DrawTopCards objEffect = new War.Effect_DrawTopCards(new Random(1));
            Ont.GameContext objResult = objEffect.Apply(objContext, DummyAction());

            List<Ont.Entity> lisTableCards = War.WarZoneQuery.FindEntitiesInZone(objResult, War.WarZoneId.Table);
            Assert.AreEqual(2, lisTableCards.Count, "양쪽 각각 한 장씩, 총 두 장이 Table로 와야 한다");
        }

        public static void Effect_DrawTopCards_RecyclesPile_WhenDeckIsEmpty()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            AddCard(objContext, "B1", 5, Ont.E_PlayerColor.Black, War.WarZoneId.PileBlack); // 덱은 비었고 Pile에만 있음
            AddCard(objContext, "W1", 9, Ont.E_PlayerColor.White, War.WarZoneId.DeckWhite);

            War.Effect_DrawTopCards objEffect = new War.Effect_DrawTopCards(new Random(1));
            Ont.GameContext objResult = objEffect.Apply(objContext, DummyAction());

            List<Ont.Entity> lisTableCards = War.WarZoneQuery.FindEntitiesInZone(objResult, War.WarZoneId.Table);
            Assert.IsTrue(lisTableCards.Exists(c => c.mv_strEntityID == "B1"), "Pile에서 재활용된 카드가 결국 Table까지 올라와야 한다");
        }

        public static void Effect_ResolveComparison_HigherRankWinsBothCards()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            Ont.Entity objBlackCard = AddCard(objContext, "B1", 10, Ont.E_PlayerColor.Black, War.WarZoneId.Table);
            Ont.Entity objWhiteCard = AddCard(objContext, "W1", 3, Ont.E_PlayerColor.White, War.WarZoneId.Table);

            War.Effect_ResolveComparison objEffect = new War.Effect_ResolveComparison();
            objEffect.Apply(objContext, DummyAction());

            Assert.AreEqual(War.WarZoneId.PileBlack, objBlackCard.mv_objLocatedZone.mv_strZoneID, "이긴 카드는 승자의 Pile로 가야 한다");
            Assert.AreEqual(War.WarZoneId.PileBlack, objWhiteCard.mv_objLocatedZone.mv_strZoneID, "진 카드도 승자의 Pile로 가야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.Black, objWhiteCard.mv_eColor, "진 카드의 소유권도 승자에게 넘어가야 한다");
        }

        public static void Effect_ResolveComparison_TieMovesCardsToWarPool()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            Ont.Entity objBlackCard = AddCard(objContext, "B1", 8, Ont.E_PlayerColor.Black, War.WarZoneId.Table);
            Ont.Entity objWhiteCard = AddCard(objContext, "W1", 8, Ont.E_PlayerColor.White, War.WarZoneId.Table);

            War.Effect_ResolveComparison objEffect = new War.Effect_ResolveComparison();
            objEffect.Apply(objContext, DummyAction());

            Assert.AreEqual(War.WarZoneId.WarPool, objBlackCard.mv_objLocatedZone.mv_strZoneID, "비기면 WarPool로 가야 한다");
            Assert.AreEqual(War.WarZoneId.WarPool, objWhiteCard.mv_objLocatedZone.mv_strZoneID, "비기면 WarPool로 가야 한다");
        }

        public static void Effect_ResolveComparison_WinnerAlsoClaimsAccumulatedWarPool()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            AddCard(objContext, "P1", 5, Ont.E_PlayerColor.None, War.WarZoneId.WarPool); // 이전 비김으로 쌓여있던 카드
            AddCard(objContext, "P2", 5, Ont.E_PlayerColor.None, War.WarZoneId.WarPool);
            AddCard(objContext, "B1", 10, Ont.E_PlayerColor.Black, War.WarZoneId.Table);
            AddCard(objContext, "W1", 3, Ont.E_PlayerColor.White, War.WarZoneId.Table);

            War.Effect_ResolveComparison objEffect = new War.Effect_ResolveComparison();
            Ont.GameContext objResult = objEffect.Apply(objContext, DummyAction());

            List<Ont.Entity> lisBlackPile = War.WarZoneQuery.FindEntitiesInZone(objResult, War.WarZoneId.PileBlack);
            Assert.AreEqual(4, lisBlackPile.Count, "이번 대결 카드 2장 + 쌓여있던 WarPool 카드 2장 = 4장을 전부 가져가야 한다");
        }

        public static void Effect_CheckWarGameOver_EndsGame_WhenOnePlayerHasNoDrawableCards()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            AddCard(objContext, "W1", 5, Ont.E_PlayerColor.White, War.WarZoneId.DeckWhite);
            // Black은 덱에도 Pile에도 카드가 없다.

            War.Effect_CheckWarGameOver objEffect = new War.Effect_CheckWarGameOver();
            Ont.GameContext objResult = objEffect.Apply(objContext, DummyAction());

            Assert.IsTrue(objResult.mv_isGameOver, "한쪽이 낼 카드가 전혀 없으면 종국 처리되어야 한다");
        }

        public static void Effect_CheckWarGameOver_DoesNotEndGame_WhenBothHaveCards()
        {
            Ont.GameContext objContext = CreateMinimalWarContext();
            AddCard(objContext, "B1", 5, Ont.E_PlayerColor.Black, War.WarZoneId.DeckBlack);
            AddCard(objContext, "W1", 5, Ont.E_PlayerColor.White, War.WarZoneId.DeckWhite);

            War.Effect_CheckWarGameOver objEffect = new War.Effect_CheckWarGameOver();
            Ont.GameContext objResult = objEffect.Apply(objContext, DummyAction());

            Assert.IsTrue(!objResult.mv_isGameOver, "양쪽 다 카드가 있으면 계속 진행되어야 한다");
        }
    }
}
