namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// War의 WarZoneQuery와 같은 이유(Entity → Zone 단방향 참조뿐이라 역으로 훑어야 함)로 존재하는
    /// 조회 헬퍼입니다. 타일이 18개뿐이라 전체 스캔 비용은 무시할 만합니다. War 것을 그대로
    /// 재사용하지 않고 이 장르 전용으로 다시 둔 것도 War와 같은 이유입니다 — 장르끼리 서로
    /// 몰라야(NFR-2) 나중에 한쪽만 바뀌어도 다른 쪽이 깨지지 않습니다.
    /// </summary>
    internal static class GuryongTuZoneQuery
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
    }
}
