namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 그레이트 킹덤의 반상 칸 상태입니다. Go는 칸 상태가 딱 3가지(빈 칸/Black/White)라
    /// Ont.E_PlayerColor를 그대로 격자 값으로 캐스팅해서 씁니다. 그레이트 킹덤은 "어느 진영도
    /// 아닌 중립 성"이라는 네 번째 상태가 있어서(시작 시 반상 정중앙에 하나 놓이고, 절대 잡히지
    /// 않으며, 누구의 영토 계산에도 도움이 안 되는 영구적인 벽), E_PlayerColor(값이 3개뿐)를
    /// 재사용하지 않고 이 장르 전용 정수 상수를 격자(ST_BoardState.m_a_nBoardGrid)에 직접 저장합니다.
    /// 공유 Ontology 계층에 이 장르만의 개념(중립 성)을 새지 않게 하려는 선택입니다(NFR-2).
    ///
    /// Player1(선공)/Player2(후공)는 턴 진행 자체는 Go처럼 Ont.E_PlayerColor(Black/White)로 추적하되,
    /// 격자에 쓰는 값만 이 상수를 쓰고 ToPlayerColor/FromPlayerColor로 서로 변환합니다.
    ///
    /// Chess의 ChessPieceType과 같은 이유로 public입니다 — WPF 클라이언트가 격자 값을 직접 읽어
    /// (Player1/Player2/Neutral을 각각 다른 색으로) 그려야 합니다.
    /// </summary>
    public static class GreatKingdomCell
    {
        public const int Empty = 0;
        public const int Player1 = 1; // 선공(파란 성), Ont.E_PlayerColor.Black에 대응
        public const int Player2 = 2; // 후공(주황 성), Ont.E_PlayerColor.White에 대응
        public const int Neutral = 3; // 중립 성 — 잡히지 않고, 누구의 그룹에도 속하지 않으며, 누구의 활로도 아니다.

        public static int FromPlayerColor(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? Player1 : Player2;
        }

        public static Ont.E_PlayerColor ToPlayerColor(int p_nCellValue)
        {
            return p_nCellValue == Player1 ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
        }

        public static int Opponent(int p_nCellValue)
        {
            return p_nCellValue == Player1 ? Player2 : Player1;
        }
    }
}
