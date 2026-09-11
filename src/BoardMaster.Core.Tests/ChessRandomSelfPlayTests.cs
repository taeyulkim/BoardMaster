namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Chess = BoardMaster.Core.Rules.Chess;

    /// <summary>
    /// 20개 손으로 짠 규칙 테스트로는 체스처럼 기물별 이동 규칙이 복잡하게 얽힌 장르의 모든 국면
    /// 조합을 다 훑을 수 없습니다. 그래서 양쪽 다 무작위 합법수만 두는 자기 대국을 여러 판 돌려,
    /// 최소한의 불변식(King이 갑자기 사라지면 안 된다 = 체크 안전성 필터가 어딘가 새고 있다는 뜻,
    /// 예외 없이 끝까지 돌아간다, 총 기물 수는 절대 늘지 않는다)이 깨지지 않는지 확인하는 안전망입니다.
    /// GoLibertyCacheDifferentialTests와 같은 성격의 테스트입니다.
    /// </summary>
    internal static class ChessRandomSelfPlayTests
    {
        public static void RandomSelfPlay_NeverLosesAKing_AndTerminatesWithoutException_AcrossManyGames()
        {
            const int GAME_COUNT = 20;
            const int MAX_HALF_MOVES = 300;

            for (int nGame = 0; nGame < GAME_COUNT; nGame++)
            {
                Random objRandom = new Random(1000 + nGame);
                Chess.ChessGameSession objSession = new Chess.ChessGameSession(Chess.ChessGameFactory.CreateStandardGame());

                int nHalfMoveCount = 0;
                int nPreviousPieceCount = objSession.mv_objCurrentContext.mv_lisEntities.Count;

                while (objSession.CurrentPhaseName != "GameOver" && nHalfMoveCount < MAX_HALF_MOVES)
                {
                    Ont.E_PlayerColor eActiveColor = objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
                    (int FromX, int FromY, int ToX, int ToY)? stMove = PickRandomMove(objSession, eActiveColor, objRandom);

                    Assert.IsTrue(stMove is not null, $"게임 {nGame}, 수 {nHalfMoveCount}: MainPlay 페이즈인데 둘 수 있는 합법수가 없다면 모순이다");

                    objSession.MovePiece(stMove!.Value.FromX, stMove.Value.FromY, stMove.Value.ToX, stMove.Value.ToY);
                    nHalfMoveCount++;

                    int nCurrentPieceCount = objSession.mv_objCurrentContext.mv_lisEntities.Count;
                    Assert.AreEqual(nPreviousPieceCount, nCurrentPieceCount, $"게임 {nGame}, 수 {nHalfMoveCount}: 기물이 새로 생기거나 사라지면 안 된다(포획은 Captured Zone으로 이동일 뿐)");
                    nPreviousPieceCount = nCurrentPieceCount;

                    bool bWhiteKingAlive = HasKing(objSession, Ont.E_PlayerColor.White);
                    bool bBlackKingAlive = HasKing(objSession, Ont.E_PlayerColor.Black);
                    Assert.IsTrue(bWhiteKingAlive && bBlackKingAlive, $"게임 {nGame}, 수 {nHalfMoveCount}: King은 절대 포획되면 안 된다(체크 안전성 필터가 새고 있다는 신호)");
                }
            }
        }

        private static bool HasKing(Chess.ChessGameSession p_objSession, Ont.E_PlayerColor p_eColor)
        {
            for (int nY = 0; nY < Chess.ChessGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < Chess.ChessGameFactory.BOARD_SIZE; nX++)
                {
                    Ont.Entity? objPiece = p_objSession.GetPieceAt(nX, nY);
                    if (objPiece is not null && objPiece.mv_eColor == p_eColor && objPiece.mv_strType == Chess.ChessPieceType.King)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static (int FromX, int FromY, int ToX, int ToY)? PickRandomMove(Chess.ChessGameSession p_objSession, Ont.E_PlayerColor p_eColor, Random p_objRandom)
        {
            List<(int FromX, int FromY, int ToX, int ToY)> lisCandidates = new();

            for (int nY = 0; nY < Chess.ChessGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < Chess.ChessGameFactory.BOARD_SIZE; nX++)
                {
                    Ont.Entity? objPiece = p_objSession.GetPieceAt(nX, nY);
                    if (objPiece is null || objPiece.mv_eColor != p_eColor)
                    {
                        continue;
                    }

                    foreach (Chess.ChessMove stMove in p_objSession.GetLegalMoves(nX, nY))
                    {
                        lisCandidates.Add((nX, nY, stMove.ToX, stMove.ToY));
                    }
                }
            }

            if (lisCandidates.Count == 0)
            {
                return null;
            }

            return lisCandidates[p_objRandom.Next(lisCandidates.Count)];
        }
    }
}
