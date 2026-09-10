namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class Cond_NotSuicideTests
    {
        public static void IsSatisfied_ReturnsFalse_ForPureSuicideWithNoCapture()
        {
            // 5x5 코너. Black이 (0,0)에 두면 이웃 (1,0),(0,1) 모두 White라 자기 활로 0개.
            // 두 White 돌 모두 다른 활로((2,0),(1,1)/(0,2),(1,1))를 갖고 있어 따내지지 않는다 -> 순수 자충수.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);

            Go.Cond_NotSuicide objCondition = new Go.Cond_NotSuicide();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objCondition.IsSatisfied(objContext, objAction), "포위되고 상대를 따낼 수도 없는 착수는 자충수로 거부되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_WhenPlacementHasOpenLiberties()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_NotSuicide objCondition = new Go.Cond_NotSuicide();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "빈 보드 중앙 착수는 활로가 충분하므로 합법수여야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_WhenPlacementCapturesAdjacentGroup()
        {
            // White(1,0)의 유일한 활로가 (0,0). White(0,1)은 다른 활로((0,2))가 남아 있어 따내지지 않는다.
            // Black이 (0,0)에 두면 자기 활로는 0개지만 White(1,0)을 활로 0으로 만들어 따낼 수 있다 -> 합법수.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);

            Go.Cond_NotSuicide objCondition = new Go.Cond_NotSuicide();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "자기 활로는 없어도 상대 그룹을 따낼 수 있는 착수는 합법수여야 한다");
        }

        public static void CalculateLiberties_DedupesSharedLiberty_ForLShapedGroup()
        {
            // L자 그룹: Black(1,1)-(2,1)-(1,2). (2,2)는 (2,1)과 (1,2) 양쪽 모두의 이웃이라
            // 중복 제거하지 않으면 8, 올바르게 제거하면 7이 나와야 한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 2, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 2, Ont.E_PlayerColor.Black);

            int[,] a_nGrid = objContext.mv_stCurrentState.m_a_nBoardGrid;
            Go.Cond_NotSuicide objCondition = new Go.Cond_NotSuicide();

            int nLiberties = objCondition.CalculateLiberties(
                a_nGrid, 5, 5, 1, 1, Ont.E_PlayerColor.Black, -1, -1, Ont.E_PlayerColor.None);

            Assert.AreEqual(7, nLiberties, "L자 그룹의 활로는 공유 좌표를 한 번만 세어 7이어야 한다");
        }

        public static void IsSatisfied_ReusesInstance_WithoutLeakingStateBetweenCalls()
        {
            Go.Cond_NotSuicide objCondition = new Go.Cond_NotSuicide();

            Ont.GameContext objSuicideContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objSuicideContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objSuicideContext, 0, 1, Ont.E_PlayerColor.White);
            DomainAction objSuicideAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            bool bFirstResult = objCondition.IsSatisfied(objSuicideContext, objSuicideAction);

            Ont.GameContext objOpenContext = TestFixtures.CreateContext(p_nSize: 5);
            DomainAction objOpenAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            bool bSecondResult = objCondition.IsSatisfied(objOpenContext, objOpenAction);

            Assert.IsTrue(!bFirstResult, "첫 호출(자충수)은 여전히 false여야 한다");
            Assert.IsTrue(bSecondResult, "동일 인스턴스를 재사용한 두 번째 호출(합법수)이 이전 호출 상태에 오염되면 안 된다");
        }
    }
}
