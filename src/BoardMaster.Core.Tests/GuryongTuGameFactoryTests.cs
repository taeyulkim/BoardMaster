namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using GT = BoardMaster.Core.Rules.GuryongTu;

    internal static class GuryongTuGameFactoryTests
    {
        public static void CreateStandardGame_Deals9TilesEachSide()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();

            Assert.AreEqual(18, objContext.mv_lisEntities.Count, "타일은 18개(양쪽 9개씩)여야 한다");

            int nBlackCount = 0;
            int nWhiteCount = 0;
            foreach (Ont.Entity objTile in objContext.mv_lisEntities)
            {
                if (objTile.mv_eColor == Ont.E_PlayerColor.Black) nBlackCount++;
                else if (objTile.mv_eColor == Ont.E_PlayerColor.White) nWhiteCount++;
            }

            Assert.AreEqual(9, nBlackCount, "Black은 9개를 받아야 한다");
            Assert.AreEqual(9, nWhiteCount, "White는 9개를 받아야 한다");
        }

        public static void CreateStandardGame_BothSidesHaveOneOfEachRankFrom1To9()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();

            for (int nRank = 1; nRank <= 9; nRank++)
            {
                int nBlackMatches = 0;
                int nWhiteMatches = 0;

                foreach (Ont.Entity objTile in objContext.mv_lisEntities)
                {
                    if (objTile.mv_strType != nRank.ToString())
                    {
                        continue;
                    }

                    if (objTile.mv_eColor == Ont.E_PlayerColor.Black) nBlackMatches++;
                    else if (objTile.mv_eColor == Ont.E_PlayerColor.White) nWhiteMatches++;
                }

                Assert.AreEqual(1, nBlackMatches, $"Black은 랭크 {nRank}를 정확히 하나 가져야 한다");
                Assert.AreEqual(1, nWhiteMatches, $"White는 랭크 {nRank}를 정확히 하나 가져야 한다");
            }
        }

        public static void CreateStandardGame_RegistersZonesWithCorrectVisibility()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();

            Assert.AreEqual(Ont.E_VisibilityType.Hidden, objContext.mv_dicZones[GT.GuryongTuZoneId.HandBlack].mv_eVisibility, "Hand는 Hidden이어야 한다");
            Assert.AreEqual(Ont.E_VisibilityType.Hidden, objContext.mv_dicZones[GT.GuryongTuZoneId.PendingWhite].mv_eVisibility, "Pending도 Hidden이어야 한다");
            Assert.AreEqual(Ont.E_VisibilityType.Public, objContext.mv_dicZones[GT.GuryongTuZoneId.Discard].mv_eVisibility, "Discard는 Public이어야 한다");
        }

        public static void CreateStandardGame_AllTilesStartInHand()
        {
            Ont.GameContext objContext = GT.GuryongTuGameFactory.CreateStandardGame();

            foreach (Ont.Entity objTile in objContext.mv_lisEntities)
            {
                bool bInAHand = objTile.mv_objLocatedZone.mv_strZoneID == GT.GuryongTuZoneId.HandBlack
                    || objTile.mv_objLocatedZone.mv_strZoneID == GT.GuryongTuZoneId.HandWhite;
                Assert.IsTrue(bInAHand, $"{objTile.mv_strEntityID}은 시작 시 Hand 안에 있어야 한다");
            }
        }
    }
}
