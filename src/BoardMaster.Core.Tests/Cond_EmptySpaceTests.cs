namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class Cond_EmptySpaceTests
    {
        public static void IsSatisfied_ReturnsTrue_ForEmptyCell()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_EmptySpace objCondition = new Go.Cond_EmptySpace();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "빈 칸은 통과해야 한다");
        }

        public static void IsSatisfied_ReturnsFalse_ForOccupiedCell()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 2, 2, Ont.E_PlayerColor.White);

            Go.Cond_EmptySpace objCondition = new Go.Cond_EmptySpace();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objCondition.IsSatisfied(objContext, objAction), "이미 돌이 있는 칸은 거부되어야 한다");
        }

        public static void IsSatisfied_ReturnsFalse_ForOutOfBoundsCell()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_EmptySpace objCondition = new Go.Cond_EmptySpace();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(-1, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objCondition.IsSatisfied(objContext, objAction), "보드 범위 밖 좌표는 거부되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_ForPass_RegardlessOfCoordinates()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_EmptySpace objCondition = new Go.Cond_EmptySpace();
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(99, 99, true, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "패스는 좌표와 무관하게 항상 통과해야 한다");
        }
    }
}
