namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using Chess = BoardMaster.Core.Rules.Chess;

    internal static class ChessGameSessionTests
    {
        public static void CurrentPhaseName_StartsAsMainPlay()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());
            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "시작 시점엔 MainPlay 페이즈여야 한다");
        }

        public static void MovePiece_MovesPawnForward_AndSwitchesTurn()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            Ont.GameContext objResult = objSession.MovePiece(4, 1, 4, 3); // e2-e4

            Assert.IsTrue(Chess.ChessZoneQuery.FindPieceAt(objResult, 4, 3) is not null, "e4에 폰이 있어야 한다");
            Assert.IsTrue(Chess.ChessZoneQuery.FindPieceAt(objResult, 4, 1) is null, "e2는 비어 있어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.Black, objResult.mv_stCurrentState.m_eActiveColor, "White가 두면 Black 차례로 넘어가야 한다");
        }

        public static void MovePiece_Throws_WhenMovingOpponentsPiece()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.MovePiece(4, 6, 4, 4), // White 차례에 Black 폰을 움직이려는 시도
                "자기 차례가 아닌 상대 기물을 움직이면 거부되어야 한다");
        }

        public static void MovePiece_Throws_ForIllegalMove()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.MovePiece(4, 1, 4, 5), // 폰이 네 칸을 가려는 시도
                "규칙에 없는 수는 거부되어야 한다");
        }

        public static void MovePiece_CastlesKingAndRookTogether()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 7, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.King, 4, 7);
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(objContext);

            Ont.GameContext objResult = objSession.MovePiece(4, 0, 6, 0);

            Assert.AreEqual(Chess.ChessPieceType.King, Chess.ChessZoneQuery.FindPieceAt(objResult, 6, 0)!.mv_strType, "King이 g1로 캐슬링해야 한다");
            Assert.AreEqual(Chess.ChessPieceType.Rook, Chess.ChessZoneQuery.FindPieceAt(objResult, 5, 0)!.mv_strType, "Rook도 f1로 함께 움직여야 한다");
            Assert.IsTrue(Chess.ChessZoneQuery.FindPieceAt(objResult, 7, 0) is null, "룩의 원래 자리(h1)는 비어야 한다");
        }

        public static void MovePiece_CapturesEnPassant_AcrossTwoMoves()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext(Ont.E_PlayerColor.Black);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.King, 4, 7);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 4); // e5
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 3, 6); // d7
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(objContext);

            objSession.MovePiece(3, 6, 3, 4); // Black d7-d5(더블스텝)
            Ont.GameContext objResult = objSession.MovePiece(4, 4, 3, 5); // White exd6 e.p.

            Assert.AreEqual(Chess.ChessPieceType.Pawn, Chess.ChessZoneQuery.FindPieceAt(objResult, 3, 5)!.mv_strType, "White 폰이 d6에 있어야 한다");
            Assert.IsTrue(Chess.ChessZoneQuery.FindPieceAt(objResult, 3, 4) is null, "앙파상으로 잡힌 Black 폰(d5)은 사라져야 한다");
        }

        public static void MovePiece_PromotesPawnToQueen_OnReachingLastRank()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.King, 4, 7);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 0, 6); // a7
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(objContext);

            Ont.GameContext objResult = objSession.MovePiece(0, 6, 0, 7); // a7-a8

            Assert.AreEqual(Chess.ChessPieceType.Queen, Chess.ChessZoneQuery.FindPieceAt(objResult, 0, 7)!.mv_strType, "마지막 랭크에 도달한 폰은 퀸으로 승진해야 한다");
        }

        public static void MovePiece_FoolsMate_EndsGameWithBlackWinning()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            objSession.MovePiece(5, 1, 5, 2); // 1. f3
            objSession.MovePiece(4, 6, 4, 4); // 1... e5
            objSession.MovePiece(6, 1, 6, 3); // 2. g4
            Ont.GameContext objResult = objSession.MovePiece(3, 7, 7, 3); // 2... Qh4#

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "체크메이트로 대국이 끝나야 한다");
            Assert.IsTrue(objResult.mv_isGameOver, "GameContext에도 종국 표시가 되어야 한다");

            Ont.PlayerState objBlack = objResult.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Ont.PlayerState objWhite = objResult.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;
            Assert.AreEqual(1, objBlack.mv_nScore, "체크메이트를 성사시킨 Black이 승자여야 한다");
            Assert.AreEqual(0, objWhite.mv_nScore, "White는 패배해야 한다");
        }

        public static void MovePiece_Throws_AfterGameHasEnded()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            objSession.MovePiece(5, 1, 5, 2);
            objSession.MovePiece(4, 6, 4, 4);
            objSession.MovePiece(6, 1, 6, 3);
            objSession.MovePiece(3, 7, 7, 3); // 체크메이트로 종국

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.MovePiece(0, 1, 0, 2),
                "종국된 대국에서 기물을 움직이려 하면 거부되어야 한다");
        }
    }
}
