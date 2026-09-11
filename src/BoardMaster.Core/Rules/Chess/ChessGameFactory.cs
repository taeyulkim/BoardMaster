namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 표준 체스 초기 국면을 만드는 팩토리입니다. 8x8=64개의 칸 Zone(각각 실제 좌표를 가짐)과
    /// 잡힌 기물을 모아두는 Captured Zone을 등록하고, 32개 기물을 표준 배치대로 놓습니다.
    ///
    /// x=0..7(파일 a..h), y=0..7(랭크 1..8). White는 y=0,1(랭크 1,2)에서, Black은 y=6,7(랭크 7,8)에서
    /// 시작합니다 — 실제 체스판과 같은 방향입니다.
    /// </summary>
    public static class ChessGameFactory
    {
        public const int BOARD_SIZE = 8;

        private static readonly string[] s_a_strBackRank =
        {
            ChessPieceType.Rook, ChessPieceType.Knight, ChessPieceType.Bishop, ChessPieceType.Queen,
            ChessPieceType.King, ChessPieceType.Bishop, ChessPieceType.Knight, ChessPieceType.Rook
        };

        public static Ont.GameContext CreateStandardGame()
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(
                1, Ont.E_PlayerColor.White, new int[1, 1], 0, 0);

            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            RegisterZones(objContext);
            RegisterPlayers(objContext);
            PlacePieces(objContext);

            return objContext;
        }

        private static void RegisterZones(Ont.GameContext p_objContext)
        {
            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    string strZoneId = ChessZoneId.Square(nX, nY);
                    p_objContext.mv_dicZones.Add(strZoneId, new Ont.Zone(strZoneId, nX, nY, Ont.E_VisibilityType.Public));
                }
            }

            p_objContext.mv_dicZones.Add(ChessZoneId.Captured, new Ont.Zone(ChessZoneId.Captured, -1, -1, Ont.E_VisibilityType.Public));
        }

        private static void RegisterPlayers(Ont.GameContext p_objContext)
        {
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_White", Ont.E_PlayerColor.White));
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_Black", Ont.E_PlayerColor.Black));
        }

        private static void PlacePieces(Ont.GameContext p_objContext)
        {
            for (int nX = 0; nX < BOARD_SIZE; nX++)
            {
                AddPiece(p_objContext, Ont.E_PlayerColor.White, s_a_strBackRank[nX], nX, 0);
                AddPiece(p_objContext, Ont.E_PlayerColor.White, ChessPieceType.Pawn, nX, 1);
                AddPiece(p_objContext, Ont.E_PlayerColor.Black, ChessPieceType.Pawn, nX, 6);
                AddPiece(p_objContext, Ont.E_PlayerColor.Black, s_a_strBackRank[nX], nX, 7);
            }
        }

        private static void AddPiece(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, string p_strType, int p_nX, int p_nY)
        {
            string strEntityId = $"{p_eColor}_{p_strType}_{p_nX}_{p_nY}";
            Ont.Zone objZone = p_objContext.mv_dicZones[ChessZoneId.Square(p_nX, p_nY)];
            p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, p_eColor, p_strType, objZone));
        }
    }
}
