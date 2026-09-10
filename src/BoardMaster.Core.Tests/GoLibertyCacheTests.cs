namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoLibertyCacheTests
    {
        public static void IsNonSuicidePlacement_ReturnsTrue_WhenEmptyNeighborExists()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.GoLibertyCache objCache = new Go.GoLibertyCache();
            objCache.Rebuild(objContext.mv_stCurrentState.m_a_nBoardGrid, 5, 5);

            bool bResult = objCache.IsNonSuicidePlacement(
                objContext.mv_stCurrentState.m_a_nBoardGrid, 5, 5, 2, 2, Ont.E_PlayerColor.Black);

            Assert.IsTrue(bResult, "빈 보드 한복판은 빈 이웃이 있으므로 자충수가 아니어야 한다");
        }

        public static void IsNonSuicidePlacement_ReturnsFalse_ForPureSuicideWithNoCapture()
        {
            // (0,0)을 White 세 점(1,0)(0,1)로 완전히 둘러싸고, 그 White들은 각자 다른 활로를 가진 상태.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.White); // White들을 한 그룹으로 묶어 활로를 늘려준다(2,0)/(1,2) 등

            int[,] a_nGrid = objContext.mv_stCurrentState.m_a_nBoardGrid;
            Go.GoLibertyCache objCache = new Go.GoLibertyCache();
            objCache.Rebuild(a_nGrid, 5, 5);

            bool bResult = objCache.IsNonSuicidePlacement(a_nGrid, 5, 5, 0, 0, Ont.E_PlayerColor.Black);

            Assert.IsTrue(!bResult, "따낼 수도 없고 활로도 없는 자충수는 false여야 한다");
        }

        public static void IsNonSuicidePlacement_ReturnsTrue_WhenPlacementCapturesAdjacentGroup()
        {
            // White(1,0)의 유일한 활로가 (0,0) — Black이 (0,0)에 두면 자기 활로는 0이지만 White를 따내 합법.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.Black);

            int[,] a_nGrid = objContext.mv_stCurrentState.m_a_nBoardGrid;
            Go.GoLibertyCache objCache = new Go.GoLibertyCache();
            objCache.Rebuild(a_nGrid, 5, 5);

            bool bResult = objCache.IsNonSuicidePlacement(a_nGrid, 5, 5, 0, 0, Ont.E_PlayerColor.Black);

            Assert.IsTrue(bResult, "인접 상대 그룹을 활로 0으로 만들어 따내는 수는 자충수가 아니어야 한다");
        }

        public static void IsNonSuicidePlacement_ReturnsTrue_ViaFriendlyGroupLiberty_EvenWhenNoNeighborCellIsDirectlyEmpty()
        {
            // Q=(2,1)의 네 이웃((1,1)=아군 Black, (3,1)/(2,0)/(2,2)=White)이 전부 점유되어 있어
            // "이웃 칸이 비어 있는가"만 보는 순진한 검사라면 자충수로 오판한다. 하지만 (1,1)이 속한
            // Black 그룹((1,1)-(1,2))은 Q 말고도 (0,1)/(1,0)/(0,2)/(1,3)이라는 다른 활로를 이미
            // 갖고 있으므로, 그룹 활로 "개수"(5, Q 포함)로 판정하면 정확히 합법으로 나와야 한다.
            // White 이웃 셋은 모두 자기 활로가 남아 있어(단수 아님) 상대를 따내서 합법이 되는 경우가
            // 아니라는 것도 함께 보장한다 — 오직 아군 그룹의 잉여 활로 때문에 합법인 경우만 격리한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 2, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 3, 1, Ont.E_PlayerColor.White); // 자기 활로(4,1) 등이 남아 단수 아님
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.White); // 자기 활로(1,0)/(3,0) 남아 단수 아님
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.White); // 자기 활로(3,2)/(2,3) 남아 단수 아님

            int[,] a_nGrid = objContext.mv_stCurrentState.m_a_nBoardGrid;
            Go.GoLibertyCache objCache = new Go.GoLibertyCache();
            objCache.Rebuild(a_nGrid, 5, 5);

            bool bResult = objCache.IsNonSuicidePlacement(a_nGrid, 5, 5, 2, 1, Ont.E_PlayerColor.Black);

            Assert.IsTrue(bResult, "이웃 칸이 전부 막혀 있어도 아군 그룹에 다른 활로가 남아 있으면 합법이어야 한다");
        }
    }
}
