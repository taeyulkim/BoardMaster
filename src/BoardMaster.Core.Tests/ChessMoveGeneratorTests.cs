namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Chess = BoardMaster.Core.Rules.Chess;

    internal static class ChessMoveGeneratorTests
    {
        private static readonly Chess.ChessCastlingRights s_stNoRights = Chess.ChessCastlingRights.CreateInitial();

        public static void GetLegalMoves_Pawn_CanStepOneOrTwo_FromStartRank()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 1);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 1, s_stNoRights, null);

            Assert.AreEqual(2, lisMoves.Count, "시작 랭크의 폰은 한 칸/두 칸 전진 모두 가능해야 한다");
        }

        public static void GetLegalMoves_Pawn_CannotDoubleStep_WhenIntermediateSquareBlocked()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 1);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Knight, 4, 2); // e3을 막는다

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 1, s_stNoRights, null);

            Assert.AreEqual(0, lisMoves.Count, "한 칸 앞이 막히면 두 칸은커녕 한 칸도 못 나간다");
        }

        public static void GetLegalMoves_Pawn_CapturesDiagonally_ButNotStraightAhead()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 4);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 5, 5);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 3, 5);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 4, s_stNoRights, null);

            Assert.AreEqual(3, lisMoves.Count, "직진 1칸 + 대각선 포획 2개 = 3수여야 한다");
            Assert.IsTrue(lisMoves.Exists(m => m.ToX == 5 && m.ToY == 5 && m.IsCapture), "대각선의 적 기물을 포획할 수 있어야 한다");
        }

        public static void GetLegalMoves_Pawn_CannotCaptureStraightAhead_EvenIfEnemyThere()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 4);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 4, 5);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 4, s_stNoRights, null);

            Assert.AreEqual(0, lisMoves.Count, "바로 앞에 적이 있으면 직진도 포획도 불가능해야 한다");
        }

        public static void GetLegalMoves_Pawn_CanCaptureEnPassant_WhenTargetMatches()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 4);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 3, 4);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 4, s_stNoRights, (3, 5));

            Assert.IsTrue(lisMoves.Exists(m => m.ToX == 3 && m.ToY == 5 && m.IsEnPassantCapture), "앙파상 대상 칸으로의 대각선 이동이 후보에 있어야 한다");
        }

        public static void GetLegalMoves_Knight_JumpsOverPieces_AndCanCaptureButNotOwnPiece()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Knight, 4, 4);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 5); // 나이트를 둘러싼 자기 편(뛰어넘어야 함)
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 6, 5); // 나이트가 갈 수 있는 칸의 적

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 4, s_stNoRights, null);

            Assert.AreEqual(8, lisMoves.Count, "가로막는 기물이 있어도 나이트는 8칸 전부 갈 수 있어야 한다");
            Assert.IsTrue(lisMoves.Exists(m => m.ToX == 6 && m.ToY == 5 && m.IsCapture), "적이 있는 칸은 포획으로 표시되어야 한다");
        }

        public static void GetLegalMoves_Rook_BlockedByOwnPiece_CannotPassThrough()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 7, 7);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 0, 3);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 0, 0, s_stNoRights, null);

            Assert.IsTrue(lisMoves.Exists(m => m.ToX == 0 && m.ToY == 2), "막고 있는 자기 편 바로 앞까지는 갈 수 있어야 한다");
            Assert.IsTrue(!lisMoves.Exists(m => m.ToX == 0 && m.ToY == 3), "자기 편이 있는 칸으로는 갈 수 없다");
            Assert.IsTrue(!lisMoves.Exists(m => m.ToX == 0 && m.ToY == 4), "자기 편에 막혀 그 너머로는 갈 수 없다");
        }

        public static void GetLegalMoves_Rook_CapturesEnemy_ButStopsThere()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 7, 7);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 0, 3);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 0, 0, s_stNoRights, null);

            Assert.IsTrue(lisMoves.Exists(m => m.ToX == 0 && m.ToY == 3 && m.IsCapture), "적을 포획할 수 있어야 한다");
            Assert.IsTrue(!lisMoves.Exists(m => m.ToX == 0 && m.ToY == 4), "포획한 칸 너머로는 갈 수 없다");
        }

        public static void IsSquareAttacked_DetectsPawnDiagonalAttack_NotStraightAhead()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 4, 4);

            Assert.IsTrue(Chess.ChessMoveGenerator.IsSquareAttacked(objContext, 5, 5, Ont.E_PlayerColor.White), "폰은 대각선 앞칸을 위협해야 한다");
            Assert.IsTrue(Chess.ChessMoveGenerator.IsSquareAttacked(objContext, 3, 5, Ont.E_PlayerColor.White), "반대편 대각선도 위협해야 한다");
            Assert.IsTrue(!Chess.ChessMoveGenerator.IsSquareAttacked(objContext, 4, 5, Ont.E_PlayerColor.White), "폰은 바로 앞칸은 위협하지 않는다(이동만 가능)");
        }

        public static void GetLegalMoves_King_CanCastleKingside_WhenPathClearAndSafe()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 7, 0);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 0, s_stNoRights, null);

            Assert.IsTrue(lisMoves.Exists(m => m.ToX == 6 && m.ToY == 0 && m.IsCastleKingside), "경로가 비어있고 안전하면 캐슬링이 후보에 있어야 한다");
        }

        public static void GetLegalMoves_King_CannotCastle_WhenPathBlocked()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 7, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Knight, 5, 0);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 0, s_stNoRights, null);

            Assert.IsTrue(!lisMoves.Exists(m => m.IsCastleKingside), "King과 Rook 사이가 막혀 있으면 캐슬링할 수 없다");
        }

        public static void GetLegalMoves_King_CannotCastle_WhenRookAlreadyMoved()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 7, 0);
            Chess.ChessCastlingRights stRookMoved = new Chess.ChessCastlingRights(false, true, false, false, false, false);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 0, stRookMoved, null);

            Assert.IsTrue(!lisMoves.Exists(m => m.IsCastleKingside), "그 룩이 이미 움직였으면 캐슬링할 수 없다");
        }

        public static void GetLegalMoves_King_CannotCastle_ThroughAttackedSquare()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 7, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Rook, 5, 7); // f-파일 전체를 위협

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 0, s_stNoRights, null);

            Assert.IsTrue(!lisMoves.Exists(m => m.IsCastleKingside), "King이 지나가는 칸이 공격받고 있으면 캐슬링할 수 없다");
        }

        public static void GetLegalMoves_PinnedPiece_CannotMoveOffThePinLine()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Bishop, 4, 1);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Rook, 4, 7);

            List<Chess.ChessMove> lisMoves = Chess.ChessMoveGenerator.GetLegalMoves(objContext, 4, 1, s_stNoRights, null);

            Assert.AreEqual(0, lisMoves.Count, "e-파일에 핀 잡힌 비숍은 대각선으로 못 벗어나(핀 라인 이탈 자체가 불가능) 합법수가 없어야 한다");
        }

        public static void HasAnyLegalMove_ReturnsFalse_ForBackRankCheckmate()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 6, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 5, 1);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 6, 1);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Pawn, 7, 1);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Rook, 0, 0);

            Assert.IsTrue(Chess.ChessMoveGenerator.IsInCheck(objContext, Ont.E_PlayerColor.White), "룩이 랭크1 전체를 훑어 King을 체크해야 한다");
            Assert.IsTrue(!Chess.ChessMoveGenerator.HasAnyLegalMove(objContext, Ont.E_PlayerColor.White, s_stNoRights, null), "자기 폰들에 갇혀 King이 도망갈 곳이 없으므로 체크메이트여야 한다");
        }

        public static void HasAnyLegalMove_ReturnsFalse_ButNotInCheck_ForStalemate()
        {
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.King, 0, 7);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 1, 5);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Queen, 2, 6);

            Assert.IsTrue(!Chess.ChessMoveGenerator.IsInCheck(objContext, Ont.E_PlayerColor.Black), "체크 상태가 아니어야 스테일메이트다");
            Assert.IsTrue(!Chess.ChessMoveGenerator.HasAnyLegalMove(objContext, Ont.E_PlayerColor.Black, s_stNoRights, null), "King이 갈 수 있는 세 칸이 전부 막히거나 위협받아 합법수가 없어야 한다");
        }
    }
}
