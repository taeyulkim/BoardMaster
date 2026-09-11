namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using GK = BoardMaster.Core.Rules.GreatKingdom;

    /// <summary>
    /// GoLibertyCacheDifferentialTests/ChessRandomSelfPlayTests와 같은 성격의 안전망입니다.
    /// 영토 판정 + 즉시 포위 승리라는, 손으로 짠 시나리오만으로는 다 훑기 어려운 규칙 조합을
    /// 무작위 자기 대국으로 여러 판 돌려 최소한의 불변식이 깨지지 않는지 확인합니다: 예외 없이
    /// 끝까지 돌고, 대국이 끝나면 항상 정확히 한쪽만 승자(mv_nScore == 1)여야 합니다.
    /// </summary>
    internal static class GreatKingdomRandomSelfPlayTests
    {
        public static void RandomSelfPlay_AlwaysEndsWithExactlyOneWinner_AcrossManyGames()
        {
            const int GAME_COUNT = 20;
            const int MAX_MOVES = 300;
            const double PASS_PROBABILITY = 0.05; // 계속 두기만 하면 끝나지 않는 대국이 나올 수 있어 약간의 패스 확률을 섞는다.

            for (int nGame = 0; nGame < GAME_COUNT; nGame++)
            {
                Random objRandom = new Random(2000 + nGame);
                GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

                int nMoveCount = 0;
                while (objSession.CurrentPhaseName != "GameOver" && nMoveCount < MAX_MOVES)
                {
                    List<(int X, int Y)> lisLegalMoves = objSession.GetLegalMoves();

                    if (lisLegalMoves.Count == 0 || objRandom.NextDouble() < PASS_PROBABILITY)
                    {
                        objSession.Pass();
                    }
                    else
                    {
                        (int X, int Y) = lisLegalMoves[objRandom.Next(lisLegalMoves.Count)];
                        objSession.PlaceStone(X, Y);
                    }

                    nMoveCount++;
                }

                if (objSession.CurrentPhaseName == "GameOver")
                {
                    int nPlayer1Score = objSession.mv_objCurrentContext.mv_lisPlayers[0].mv_nScore;
                    int nPlayer2Score = objSession.mv_objCurrentContext.mv_lisPlayers[1].mv_nScore;

                    Assert.IsTrue(
                        (nPlayer1Score == 1 && nPlayer2Score == 0) || (nPlayer1Score == 0 && nPlayer2Score == 1),
                        $"게임 {nGame}: 종국되면 정확히 한쪽만 승자여야 한다(무승부 없음). Player1={nPlayer1Score}, Player2={nPlayer2Score}");
                }
            }
        }
    }
}
