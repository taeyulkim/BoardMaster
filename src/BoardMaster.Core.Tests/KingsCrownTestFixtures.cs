namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using KC = BoardMaster.Core.Rules.KingsCrown;

    /// <summary>KingsCrownGameFactory.CreateStandardGame()은 이미 빈 5x5 반상이지만 항상 Black
    /// 선공으로 고정되어 있습니다. White 차례 시나리오도 테스트해야 하므로, 이 픽스처는 활성 색을
    /// 고를 수 있는 빈 반상 생성 헬퍼와, 특정 국면을 조립하기 위해 원하는 자리에 왕관을 직접
    /// 놓는(합법성 검사를 거치지 않는) 헬퍼를 제공합니다.</summary>
    internal static class KingsCrownTestFixtures
    {
        public static Ont.GameContext CreateEmptyBoardContext(Ont.E_PlayerColor p_eActiveColor = Ont.E_PlayerColor.Black)
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(1, p_eActiveColor, new int[1, 1], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            for (int nY = 0; nY < KC.KingsCrownGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < KC.KingsCrownGameFactory.BOARD_SIZE; nX++)
                {
                    string strZoneId = $"Sq_{nX}_{nY}";
                    objContext.mv_dicZones.Add(strZoneId, new Ont.Zone(strZoneId, nX, nY, Ont.E_VisibilityType.Public));
                }
            }

            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player1", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player2", Ont.E_PlayerColor.White));

            return objContext;
        }

        public static void PlaceCrown(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nNumber, int p_nX, int p_nY)
        {
            string strEntityId = $"{p_eColor}_Crown_{p_nX}_{p_nY}";
            Ont.Zone objZone = p_objContext.mv_dicZones[$"Sq_{p_nX}_{p_nY}"];
            p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, p_eColor, p_nNumber.ToString(), objZone));
        }
    }
}
