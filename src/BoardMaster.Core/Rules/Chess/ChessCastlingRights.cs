namespace BoardMaster.Core.Rules.Chess
{
    /// <summary>
    /// 캐슬링(Castling) 가능 여부를 추적하는 불변 값입니다. GameContext에는 이런 장르 전용 파생
    /// 상태를 두지 않는 원칙(Go의 m_stForbiddenKoPoint와 같은 이유)에 따라 ChessGameSession이
    /// 이 값을 들고 있다가, 매 수를 둘 때마다 Cond_LegalChessMove/Effect_CheckChessGameOver에
    /// 생성자로 주입합니다.
    ///
    /// King이 한 번이라도 움직이면 그 색의 캐슬링은 영구히 불가능해집니다. 룩은 "자기 원래 자리에서
    /// 벗어난 적이 있는지"만 추적하면 충분합니다 — 그 룩이 잡혔거나 다른 곳으로 움직였다면 원래
    /// 자리에 더 이상 룩이 없을 것이므로(Cond_LegalChessMove가 "그 자리에 자기 편 룩이 실제로
    /// 있는지"도 별도로 확인합니다), 이 플래그와 실제 반상 상태를 함께 봐야 정확합니다.
    /// </summary>
    internal readonly struct ChessCastlingRights
    {
        public readonly bool WhiteKingMoved;
        public readonly bool WhiteKingsideRookMoved;
        public readonly bool WhiteQueensideRookMoved;
        public readonly bool BlackKingMoved;
        public readonly bool BlackKingsideRookMoved;
        public readonly bool BlackQueensideRookMoved;

        public ChessCastlingRights(
            bool p_bWhiteKingMoved, bool p_bWhiteKingsideRookMoved, bool p_bWhiteQueensideRookMoved,
            bool p_bBlackKingMoved, bool p_bBlackKingsideRookMoved, bool p_bBlackQueensideRookMoved)
        {
            WhiteKingMoved = p_bWhiteKingMoved;
            WhiteKingsideRookMoved = p_bWhiteKingsideRookMoved;
            WhiteQueensideRookMoved = p_bWhiteQueensideRookMoved;
            BlackKingMoved = p_bBlackKingMoved;
            BlackKingsideRookMoved = p_bBlackKingsideRookMoved;
            BlackQueensideRookMoved = p_bBlackQueensideRookMoved;
        }

        public static ChessCastlingRights CreateInitial()
        {
            return new ChessCastlingRights(false, false, false, false, false, false);
        }

        /// <summary>
        /// p_nFromX/Y에서 p_nToX/Y로의 이번 수를 반영한 다음 상태를 계산합니다(순수 함수 — 이
        /// 값 자체는 바뀌지 않습니다). King이 움직였는지는 p_strMovedPieceType으로, 룩의 원래
        /// 자리를 벗어나거나(출발) 그 자리의 기물이 사라지는지(도착 = 이동 또는 포획)는 좌표만으로
        /// 판정합니다 — 그래서 "룩이 그 자리에서 잡혔다"도 "룩이 그 자리를 떠났다"와 동일하게
        /// 자동으로 처리됩니다.
        /// </summary>
        public ChessCastlingRights AfterMove(
            Ontology.E_PlayerColor p_eMovedColor, string p_strMovedPieceType, int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            bool bWhiteKingMoved = WhiteKingMoved || (p_strMovedPieceType == ChessPieceType.King && p_eMovedColor == Ontology.E_PlayerColor.White);
            bool bBlackKingMoved = BlackKingMoved || (p_strMovedPieceType == ChessPieceType.King && p_eMovedColor == Ontology.E_PlayerColor.Black);

            bool bWhiteQueensideRookMoved = WhiteQueensideRookMoved || TouchesSquare(p_nFromX, p_nFromY, 0, 0) || TouchesSquare(p_nToX, p_nToY, 0, 0);
            bool bWhiteKingsideRookMoved = WhiteKingsideRookMoved || TouchesSquare(p_nFromX, p_nFromY, 7, 0) || TouchesSquare(p_nToX, p_nToY, 7, 0);
            bool bBlackQueensideRookMoved = BlackQueensideRookMoved || TouchesSquare(p_nFromX, p_nFromY, 0, 7) || TouchesSquare(p_nToX, p_nToY, 0, 7);
            bool bBlackKingsideRookMoved = BlackKingsideRookMoved || TouchesSquare(p_nFromX, p_nFromY, 7, 7) || TouchesSquare(p_nToX, p_nToY, 7, 7);

            return new ChessCastlingRights(
                bWhiteKingMoved, bWhiteKingsideRookMoved, bWhiteQueensideRookMoved,
                bBlackKingMoved, bBlackKingsideRookMoved, bBlackQueensideRookMoved);
        }

        private static bool TouchesSquare(int p_nX, int p_nY, int p_nTargetX, int p_nTargetY)
        {
            return p_nX == p_nTargetX && p_nY == p_nTargetY;
        }
    }
}
