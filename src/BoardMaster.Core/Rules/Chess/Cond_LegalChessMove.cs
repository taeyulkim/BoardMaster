namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 요청된 수(ST_ActionData로 인코딩된 출발/도착 칸)가 ChessMoveGenerator 기준으로 진짜 합법수인지
    /// 검증합니다 — 기물 이동 규칙, 자기 King 노출 금지, 캐슬링/앙파상 조건까지 전부 포함합니다.
    /// 캐슬링 권리와 앙파상 대상 칸은 GameContext에 없는 장르 전용 파생 상태라 ChessGameSession이
    /// 생성자로 주입합니다(Go의 Cond_NotKoRecapture/Cond_NotSuperko와 같은 패턴).
    /// </summary>
    public sealed class Cond_LegalChessMove : Ont.ICondition
    {
        private readonly ChessCastlingRights m_stRights;
        private readonly (int X, int Y)? m_stEnPassantTarget;

        internal Cond_LegalChessMove(ChessCastlingRights p_stRights, (int X, int Y)? p_stEnPassantTarget)
        {
            m_stRights = p_stRights;
            m_stEnPassantTarget = p_stEnPassantTarget;
        }

        public bool IsSatisfied(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            (int nFromX, int nFromY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            (int nToX, int nToY) = ChessActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            Ont.Entity? objPiece = ChessZoneQuery.FindPieceAt(p_objContext, nFromX, nFromY);
            if (objPiece is null || objPiece.mv_eColor != p_objAction.mv_stActionData.m_eColor)
            {
                return false; // 출발 칸에 자기 기물이 없다.
            }

            return ChessMoveGenerator.IsLegalMove(p_objContext, nFromX, nFromY, nToX, nToY, m_stRights, m_stEnPassantTarget);
        }
    }
}
