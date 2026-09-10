namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;

    internal static class GameContextTests
    {
        public static void Clone_DeepCopiesEntities_AndRewiresThemToClonedZones()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            Ont.Zone objZone = new Ont.Zone("Z1", 0, 0, Ont.E_VisibilityType.Public);
            objContext.mv_dicZones.Add("Z1", objZone);
            Ont.Entity objEntity = new Ont.Entity("E1", Ont.E_PlayerColor.Black, "Token", objZone);
            objContext.mv_lisEntities.Add(objEntity);

            Ont.GameContext objClone = objContext.Clone();

            Assert.AreEqual(1, objClone.mv_lisEntities.Count, "엔티티 개수가 복제되어야 한다");

            Ont.Entity objClonedEntity = objClone.mv_lisEntities[0];
            Assert.IsTrue(!ReferenceEquals(objEntity, objClonedEntity), "엔티티는 별개의 인스턴스여야 한다");
            Assert.IsTrue(
                !ReferenceEquals(objZone, objClonedEntity.mv_objLocatedZone),
                "복제된 엔티티는 원본이 아니라 복제된 Zone 인스턴스를 가리켜야 한다");
            Assert.AreEqual("Z1", objClonedEntity.mv_objLocatedZone.mv_strZoneID, "가리키는 Zone의 ID는 원본과 같아야 한다");

            Ont.Zone objOtherZone = new Ont.Zone("Z2", 0, 0, Ont.E_VisibilityType.Public);
            objClonedEntity.mv_objLocatedZone = objOtherZone;
            Assert.AreEqual("Z1", objEntity.mv_objLocatedZone.mv_strZoneID, "복제본의 변경이 원본 엔티티에 영향을 주면 안 된다");
        }
    }
}
