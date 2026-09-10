namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Go = BoardMaster.Core.Rules.Go;
    using AiGo = BoardMaster.Core.AI.Go;

    internal static class GoMctsSearcherTests
    {
        public static void FindBestMove_ReturnsLegalMove_OnEmptyBoard()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            AiGo.GoMctsSearcher objSearcher = new AiGo.GoMctsSearcher(p_nMaxRolloutMoves: 60);
            (int X, int Y, bool IsPass)? stMove = objSearcher.FindBestMove(objSession, 100, new Random(1));

            Assert.IsTrue(stMove.HasValue, "빈 보드에서는 탐색 결과가 반드시 있어야 한다(패스 포함)");

            if (!stMove!.Value.IsPass)
            {
                List<(int X, int Y)> lisLegalMoves = objSession.GetLegalMoves();
                Assert.IsTrue(lisLegalMoves.Contains((stMove.Value.X, stMove.Value.Y)), "탐색이 추천한 좌표는 실제 합법수 목록에 있어야 한다");
            }
        }

        public static void FindBestMove_DoesNotMutateRootSession()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);
            Ont.GameContext objContextBeforeSearch = objSession.mv_objCurrentContext;

            AiGo.GoMctsSearcher objSearcher = new AiGo.GoMctsSearcher(p_nMaxRolloutMoves: 60);
            objSearcher.FindBestMove(objSession, 100, new Random(1));

            Assert.IsTrue(
                ReferenceEquals(objContextBeforeSearch, objSession.mv_objCurrentContext),
                "탐색은 루트 세션을 절대 건드리면 안 된다 — 항상 복제본 위에서만 이루어져야 한다");
        }

        public static void FindBestMove_ReturnsNull_WhenGameAlreadyOver()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);
            objSession.Pass();
            objSession.Pass(); // 연속 패스로 종국

            AiGo.GoMctsSearcher objSearcher = new AiGo.GoMctsSearcher();
            (int X, int Y, bool IsPass)? stMove = objSearcher.FindBestMove(objSession, 50, new Random(1));

            Assert.IsTrue(!stMove.HasValue, "이미 끝난 대국에서는 탐색할 것이 없으므로 null을 반환해야 한다");
        }

        public static void FindBestMove_PicksTheObviousCapturingMove()
        {
            // White L자 그룹 (1,1)-(2,1)-(1,2)을 활로 1개(=2,2)만 남기고 포위한 국면.
            // Black이 (2,2)에 두면 White 3점을 통째로 따내는, 이 국면에서 압도적으로 좋은 수다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 1, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 1, 2, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 3, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 0, 2, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 3, Ont.E_PlayerColor.Black);
            // (2,2)만 비워둔다 — White L자 그룹의 유일한 남은 활로.

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            AiGo.GoMctsSearcher objSearcher = new AiGo.GoMctsSearcher(p_nMaxRolloutMoves: 80);
            (int X, int Y, bool IsPass)? stMove = objSearcher.FindBestMove(objSession, 800, new Random(42));

            Assert.IsTrue(stMove.HasValue, "탐색 결과가 있어야 한다");
            Assert.IsTrue(!stMove!.Value.IsPass, "White 3점을 통째로 잡을 수 있는데 패스를 추천하면 안 된다");
            Assert.AreEqual(2, stMove.Value.X, "White 그룹을 따내는 (2,2)를 추천해야 한다");
            Assert.AreEqual(2, stMove.Value.Y, "White 그룹을 따내는 (2,2)를 추천해야 한다");
        }

        public static void Search_TotalVisitCountAcrossCandidates_EqualsIterationCount()
        {
            // 루트에서 매 반복(iteration)은 정확히 하나의 루트 자식을 거쳐 역전파되므로, 모든 후보의
            // 방문 횟수 합은 항상 반복 횟수와 같아야 한다 — MCTS 구현이 맞는지 확인하는 불변식이다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            AiGo.GoMctsSearcher objSearcher = new AiGo.GoMctsSearcher(p_nMaxRolloutMoves: 60);
            AiGo.GoMctsSearchResult objResult = objSearcher.Search(objSession, 150, new Random(3));

            int nTotalVisits = 0;
            foreach (AiGo.GoMctsCandidateStat objCandidate in objResult.CandidateMoves)
            {
                nTotalVisits += objCandidate.VisitCount;
            }

            Assert.AreEqual(150, nTotalVisits, "모든 후보수의 방문 횟수 합은 반복 횟수와 같아야 한다");
        }

        public static void Search_BestMove_MatchesCandidateWithHighestVisitCount()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            AiGo.GoMctsSearcher objSearcher = new AiGo.GoMctsSearcher(p_nMaxRolloutMoves: 60);
            AiGo.GoMctsSearchResult objResult = objSearcher.Search(objSession, 150, new Random(3));

            Assert.IsTrue(objResult.BestMove.HasValue, "탐색 결과가 있어야 한다");
            Assert.IsTrue(objResult.CandidateMoves.Count > 0, "후보수 목록이 비어 있으면 안 된다");

            AiGo.GoMctsCandidateStat objTopCandidate = objResult.CandidateMoves[0];
            Assert.AreEqual(objTopCandidate.Move, objResult.BestMove!.Value, "정렬된 후보 목록의 1등이 BestMove와 같아야 한다");

            foreach (AiGo.GoMctsCandidateStat objCandidate in objResult.CandidateMoves)
            {
                Assert.IsTrue(objCandidate.VisitCount <= objTopCandidate.VisitCount, "1등 후보의 방문 횟수가 가장 많아야 한다");
            }
        }
    }
}
