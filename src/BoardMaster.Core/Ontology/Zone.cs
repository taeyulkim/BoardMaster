namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// 기물이 위치할 수 있는 논리적/물리적 공간 노드입니다.
    /// mv_lisAdjacentZoneIDs를 통해 인접 그래프(Topology)를 구성합니다.
    /// </summary>
    public sealed class Zone
    {
        public string mv_strZoneID { get; set; }
        public int mv_nX { get; set; }
        public int mv_nY { get; set; }
        public E_VisibilityType mv_eVisibility { get; set; }
        public List<string> mv_lisAdjacentZoneIDs { get; set; }

        public Zone(string p_strZoneID, int p_nX, int p_nY, E_VisibilityType p_eVisibility = E_VisibilityType.Public)
        {
            mv_strZoneID = p_strZoneID ?? throw new ArgumentNullException(nameof(p_strZoneID));
            mv_nX = p_nX;
            mv_nY = p_nY;
            mv_eVisibility = p_eVisibility;
            mv_lisAdjacentZoneIDs = new List<string>();
        }
    }
}
