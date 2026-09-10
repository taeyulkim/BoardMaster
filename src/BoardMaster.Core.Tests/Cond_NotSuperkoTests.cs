namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class Cond_NotSuperkoTests
    {
        public static void IsSatisfied_ReturnsFalse_WhenResultingPositionAlreadyVisited()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);

            int[,] a_nExpectedResultGrid = new int[5, 5];
            a_nExpectedResultGrid[2, 2] = (int)Ont.E_PlayerColor.Black;
            string strAlreadyVisitedKey = Go.GoBoardPositionKey.Compute(a_nExpectedResultGrid, 5, 5);

            HashSet<string> setVisited = new HashSet<string> { strAlreadyVisitedKey };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(setVisited);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objCondition.IsSatisfied(objContext, objAction), "이 착수가 만들 배치가 이미 나온 적 있다면 거부되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_WhenResultingPositionIsNew()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            HashSet<string> setVisited = new HashSet<string>();
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(setVisited);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "처음 나오는 배치는 허용되어야 한다");
        }

        public static void IsSatisfied_ComparesPostCaptureResult_NotRawPlacement()
        {
            // White(1,0)의 유일한 활로가 (0,0). Black이 (0,0)에 두면 White(1,0)이 사라진 배치가 된다.
            // "따낸 뒤"의 배치를 미리 이력에 넣어 두면, 이 조건이 단순히 착수 좌표만 비교하는 게 아니라
            // 포획까지 반영한 진짜 결과 배치를 기준으로 비교하는지 확인할 수 있다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);

            int[,] a_nExpectedResultGrid = new int[5, 5];
            a_nExpectedResultGrid[0, 0] = (int)Ont.E_PlayerColor.Black; // 착수
            a_nExpectedResultGrid[2, 0] = (int)Ont.E_PlayerColor.Black;
            a_nExpectedResultGrid[1, 1] = (int)Ont.E_PlayerColor.Black;
            // White(1,0)은 따내져서 결과 배치엔 없다 — 0(None)으로 남겨둔다.
            string strAlreadyVisitedKey = Go.GoBoardPositionKey.Compute(a_nExpectedResultGrid, 5, 5);

            HashSet<string> setVisited = new HashSet<string> { strAlreadyVisitedKey };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(setVisited);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(
                !objCondition.IsSatisfied(objContext, objAction),
                "포획을 반영한 결과 배치가 이력에 있으면 거부되어야 한다(착수 직후의 미포획 배치가 아니라)");
        }

        public static void IsSatisfied_ReturnsTrue_ForPass_RegardlessOfHistory()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            HashSet<string> setVisited = new HashSet<string> { GoBoardPositionKeyOfEmptyBoard() };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(setVisited);
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "패스는 반상을 바꾸지 않으므로 이력과 무관하게 항상 허용되어야 한다");
        }

        private static string GoBoardPositionKeyOfEmptyBoard()
        {
            return Go.GoBoardPositionKey.Compute(new int[5, 5], 5, 5);
        }
    }
}
