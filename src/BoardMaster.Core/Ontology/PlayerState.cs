namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// 대국에 참가하는 한 플레이어의 소유 자원 및 상태를 추적하는 도메인 클래스입니다.
    /// </summary>
    public sealed class PlayerState
    {
        public string mv_strPlayerID { get; set; }
        public E_PlayerColor mv_eColor { get; set; }
        public int mv_nPrisonerCount { get; set; }
        public int mv_nScore { get; set; }

        public PlayerState(string p_strPlayerID, E_PlayerColor p_eColor)
        {
            mv_strPlayerID = p_strPlayerID ?? throw new ArgumentNullException(nameof(p_strPlayerID));
            mv_eColor = p_eColor;
            mv_nPrisonerCount = 0;
            mv_nScore = 0;
        }
    }
}
