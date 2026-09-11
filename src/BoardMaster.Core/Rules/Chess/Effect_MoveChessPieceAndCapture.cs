namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 출발 칸의 기물을 도착 칸으로 옮깁니다. 도착 칸에 상대 기물이 있었다면(일반 포획) 그 기물을
    /// Captured Zone으로 치웁니다. 앙파상 포획(도착 칸 자체는 비어 있고 옆 칸의 폰이 사라짐)은
    /// 이 Effect의 책임이 아니라 Effect_HandleEnPassantCapture가 별도로 처리합니다.
    /// </summary>
    public sealed class Effect_MoveChessPieceAndCapture : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            (int nFromX, int nFromY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            (int nToX, int nToY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            Ont.Entity objMovingPiece = ChessZoneQuery.FindPieceAt(p_objContext, nFromX, nFromY)!;

            Ont.Entity? objCapturedPiece = ChessZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            if (objCapturedPiece is not null)
            {
                objCapturedPiece.mv_objLocatedZone = p_objContext.mv_dicZones[ChessZoneId.Captured];
            }

            objMovingPiece.mv_objLocatedZone = p_objContext.mv_dicZones[ChessZoneId.Square(nToX, nToY)];

            return p_objContext;
        }
    }
}
