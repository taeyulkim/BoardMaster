namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Chess = BoardMaster.Core.Rules.Chess;
    using AiChess = BoardMaster.Core.AI.Chess;

    internal static class ChessMctsSearcherTests
    {
        public static void FindBestMove_ReturnsLegalMove_OnStandardOpening()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            AiChess.ChessMctsSearcher objSearcher = new AiChess.ChessMctsSearcher(p_nMaxRolloutMoves: 40);
            (int FromX, int FromY, int ToX, int ToY)? stMove = objSearcher.FindBestMove(objSession, 100, new Random(1));

            Assert.IsTrue(stMove.HasValue, "초기 배치에서는 탐색 결과가 반드시 있어야 한다");

            List<(int FromX, int FromY, int ToX, int ToY)> lisLegalMoves = objSession.GetAllLegalMoves(Ont.E_PlayerColor.White);
            Assert.IsTrue(lisLegalMoves.Contains(stMove!.Value), "탐색이 추천한 수는 실제 합법수 목록에 있어야 한다");
        }

        public static void FindBestMove_DoesNotMutateRootSession()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());
            Ont.GameContext objContextBeforeSearch = objSession.mv_objCurrentContext;

            AiChess.ChessMctsSearcher objSearcher = new AiChess.ChessMctsSearcher(p_nMaxRolloutMoves: 40);
            objSearcher.FindBestMove(objSession, 100, new Random(1));

            Assert.IsTrue(
                ReferenceEquals(objContextBeforeSearch, objSession.mv_objCurrentContext),
                "탐색은 루트 세션을 절대 건드리면 안 된다 — 항상 복제본 위에서만 이루어져야 한다");
        }

        public static void FindBestMove_ReturnsNull_WhenGameAlreadyOver()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());
            objSession.MovePiece(5, 1, 5, 2); // 1. f3
            objSession.MovePiece(4, 6, 4, 4); // 1... e5
            objSession.MovePiece(6, 1, 6, 3); // 2. g4
            objSession.MovePiece(3, 7, 7, 3); // 2... Qh4# (체크메이트)

            AiChess.ChessMctsSearcher objSearcher = new AiChess.ChessMctsSearcher();
            (int FromX, int FromY, int ToX, int ToY)? stMove = objSearcher.FindBestMove(objSession, 50, new Random(1));

            Assert.IsTrue(!stMove.HasValue, "이미 끝난 대국에서는 탐색할 것이 없으므로 null을 반환해야 한다");
        }

        public static void FindBestMove_PicksTheObviousCheckmatingMove()
        {
            // White Rook a1이 a8로 가면 f7/g7/h7 자기 폰에 갇힌 Black King(g8)을 그대로 백랭크
            // 메이트시킨다 — 이 국면에서 압도적으로 좋은(그리고 유일하게 "즉시 승리"인) 수다.
            Ont.GameContext objContext = ChessTestFixtures.CreateEmptyBoardContext();
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.King, 4, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.White, Chess.ChessPieceType.Rook, 0, 0);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.King, 6, 7);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 5, 6);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 6, 6);
            ChessTestFixtures.PlacePiece(objContext, Ont.E_PlayerColor.Black, Chess.ChessPieceType.Pawn, 7, 6);
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(objContext);

            AiChess.ChessMctsSearcher objSearcher = new AiChess.ChessMctsSearcher(p_nMaxRolloutMoves: 40);
            (int FromX, int FromY, int ToX, int ToY)? stMove = objSearcher.FindBestMove(objSession, 500, new Random(42));

            Assert.IsTrue(stMove.HasValue, "탐색 결과가 있어야 한다");
            Assert.AreEqual((0, 0, 0, 7), stMove!.Value, "즉시 체크메이트가 되는 Ra1-a8을 추천해야 한다");
        }

        public static void Search_TotalVisitCountAcrossCandidates_EqualsIterationCount()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            AiChess.ChessMctsSearcher objSearcher = new AiChess.ChessMctsSearcher(p_nMaxRolloutMoves: 30);
            AiChess.ChessMctsSearchResult objResult = objSearcher.Search(objSession, 150, new Random(3));

            int nTotalVisits = 0;
            foreach (AiChess.ChessMctsCandidateStat objCandidate in objResult.CandidateMoves)
            {
                nTotalVisits += objCandidate.VisitCount;
            }

            Assert.AreEqual(150, nTotalVisits, "모든 후보수의 방문 횟수 합은 반복 횟수와 같아야 한다");
        }

        public static void Search_BestMove_MatchesCandidateWithHighestVisitCount()
        {
            Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

            AiChess.ChessMctsSearcher objSearcher = new AiChess.ChessMctsSearcher(p_nMaxRolloutMoves: 30);
            AiChess.ChessMctsSearchResult objResult = objSearcher.Search(objSession, 150, new Random(3));

            Assert.IsTrue(objResult.BestMove.HasValue, "탐색 결과가 있어야 한다");
            Assert.IsTrue(objResult.CandidateMoves.Count > 0, "후보수 목록이 비어 있으면 안 된다");

            AiChess.ChessMctsCandidateStat objTopCandidate = objResult.CandidateMoves[0];
            Assert.AreEqual(objTopCandidate.Move, objResult.BestMove!.Value, "정렬된 후보 목록의 1등이 BestMove와 같아야 한다");

            foreach (AiChess.ChessMctsCandidateStat objCandidate in objResult.CandidateMoves)
            {
                Assert.IsTrue(objCandidate.VisitCount <= objTopCandidate.VisitCount, "1등 후보의 방문 횟수가 가장 많아야 한다");
            }
        }
    }
}
