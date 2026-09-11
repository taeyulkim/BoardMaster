namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using NK = BoardMaster.Core.Rules.NineKnights;

    /// <summary>
    /// Chess 쪽 ChessTestFixtures와 같은 목적입니다 — 빈 9x9 보드를 조립하고 원하는 자리에 원하는
    /// 번호의 기사를 직접 놓는 헬퍼입니다. NineKnightsGameFactory.CreateStandardGame()은 항상
    /// 무작위 배치라, 특정 국면(전투/미션/전멸 시나리오)을 만들어야 하는 테스트에는 안 맞습니다.
    /// </summary>
    internal static class NineKnightsTestFixtures
    {
        public static Ont.GameContext CreateEmptyBoardContext(Ont.E_PlayerColor p_eActiveColor = Ont.E_PlayerColor.Black)
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(1, p_eActiveColor, new int[1, 1], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            for (int nY = 0; nY < NK.NineKnightsGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < NK.NineKnightsGameFactory.BOARD_SIZE; nX++)
                {
                    string strZoneId = $"Sq_{nX}_{nY}";
                    objContext.mv_dicZones.Add(strZoneId, new Ont.Zone(strZoneId, nX, nY, Ont.E_VisibilityType.Public));
                }
            }

            objContext.mv_dicZones.Add("Reserve_Player1", new Ont.Zone("Reserve_Player1", -1, -1, Ont.E_VisibilityType.Hidden));
            objContext.mv_dicZones.Add("Reserve_Player2", new Ont.Zone("Reserve_Player2", -1, -1, Ont.E_VisibilityType.Hidden));
            objContext.mv_dicZones.Add("Captured", new Ont.Zone("Captured", -1, -1, Ont.E_VisibilityType.Public));

            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player1", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player2", Ont.E_PlayerColor.White));

            return objContext;
        }

        public static void PlacePiece(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nNumber, int p_nX, int p_nY)
        {
            string strEntityId = $"{p_eColor}_Knight_{p_nNumber}";
            Ont.Zone objZone = p_objContext.mv_dicZones[$"Sq_{p_nX}_{p_nY}"];
            p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, p_eColor, p_nNumber.ToString(), objZone));
        }

        public static void PlaceReserve(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nNumber)
        {
            string strEntityId = $"{p_eColor}_Knight_{p_nNumber}";
            string strReserveZoneId = p_eColor == Ont.E_PlayerColor.Black ? "Reserve_Player1" : "Reserve_Player2";
            Ont.Zone objZone = p_objContext.mv_dicZones[strReserveZoneId];
            p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, p_eColor, p_nNumber.ToString(), objZone));
        }
    }
}
