namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 캐슬링으로 King이 두 칸 이동했다면, 그에 맞춰 룩도 King 반대편 옆 칸으로 옮깁니다. 이건
    /// 이번 수가 King의 2칸 이동이라는 사실만으로 판정할 수 있어서(캐슬링이 아니면 King은 절대
    /// 2칸을 움직일 수 없습니다 — Cond_LegalChessMove가 이미 그 외의 King 2칸 이동을 걸러냈습니다),
    /// 앙파상과 달리 별도의 주입 상태 없이 반상 상태만으로 동작합니다.
    /// </summary>
    public sealed class Effect_HandleCastlingRookMove : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            (int nFromX, int nFromY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            (int nToX, int nToY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            Ont.Entity? objMovedPiece = ChessZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            if (objMovedPiece is null || objMovedPiece.mv_strType != ChessPieceType.King || Math.Abs(nToX - nFromX) != 2)
            {
                return p_objContext;
            }

            bool bKingside = nToX > nFromX;
            int nRookFromX = bKingside ? 7 : 0;
            int nRookToX = bKingside ? 5 : 3;

            Ont.Entity? objRook = ChessZoneQuery.FindPieceAt(p_objContext, nRookFromX, nFromY);
            if (objRook is not null)
            {
                objRook.mv_objLocatedZone = p_objContext.mv_dicZones[ChessZoneId.Square(nRookToX, nFromY)];
            }

            return p_objContext;
        }
    }
}
