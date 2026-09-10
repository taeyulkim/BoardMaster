namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// 조작 및 이동이 가능한 게임 구성물입니다. 항상 정확히 하나의 Zone에 위치합니다(isLocatedIn).
    /// </summary>
    public sealed class Entity
    {
        public string mv_strEntityID { get; set; }
        public E_PlayerColor mv_eColor { get; set; }
        public string mv_strType { get; set; } // e.g., "Stone", "Meele"
        public Zone mv_objLocatedZone { get; set; }

        public Entity(string p_strEntityID, E_PlayerColor p_eColor, string p_strType, Zone p_objLocatedZone)
        {
            mv_strEntityID = p_strEntityID ?? throw new ArgumentNullException(nameof(p_strEntityID));
            mv_eColor = p_eColor;
            mv_strType = p_strType ?? throw new ArgumentNullException(nameof(p_strType));
            mv_objLocatedZone = p_objLocatedZone ?? throw new ArgumentNullException(nameof(p_objLocatedZone));
        }
    }
}
