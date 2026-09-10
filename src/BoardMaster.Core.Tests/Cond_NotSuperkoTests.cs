namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class Cond_NotSuperkoTests
    {
        public static void IsSatisfied_ReturnsFalse_WhenResultingPositionAlreadyVisited()
        {
            // 빈 보드에서 Black이 (2,2)에 두면 나올 배치("Black 하나만 (2,2)")의 해시를 직접 계산해
            // 미리 이력에 넣어둔다.
            ulong ulCurrentHash = 0;
            ulong ulResultingHash = ulCurrentHash ^ Go.GoZobristTable.GetValue(2, 2, Ont.E_PlayerColor.Black);

            HashSet<ulong> setVisited = new HashSet<ulong> { ulResultingHash };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(ulCurrentHash, setVisited);

            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objCondition.IsSatisfied(objContext, objAction), "이 착수가 만들 배치가 이미 나온 적 있다면 거부되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_WhenResultingPositionIsNew()
        {
            ulong ulCurrentHash = 0;
            HashSet<ulong> setVisited = new HashSet<ulong>();
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(ulCurrentHash, setVisited);

            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "처음 나오는 배치는 허용되어야 한다");
        }

        public static void IsSatisfied_ComparesPostCaptureResult_NotRawPlacement()
        {
            // White(1,0)의 유일한 활로가 (0,0). Black이 (0,0)에 두면 White(1,0)이 사라진 배치가 된다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);

            ulong ulCurrentHash = Go.GoZobristTable.GetValue(1, 0, Ont.E_PlayerColor.White)
                ^ Go.GoZobristTable.GetValue(2, 0, Ont.E_PlayerColor.Black)
                ^ Go.GoZobristTable.GetValue(1, 1, Ont.E_PlayerColor.Black);

            // 포획을 반영한 "이후" 배치의 해시: Black(0,0) 추가 + White(1,0) 제거(같은 값을 한 번 더
            // XOR하면 토글되어 사라진다).
            ulong ulPostCaptureHash = ulCurrentHash
                ^ Go.GoZobristTable.GetValue(0, 0, Ont.E_PlayerColor.Black)
                ^ Go.GoZobristTable.GetValue(1, 0, Ont.E_PlayerColor.White);

            HashSet<ulong> setVisited = new HashSet<ulong> { ulPostCaptureHash };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(ulCurrentHash, setVisited);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(
                !objCondition.IsSatisfied(objContext, objAction),
                "포획을 반영한 결과 배치가 이력에 있으면 거부되어야 한다(착수 직후의 미포획 배치가 아니라)");
        }

        public static void IsSatisfied_ReturnsTrue_ForRawPlacementHash_WhenActualResultIsCaptureAdjusted()
        {
            // 위 테스트와 같은 국면이지만, 이력에는 "포획 반영 전" 배치(Black(0,0) + White(1,0) 둘 다
            // 있는 상태)의 해시를 넣어둔다. 실제 결과 배치(White(1,0) 제거됨)와는 다르므로 허용되어야
            // 한다 — Cond_NotSuperko가 포획을 정확히 반영해서 비교하는지 반대 방향으로도 확인한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);

            ulong ulCurrentHash = Go.GoZobristTable.GetValue(1, 0, Ont.E_PlayerColor.White)
                ^ Go.GoZobristTable.GetValue(2, 0, Ont.E_PlayerColor.Black)
                ^ Go.GoZobristTable.GetValue(1, 1, Ont.E_PlayerColor.Black);

            ulong ulRawPlacementHash = ulCurrentHash ^ Go.GoZobristTable.GetValue(0, 0, Ont.E_PlayerColor.Black);

            HashSet<ulong> setVisited = new HashSet<ulong> { ulRawPlacementHash };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(ulCurrentHash, setVisited);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(
                objCondition.IsSatisfied(objContext, objAction),
                "실제로는 나오지 않을 배치(포획 미반영)가 이력에 있을 뿐이므로 허용되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_ForPass_RegardlessOfHistory()
        {
            ulong ulCurrentHash = 123456789UL;
            HashSet<ulong> setVisited = new HashSet<ulong> { ulCurrentHash };
            Go.Cond_NotSuperko objCondition = new Go.Cond_NotSuperko(ulCurrentHash, setVisited);

            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "패스는 반상을 바꾸지 않으므로 이력과 무관하게 항상 허용되어야 한다");
        }
    }
}
