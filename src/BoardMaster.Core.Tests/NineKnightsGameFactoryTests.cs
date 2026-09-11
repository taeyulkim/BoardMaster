namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using NK = BoardMaster.Core.Rules.NineKnights;

    internal static class NineKnightsGameFactoryTests
    {
        public static void CreateStandardGame_EachPlayerHasNineUniqueNumbers()
        {
            Ont.GameContext objContext = NK.NineKnightsGameFactory.CreateStandardGame(new Random(1));

            for (int nPlayerIndex = 0; nPlayerIndex < 2; nPlayerIndex++)
            {
                Ont.E_PlayerColor eColor = nPlayerIndex == 0 ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
                HashSet<int> setNumbers = new HashSet<int>();

                foreach (Ont.Entity objPiece in objContext.mv_lisEntities)
                {
                    if (objPiece.mv_eColor == eColor)
                    {
                        setNumbers.Add(int.Parse(objPiece.mv_strType));
                    }
                }

                Assert.AreEqual(9, setNumbers.Count, $"{eColor}는 1~9가 정확히 한 번씩, 9개 있어야 한다");
                for (int n = 1; n <= 9; n++)
                {
                    Assert.IsTrue(setNumbers.Contains(n), $"{eColor}는 번호 {n}을 가진 기사가 있어야 한다");
                }
            }
        }

        public static void CreateStandardGame_SixDeployed_ThreeInReserve_PerPlayer()
        {
            Ont.GameContext objContext = NK.NineKnightsGameFactory.CreateStandardGame(new Random(2));

            int nBlackOnBoard = NK.NineKnightsZoneQuery.FindActivePiecesOnBoard(objContext, Ont.E_PlayerColor.Black).Count;
            int nBlackReserve = NK.NineKnightsZoneQuery.FindReservePieces(objContext, Ont.E_PlayerColor.Black).Count;
            int nWhiteOnBoard = NK.NineKnightsZoneQuery.FindActivePiecesOnBoard(objContext, Ont.E_PlayerColor.White).Count;
            int nWhiteReserve = NK.NineKnightsZoneQuery.FindReservePieces(objContext, Ont.E_PlayerColor.White).Count;

            Assert.AreEqual(6, nBlackOnBoard, "Black은 6명이 반상에 배치되어야 한다");
            Assert.AreEqual(3, nBlackReserve, "Black은 3명이 예비로 남아야 한다");
            Assert.AreEqual(6, nWhiteOnBoard, "White는 6명이 반상에 배치되어야 한다");
            Assert.AreEqual(3, nWhiteReserve, "White는 3명이 예비로 남아야 한다");
        }

        public static void CreateStandardGame_DeploysOnCorrectRow_ForEachSide()
        {
            Ont.GameContext objContext = NK.NineKnightsGameFactory.CreateStandardGame(new Random(3));

            foreach (Ont.Entity objPiece in objContext.mv_lisEntities)
            {
                if (objPiece.mv_objLocatedZone.mv_nX < 0)
                {
                    continue; // 예비 기물은 좌표가 없다.
                }

                int nExpectedRow = NK.NineKnightsBoardGeometry.DeploymentRow(objPiece.mv_eColor);
                Assert.AreEqual(nExpectedRow, objPiece.mv_objLocatedZone.mv_nY, $"{objPiece.mv_strEntityID}는 자기 배치 줄(y={nExpectedRow})에 있어야 한다");
            }
        }

        public static void CreateStandardGame_ActiveColorStartsAsBlack()
        {
            Ont.GameContext objContext = NK.NineKnightsGameFactory.CreateStandardGame(new Random(4));
            Assert.AreEqual(Ont.E_PlayerColor.Black, objContext.mv_stCurrentState.m_eActiveColor, "Player1(Black)이 먼저 둔다");
        }
    }
}
