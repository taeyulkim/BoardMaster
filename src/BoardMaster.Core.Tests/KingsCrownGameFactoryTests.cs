namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using KC = BoardMaster.Core.Rules.KingsCrown;

    internal static class KingsCrownGameFactoryTests
    {
        public static void CreateStandardGame_Registers25Zones()
        {
            Ont.GameContext objContext = KC.KingsCrownGameFactory.CreateStandardGame();

            Assert.AreEqual(25, objContext.mv_dicZones.Count, "5x5 반상은 25칸(가운데 포함)이어야 한다");
        }

        public static void CreateStandardGame_RegistersBothPlayers()
        {
            Ont.GameContext objContext = KC.KingsCrownGameFactory.CreateStandardGame();

            Assert.AreEqual(2, objContext.mv_lisPlayers.Count, "플레이어 2명이 등록되어야 한다");
            Assert.IsTrue(objContext.mv_lisPlayers.Exists(p => p.mv_eColor == Ont.E_PlayerColor.Black), "Black 플레이어가 있어야 한다");
            Assert.IsTrue(objContext.mv_lisPlayers.Exists(p => p.mv_eColor == Ont.E_PlayerColor.White), "White 플레이어가 있어야 한다");
        }

        public static void CreateStandardGame_ActiveColorStartsAsBlack()
        {
            Ont.GameContext objContext = KC.KingsCrownGameFactory.CreateStandardGame();

            Assert.AreEqual(Ont.E_PlayerColor.Black, objContext.mv_stCurrentState.m_eActiveColor, "Black이 선공으로 시작해야 한다");
        }

        public static void CreateStandardGame_NoEntitiesPlacedYet()
        {
            Ont.GameContext objContext = KC.KingsCrownGameFactory.CreateStandardGame();

            Assert.AreEqual(0, objContext.mv_lisEntities.Count, "숫자칩을 결합해 놓기 전까지는 왕관 Entity가 하나도 없어야 한다");
        }
    }
}
