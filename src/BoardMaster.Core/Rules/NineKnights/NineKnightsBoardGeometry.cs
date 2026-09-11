namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 9x9 반상에서 "누구의 배치 줄/목표 줄이 몇 번째 랭크인가"를 한 곳에 모아둔 좌표 상수입니다.
    /// Player1(선공)은 아래쪽(y=0이 자기 뒷줄, y=2가 배치 줄)에서, Player2(후공)는 위쪽(y=8이
    /// 자기 뒷줄, y=6이 배치 줄)에서 마주보고 시작합니다.
    /// </summary>
    internal static class NineKnightsBoardGeometry
    {
        public static int DeploymentRow(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? 2 : NineKnightsGameFactory.BOARD_SIZE - 3;
        }

        /// <summary>이 색의 "자기 뒷줄"입니다 — 상대가 이 줄에 자기 미션 번호와 맞는 기사를 데려오면 그 상대가 승리합니다.</summary>
        public static int OwnBackRow(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? 0 : NineKnightsGameFactory.BOARD_SIZE - 1;
        }

        /// <summary>이 색이 승리하려면 도달해야 하는 "상대 뒷줄"입니다.</summary>
        public static int OpponentBackRow(Ont.E_PlayerColor p_eColor)
        {
            return OwnBackRow(Opponent(p_eColor));
        }

        public static Ont.E_PlayerColor Opponent(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;
        }
    }
}
