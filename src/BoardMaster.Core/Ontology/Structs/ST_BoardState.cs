namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// AI의 MCTS 연산 시 가상 롤아웃을 빠르게 복제할 수 있도록 지원하는 경량 불변 보드 상태 구조체입니다.
    /// m_a_nBoardGrid는 참조 필드이므로, 진짜 격리가 필요한 복제 시점에는 호출자가 배열을 별도로 Clone()해야 합니다.
    /// </summary>
    public readonly struct ST_BoardState
    {
        public readonly int m_nTurnNumber;
        public readonly E_PlayerColor m_eActiveColor;
        public readonly int[,] m_a_nBoardGrid; // 19x19 or 9x9 Grid Representation
        public readonly int m_nBlackPrisoners;
        public readonly int m_nWhitePrisoners;

        public ST_BoardState(
            int p_nTurnNumber,
            E_PlayerColor p_eActiveColor,
            int[,] p_a_nBoardGrid,
            int p_nBlackPrisoners,
            int p_nWhitePrisoners)
        {
            m_nTurnNumber = p_nTurnNumber;
            m_eActiveColor = p_eActiveColor;
            m_a_nBoardGrid = p_a_nBoardGrid ?? throw new ArgumentNullException(nameof(p_a_nBoardGrid));
            m_nBlackPrisoners = p_nBlackPrisoners;
            m_nWhitePrisoners = p_nWhitePrisoners;
        }
    }
}
