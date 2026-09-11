namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 앙파상(en passant) 포획을 처리합니다. 도착 칸이 "이번 수 이전의" 앙파상 대상 칸과 같고 이동한
    /// 기물이 폰이면, 실제로 잡히는 폰은 도착 칸이 아니라 "도착 칸과 같은 파일, 출발 칸과 같은 랭크"에
    /// 있습니다(옆으로 지나가며 잡는 앙파상의 정의 그대로). 앙파상 대상 칸은 GameContext에 없는
    /// 장르 전용 파생 상태라 ChessGameSession이 생성자로 주입합니다.
    /// </summary>
    public sealed class Effect_HandleEnPassantCapture : Ont.IEffect
    {
        private readonly (int X, int Y)? m_stEnPassantTarget;

        public Effect_HandleEnPassantCapture((int X, int Y)? p_stEnPassantTarget)
        {
            m_stEnPassantTarget = p_stEnPassantTarget;
        }

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (m_stEnPassantTarget is not { } stTarget)
            {
                return p_objContext;
            }

            (int nFromX, int nFromY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            (int nToX, int nToY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            if (nToX != stTarget.X || nToY != stTarget.Y)
            {
                return p_objContext; // 이번 수는 앙파상 대상 칸으로 간 게 아니다.
            }

            Ont.Entity? objMovedPiece = ChessZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            if (objMovedPiece is null || objMovedPiece.mv_strType != ChessPieceType.Pawn)
            {
                return p_objContext; // 폰이 아니면(우연히 그 칸으로 간 다른 기물) 앙파상이 아니다.
            }

            Ont.Entity? objVictim = ChessZoneQuery.FindPieceAt(p_objContext, nToX, nFromY);
            if (objVictim is not null)
            {
                objVictim.mv_objLocatedZone = p_objContext.mv_dicZones[ChessZoneId.Captured];
            }

            return p_objContext;
        }
    }
}
