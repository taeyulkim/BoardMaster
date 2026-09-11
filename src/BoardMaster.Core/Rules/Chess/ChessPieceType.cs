namespace BoardMaster.Core.Rules.Chess
{
    /// <summary>
    /// Entity.mv_strType에 담기는 기물 종류 문자열 상수입니다. War/GuryongTu의 ZoneId 상수화와
    /// 같은 이유(오타 방지)로 모아뒀습니다. War의 카드 랭크(숫자 비교만 하면 됨)와 달리 체스는
    /// UI가 기물 종류별로 다른 글리프를 그려야 하므로, 이 상수 목록은 public으로 둬서 WPF 클라이언트가
    /// (내부 구현인 ChessZoneId/ChessMoveGenerator는 건드리지 않고) 안전하게 참조할 수 있게 했습니다.
    /// </summary>
    public static class ChessPieceType
    {
        public const string King = "King";
        public const string Queen = "Queen";
        public const string Rook = "Rook";
        public const string Bishop = "Bishop";
        public const string Knight = "Knight";
        public const string Pawn = "Pawn";
    }
}
