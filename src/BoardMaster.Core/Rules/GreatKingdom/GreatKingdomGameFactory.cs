namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 표준 그레이트 킹덤 초기 국면을 만드는 팩토리입니다: 9x9 빈 반상 정중앙에 중립 성 하나를
    /// 놓고 시작합니다. 선공(Player1, 파란 성)은 Ont.E_PlayerColor.Black에, 후공(Player2, 주황 성)은
    /// White에 대응시켜 Go/War/GuryongTu/Chess와 같은 턴 추적 방식을 그대로 씁니다.
    /// </summary>
    public static class GreatKingdomGameFactory
    {
        public const int BOARD_SIZE = 9;

        public static Ont.GameContext CreateStandardGame()
        {
            int[,] a_nGrid = new int[BOARD_SIZE, BOARD_SIZE];
            int nCenter = BOARD_SIZE / 2;
            a_nGrid[nCenter, nCenter] = GreatKingdomCell.Neutral;

            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(1, Ont.E_PlayerColor.Black, a_nGrid, 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player1_Blue", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player2_Orange", Ont.E_PlayerColor.White));

            return objContext;
        }
    }
}
