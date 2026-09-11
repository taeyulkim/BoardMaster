namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using GK = BoardMaster.Core.Rules.GreatKingdom;

    /// <summary>
    /// Go 쪽 TestFixtures와 같은 목적입니다 — 특정 국면(영토/포위/자충수 시나리오)을 직접 조립하기
    /// 위한 최소 GameContext 헬퍼입니다. 격자 값은 Ont.E_PlayerColor가 아니라 GreatKingdomCell의
    /// 정수 상수를 그대로 씁니다.
    /// </summary>
    internal static class GreatKingdomTestFixtures
    {
        public static Ont.GameContext CreateContext(int p_nSize, Ont.E_PlayerColor p_eActiveColor = Ont.E_PlayerColor.Black)
        {
            Ont.ST_BoardState stState = new Ont.ST_BoardState(1, p_eActiveColor, new int[p_nSize, p_nSize], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stState);

            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player1_Blue", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player2_Orange", Ont.E_PlayerColor.White));

            return objContext;
        }

        public static void SetCell(Ont.GameContext p_objContext, int p_nX, int p_nY, int p_nCellValue)
        {
            p_objContext.mv_stCurrentState.m_a_nBoardGrid[p_nX, p_nY] = p_nCellValue;
        }
    }
}
