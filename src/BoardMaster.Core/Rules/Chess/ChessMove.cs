namespace BoardMaster.Core.Rules.Chess
{
    /// <summary>
    /// 하나의 합법수와 그 부가 효과(포획/캐슬링/앙파상/승진 여부)를 함께 담는 값입니다.
    /// ChessGameSession.GetLegalMoves가 UI에 그대로 반환합니다 — UI는 이 플래그들을 보고
    /// "이 칸을 클릭하면 캐슬링이 된다" 같은 걸 사용자에게 미리 알려줄 수 있습니다.
    /// </summary>
    public readonly struct ChessMove
    {
        public readonly int ToX;
        public readonly int ToY;
        public readonly bool IsCapture;
        public readonly bool IsEnPassantCapture;
        public readonly bool IsCastleKingside;
        public readonly bool IsCastleQueenside;
        public readonly bool IsPromotion;

        public ChessMove(
            int p_nToX, int p_nToY, bool p_bIsCapture, bool p_bIsEnPassantCapture,
            bool p_bIsCastleKingside, bool p_bIsCastleQueenside, bool p_bIsPromotion)
        {
            ToX = p_nToX;
            ToY = p_nToY;
            IsCapture = p_bIsCapture;
            IsEnPassantCapture = p_bIsEnPassantCapture;
            IsCastleKingside = p_bIsCastleKingside;
            IsCastleQueenside = p_bIsCastleQueenside;
            IsPromotion = p_bIsPromotion;
        }
    }
}
