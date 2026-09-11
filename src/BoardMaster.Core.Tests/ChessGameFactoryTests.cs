namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Chess = BoardMaster.Core.Rules.Chess;

    internal static class ChessGameFactoryTests
    {
        public static void CreateStandardGame_Has32Pieces_16EachSide()
        {
            Ont.GameContext objContext = Chess.ChessGameFactory.CreateStandardGame();

            Assert.AreEqual(32, objContext.mv_lisEntities.Count, "기물은 32개(양쪽 16개씩)여야 한다");

            int nWhiteCount = 0;
            int nBlackCount = 0;
            foreach (Ont.Entity objPiece in objContext.mv_lisEntities)
            {
                if (objPiece.mv_eColor == Ont.E_PlayerColor.White) nWhiteCount++;
                else if (objPiece.mv_eColor == Ont.E_PlayerColor.Black) nBlackCount++;
            }

            Assert.AreEqual(16, nWhiteCount, "White는 16개를 가져야 한다");
            Assert.AreEqual(16, nBlackCount, "Black은 16개를 가져야 한다");
        }

        public static void CreateStandardGame_PlacesBackRankAndPawnsCorrectly()
        {
            Ont.GameContext objContext = Chess.ChessGameFactory.CreateStandardGame();

            Assert.AreEqual(Chess.ChessPieceType.Rook, Chess.ChessZoneQuery.FindPieceAt(objContext, 0, 0)!.mv_strType, "a1은 Rook이어야 한다");
            Assert.AreEqual(Chess.ChessPieceType.Knight, Chess.ChessZoneQuery.FindPieceAt(objContext, 1, 0)!.mv_strType, "b1은 Knight여야 한다");
            Assert.AreEqual(Chess.ChessPieceType.Queen, Chess.ChessZoneQuery.FindPieceAt(objContext, 3, 0)!.mv_strType, "d1은 Queen이어야 한다");
            Assert.AreEqual(Chess.ChessPieceType.King, Chess.ChessZoneQuery.FindPieceAt(objContext, 4, 0)!.mv_strType, "e1은 King이어야 한다");
            Assert.AreEqual(Chess.ChessPieceType.King, Chess.ChessZoneQuery.FindPieceAt(objContext, 4, 7)!.mv_strType, "e8은 Black King이어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.Black, Chess.ChessZoneQuery.FindPieceAt(objContext, 4, 7)!.mv_eColor, "e8의 King은 Black이어야 한다");

            for (int nX = 0; nX < 8; nX++)
            {
                Assert.AreEqual(Chess.ChessPieceType.Pawn, Chess.ChessZoneQuery.FindPieceAt(objContext, nX, 1)!.mv_strType, $"랭크2 {nX}칸은 White Pawn이어야 한다");
                Assert.AreEqual(Chess.ChessPieceType.Pawn, Chess.ChessZoneQuery.FindPieceAt(objContext, nX, 6)!.mv_strType, $"랭크7 {nX}칸은 Black Pawn이어야 한다");
            }

            for (int nY = 2; nY <= 5; nY++)
            {
                for (int nX = 0; nX < 8; nX++)
                {
                    Assert.IsTrue(Chess.ChessZoneQuery.FindPieceAt(objContext, nX, nY) is null, $"({nX},{nY})는 중앙이라 비어 있어야 한다");
                }
            }
        }

        public static void CreateStandardGame_ActiveColorStartsAsWhite()
        {
            Ont.GameContext objContext = Chess.ChessGameFactory.CreateStandardGame();
            Assert.AreEqual(Ont.E_PlayerColor.White, objContext.mv_stCurrentState.m_eActiveColor, "체스는 White가 먼저 둔다");
        }
    }
}
