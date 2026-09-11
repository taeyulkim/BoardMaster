namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Chess = BoardMaster.Core.Rules.Chess;

    /// <summary>
    /// 체스 테스트 전용으로 빈 8x8 보드를 조립하고 원하는 자리에 기물을 직접 놓는 헬퍼입니다.
    /// ChessGameFactory.CreateStandardGame()은 항상 표준 초기 배치라, 캐슬링/앙파상/체크메이트/
    /// 스테일메이트처럼 특정 국면을 만들어야 하는 테스트에는 맞지 않습니다.
    /// </summary>
    internal static class ChessTestFixtures
    {
        public static Ont.GameContext CreateEmptyBoardContext(Ont.E_PlayerColor p_eActiveColor = Ont.E_PlayerColor.White)
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(1, p_eActiveColor, new int[1, 1], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            for (int nY = 0; nY < Chess.ChessGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < Chess.ChessGameFactory.BOARD_SIZE; nX++)
                {
                    string strZoneId = Chess.ChessZoneId.Square(nX, nY);
                    objContext.mv_dicZones.Add(strZoneId, new Ont.Zone(strZoneId, nX, nY, Ont.E_VisibilityType.Public));
                }
            }

            objContext.mv_dicZones.Add(Chess.ChessZoneId.Captured, new Ont.Zone(Chess.ChessZoneId.Captured, -1, -1, Ont.E_VisibilityType.Public));

            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_White", Ont.E_PlayerColor.White));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_Black", Ont.E_PlayerColor.Black));

            return objContext;
        }

        public static void PlacePiece(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, string p_strType, int p_nX, int p_nY)
        {
            string strEntityId = $"{p_eColor}_{p_strType}_{p_nX}_{p_nY}";
            Ont.Zone objZone = p_objContext.mv_dicZones[Chess.ChessZoneId.Square(p_nX, p_nY)];
            p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, p_eColor, p_strType, objZone));
        }
    }
}
