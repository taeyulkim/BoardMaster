namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    /// <summary>
    /// GoGameSession.GetLegalMoves()의 빠른 경로(GoLibertyCache 기반, O(1) 자충수 판정)가 브루트포스
    /// 기준(빈 칸마다 CreatePlaceStoneAction + Action.Validate로 직접 검증 — 기존 BFS 기반 Cond_NotSuicide를
    /// 그대로 거치는 실제 착수 검증 경로)과 항상 정확히 같은 결과를 내는지 무작위 대국으로 대조합니다.
    /// GoLibertyCache의 정확성에 대한 최종 안전장치입니다 — 여기서 하나라도 어긋나면 즉시 실패합니다.
    /// </summary>
    internal static class GoLibertyCacheDifferentialTests
    {
        public static void GetLegalMoves_MatchesBruteForceValidation_AcrossManyRandomGames()
        {
            const int BOARD_SIZE = 7;
            const int GAME_COUNT = 15;
            const int MAX_MOVES_PER_GAME = 200;

            Random objRandom = new Random(20260910);

            for (int nGame = 0; nGame < GAME_COUNT; nGame++)
            {
                Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: BOARD_SIZE, p_eActiveColor: Ont.E_PlayerColor.Black);
                Go.GoGameSession objSession = new Go.GoGameSession(objContext);

                int nMoveCount = 0;
                while (!objSession.mv_objCurrentContext.mv_isGameOver && nMoveCount < MAX_MOVES_PER_GAME)
                {
                    List<(int X, int Y)> lisFast = objSession.GetLegalMoves();
                    HashSet<(int X, int Y)> setFast = new HashSet<(int X, int Y)>(lisFast);
                    HashSet<(int X, int Y)> setBruteForce = BruteForceLegalMoves(objSession, BOARD_SIZE);

                    Assert.AreEqual(
                        setBruteForce.Count, setFast.Count,
                        $"게임 {nGame}, 수 {nMoveCount}: 빠른 경로(GoLibertyCache)와 브루트포스(BFS)의 합법수 개수가 달라야 할 이유가 없다");

                    foreach ((int X, int Y) stMove in setBruteForce)
                    {
                        Assert.IsTrue(
                            setFast.Contains(stMove),
                            $"게임 {nGame}, 수 {nMoveCount}: 브루트포스는 합법이라는데 빠른 경로가 놓친 좌표 ({stMove.X},{stMove.Y})");
                    }

                    if (lisFast.Count == 0)
                    {
                        objSession.Pass();
                    }
                    else
                    {
                        int nChoice = objRandom.Next(lisFast.Count + 1);
                        if (nChoice == lisFast.Count)
                        {
                            objSession.Pass();
                        }
                        else
                        {
                            (int X, int Y) = lisFast[nChoice];
                            objSession.PlayStone(X, Y);
                        }
                    }

                    nMoveCount++;
                }
            }
        }

        private static HashSet<(int X, int Y)> BruteForceLegalMoves(Go.GoGameSession p_objSession, int p_nBoardSize)
        {
            HashSet<(int X, int Y)> setLegal = new HashSet<(int X, int Y)>();
            Ont.E_PlayerColor eActiveColor = p_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            int[,] a_nGrid = p_objSession.mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;

            for (int nY = 0; nY < p_nBoardSize; nY++)
            {
                for (int nX = 0; nX < p_nBoardSize; nX++)
                {
                    if (a_nGrid[nX, nY] != (int)Ont.E_PlayerColor.None)
                    {
                        continue;
                    }

                    DomainAction objProbe = p_objSession.CreatePlaceStoneAction(nX, nY, eActiveColor);
                    if (objProbe.Validate(p_objSession.mv_objCurrentContext))
                    {
                        setLegal.Add((nX, nY));
                    }
                }
            }

            return setLegal;
        }
    }
}
