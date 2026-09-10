namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class Cond_NotKoRecaptureTests
    {
        public static void IsSatisfied_ReturnsFalse_WhenPlacingExactlyAtForbiddenPoint()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_NotKoRecapture objCondition = new Go.Cond_NotKoRecapture((2, 2));
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(!objCondition.IsSatisfied(objContext, objAction), "금지된 좌표에 두는 착수는 거부되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_WhenPlacingElsewhere()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_NotKoRecapture objCondition = new Go.Cond_NotKoRecapture((2, 2));
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "금지된 좌표가 아니면 허용되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_WhenNoForbiddenPointSet()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_NotKoRecapture objCondition = new Go.Cond_NotKoRecapture(null);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(2, 2, false, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "금지된 좌표가 없으면 항상 허용되어야 한다");
        }

        public static void IsSatisfied_ReturnsTrue_ForPass_EvenAtForbiddenCoordinates()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            Go.Cond_NotKoRecapture objCondition = new Go.Cond_NotKoRecapture((0, 0));
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));

            Assert.IsTrue(objCondition.IsSatisfied(objContext, objAction), "패스는 좌표와 무관하게 항상 허용되어야 한다");
        }
    }
}
