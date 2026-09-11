namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using GK = BoardMaster.Core.Rules.GreatKingdom;
    using AiGk = BoardMaster.Core.AI.GreatKingdom;

    internal static class GreatKingdomMctsSearcherTests
    {
        public static void FindBestMove_ReturnsLegalMove_OnStandardOpening()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            AiGk.GreatKingdomMctsSearcher objSearcher = new AiGk.GreatKingdomMctsSearcher(p_nMaxRolloutMoves: 60);
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
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());
            Ont.GameContext objContextBeforeSearch = objSession.mv_objCurrentContext;

            AiGk.GreatKingdomMctsSearcher objSearcher = new AiGk.GreatKingdomMctsSearcher(p_nMaxRolloutMoves: 60);
            objSearcher.FindBestMove(objSession, 100, new Random(1));

            Assert.IsTrue(
                ReferenceEquals(objContextBeforeSearch, objSession.mv_objCurrentContext),
                "탐색은 루트 세션을 절대 건드리면 안 된다");
        }

        public static void FindBestMove_ReturnsNull_WhenGameAlreadyOver()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());
            objSession.Pass();
            objSession.Pass();

            AiGk.GreatKingdomMctsSearcher objSearcher = new AiGk.GreatKingdomMctsSearcher();
            (int X, int Y, bool IsPass)? stMove = objSearcher.FindBestMove(objSession, 50, new Random(1));

            Assert.IsTrue(!stMove.HasValue, "이미 끝난 대국에서는 탐색할 것이 없으므로 null을 반환해야 한다");
        }

        public static void FindBestMove_PicksTheObviousSiegeWinningMove()
        {
            // White L자 그룹 (1,1)-(2,1)-(1,2)을 활로 1개(=2,2)만 남기고 포위한 국면. Black이 (2,2)에
            // 두면 그 자리에서 즉시 대국을 끝내며 승리한다 — 이 국면에서 유일하게 "확정 승리"인 수다.
            // 일부러 작은 5x5 보드를 쓴다 — 9x9라면 빈 칸이 훨씬 많아서(후보 수가 많아져) 같은
            // 반복 횟수로는 이 수 하나에 충분히 방문이 쏠리지 않을 수 있다(Go 쪽 동일 테스트도
            // 5x5를 쓰는 이유와 같다).
            Ont.GameContext objContext = GreatKingdomTestFixtures.CreateContext(5, Ont.E_PlayerColor.Black);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 1, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 1, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 2, GK.GreatKingdomCell.Player2);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 1, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 3, 1, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 2, 0, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 0, 2, GK.GreatKingdomCell.Player1);
            GreatKingdomTestFixtures.SetCell(objContext, 1, 3, GK.GreatKingdomCell.Player1);
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(objContext);

            AiGk.GreatKingdomMctsSearcher objSearcher = new AiGk.GreatKingdomMctsSearcher(p_nMaxRolloutMoves: 60);
            (int X, int Y, bool IsPass)? stMove = objSearcher.FindBestMove(objSession, 500, new Random(42));

            Assert.IsTrue(stMove.HasValue, "탐색 결과가 있어야 한다");
            Assert.IsTrue(!stMove!.Value.IsPass, "그 자리에서 이기는 수가 있는데 패스를 추천하면 안 된다");
            Assert.AreEqual(2, stMove.Value.X, "White 그룹을 포위해 즉시 승리하는 (2,2)를 추천해야 한다");
            Assert.AreEqual(2, stMove.Value.Y, "White 그룹을 포위해 즉시 승리하는 (2,2)를 추천해야 한다");
        }

        public static void Search_TotalVisitCountAcrossCandidates_EqualsIterationCount()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            AiGk.GreatKingdomMctsSearcher objSearcher = new AiGk.GreatKingdomMctsSearcher(p_nMaxRolloutMoves: 40);
            AiGk.GreatKingdomMctsSearchResult objResult = objSearcher.Search(objSession, 150, new Random(3));

            int nTotalVisits = 0;
            foreach (AiGk.GreatKingdomMctsCandidateStat objCandidate in objResult.CandidateMoves)
            {
                nTotalVisits += objCandidate.VisitCount;
            }

            Assert.AreEqual(150, nTotalVisits, "모든 후보수의 방문 횟수 합은 반복 횟수와 같아야 한다");
        }

        public static void Search_BestMove_MatchesCandidateWithHighestVisitCount()
        {
            GK.GreatKingdomGameSession objSession = new GK.GreatKingdomGameSession(GK.GreatKingdomGameFactory.CreateStandardGame());

            AiGk.GreatKingdomMctsSearcher objSearcher = new AiGk.GreatKingdomMctsSearcher(p_nMaxRolloutMoves: 40);
            AiGk.GreatKingdomMctsSearchResult objResult = objSearcher.Search(objSession, 150, new Random(3));

            Assert.IsTrue(objResult.BestMove.HasValue, "탐색 결과가 있어야 한다");
            Assert.IsTrue(objResult.CandidateMoves.Count > 0, "후보수 목록이 비어 있으면 안 된다");

            AiGk.GreatKingdomMctsCandidateStat objTopCandidate = objResult.CandidateMoves[0];
            Assert.AreEqual(objTopCandidate.Move, objResult.BestMove!.Value, "정렬된 후보 목록의 1등이 BestMove와 같아야 한다");

            foreach (AiGk.GreatKingdomMctsCandidateStat objCandidate in objResult.CandidateMoves)
            {
                Assert.IsTrue(objCandidate.VisitCount <= objTopCandidate.VisitCount, "1등 후보의 방문 횟수가 가장 많아야 한다");
            }
        }
    }
}
