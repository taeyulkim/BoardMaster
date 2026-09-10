namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// 하나의 행동을 완전히 서술하는 경량 불변 값 구조체입니다.
    /// 기보 이력(mv_lisHistory) 및 AI 시뮬레이션 롤아웃 전달용으로 힙 할당 없이 값 복사됩니다.
    /// </summary>
    public readonly struct ST_ActionData
    {
        public readonly int m_nX;
        public readonly int m_nY;
        public readonly bool m_isPass;
        public readonly E_PlayerColor m_eColor;

        public ST_ActionData(int p_nX, int p_nY, bool p_isPass, E_PlayerColor p_eColor)
        {
            m_nX = p_nX;
            m_nY = p_nY;
            m_isPass = p_isPass;
            m_eColor = p_eColor;
        }
    }
}
