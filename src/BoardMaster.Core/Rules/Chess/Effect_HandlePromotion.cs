namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 폰이 마지막 랭크(White는 7, Black은 0)에 도달하면 승진시킵니다. 실제 체스는 플레이어가 승진할
    /// 기물을 고르지만(퀸/룩/비숍/나이트), 이 구현은 항상 퀸으로 자동 승진합니다 — War가 전쟁 시
    /// 서브 배틀을 생략한 것과 같은 성격의 의도적 단순화입니다(선택 UI 없이도 게임이 완결되게 하려고).
    /// </summary>
    public sealed class Effect_HandlePromotion : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            (int nToX, int nToY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            Ont.Entity? objPiece = ChessZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            if (objPiece is null || objPiece.mv_strType != ChessPieceType.Pawn)
            {
                return p_objContext;
            }

            int nPromotionRank = objPiece.mv_eColor == Ont.E_PlayerColor.White ? 7 : 0;
            if (nToY == nPromotionRank)
            {
                objPiece.mv_strType = ChessPieceType.Queen;
            }

            return p_objContext;
        }
    }
}
