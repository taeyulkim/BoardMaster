namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// GameContext.mv_lisEntities를 훑어 특정 Zone에 있는 Entity를 찾는 조회 헬퍼입니다. Zone 자체는
    /// 자신이 담고 있는 Entity 목록을 들고 있지 않으므로(정적 설계서 원안 그대로 — Entity가
    /// mv_objLocatedZone으로 Zone을 가리키기만 할 뿐, 역방향 참조는 없습니다), War의 여러 Effect가
    /// 공통으로 이렇게 전체를 훑어 찾습니다. 카드가 52장뿐이라 매번 전체 스캔해도 비용이 무시할
    /// 만합니다 — 바둑의 반상 스캔과 달리 zero-alloc을 강제할 이유가 없습니다.
    /// </summary>
    internal static class WarZoneQuery
    {
        public static List<Ont.Entity> FindEntitiesInZone(Ont.GameContext p_objContext, string p_strZoneId)
        {
            List<Ont.Entity> lisResult = new List<Ont.Entity>();

            foreach (Ont.Entity objEntity in p_objContext.mv_lisEntities)
            {
                if (objEntity.mv_objLocatedZone.mv_strZoneID == p_strZoneId)
                {
                    lisResult.Add(objEntity);
                }
            }

            return lisResult;
        }

        public static int CountEntitiesOwnedBy(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            int nCount = 0;

            foreach (Ont.Entity objEntity in p_objContext.mv_lisEntities)
            {
                if (objEntity.mv_eColor == p_eColor)
                {
                    nCount++;
                }
            }

            return nCount;
        }
    }
}
