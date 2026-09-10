namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using War = BoardMaster.Core.Rules.War;

    internal static class WarGameFactoryTests
    {
        public static void CreateStandardGame_Deals52Cards_26EachSide()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(1));

            Assert.AreEqual(52, objContext.mv_lisEntities.Count, "카드는 52장이어야 한다");

            int nBlackCount = 0;
            int nWhiteCount = 0;
            foreach (Ont.Entity objCard in objContext.mv_lisEntities)
            {
                if (objCard.mv_eColor == Ont.E_PlayerColor.Black)
                {
                    nBlackCount++;
                }
                else if (objCard.mv_eColor == Ont.E_PlayerColor.White)
                {
                    nWhiteCount++;
                }
            }

            Assert.AreEqual(26, nBlackCount, "Black은 26장을 받아야 한다");
            Assert.AreEqual(26, nWhiteCount, "White는 26장을 받아야 한다");
        }

        public static void CreateStandardGame_RegistersZonesWithCorrectVisibility()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(1));

            Assert.AreEqual(Ont.E_VisibilityType.Hidden, objContext.mv_dicZones[War.WarZoneId.DeckBlack].mv_eVisibility, "덱은 Hidden이어야 한다");
            Assert.AreEqual(Ont.E_VisibilityType.Hidden, objContext.mv_dicZones[War.WarZoneId.PileWhite].mv_eVisibility, "Pile도 Hidden이어야 한다");
            Assert.AreEqual(Ont.E_VisibilityType.Public, objContext.mv_dicZones[War.WarZoneId.Table].mv_eVisibility, "Table은 Public이어야 한다");
            Assert.AreEqual(Ont.E_VisibilityType.Public, objContext.mv_dicZones[War.WarZoneId.WarPool].mv_eVisibility, "WarPool도 Public이어야 한다");
        }

        public static void CreateStandardGame_AllCardsStartInDecks()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(1));

            foreach (Ont.Entity objCard in objContext.mv_lisEntities)
            {
                bool bInADeck = objCard.mv_objLocatedZone.mv_strZoneID == War.WarZoneId.DeckBlack
                    || objCard.mv_objLocatedZone.mv_strZoneID == War.WarZoneId.DeckWhite;
                Assert.IsTrue(bInADeck, $"{objCard.mv_strEntityID}은 시작 시 덱 안에 있어야 한다");
            }
        }
    }
}
