namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// War/GuryongTu의 ZoneQuery와 같은 이유(Entity → Zone 단방향 참조뿐)로 존재하는 조회 헬퍼입니다.
    /// 체스는 칸이 64개뿐이고 기물도 최대 32개뿐이라 전체 스캔 비용은 무시할 만합니다 — MCTS처럼
    /// 초당 수만 번씩 부르는 뜨거운 경로가 아니므로 Go의 zero-alloc 설계를 따라갈 이유가 없습니다.
    /// </summary>
    internal static class ChessZoneQuery
    {
        /// <summary>(p_nX, p_nY) 칸에 있는 기물을 찾습니다. 비어 있으면 null입니다.</summary>
        public static Ont.Entity? FindPieceAt(Ont.GameContext p_objContext, int p_nX, int p_nY)
        {
            foreach (Ont.Entity objPiece in p_objContext.mv_lisEntities)
            {
                if (objPiece.mv_objLocatedZone.mv_strZoneID == ChessZoneId.Captured)
                {
                    continue;
                }

                if (objPiece.mv_objLocatedZone.mv_nX == p_nX && objPiece.mv_objLocatedZone.mv_nY == p_nY)
                {
                    return objPiece;
                }
            }

            return null;
        }

        /// <summary>아직 잡히지 않은(반상 위에 있는) p_eColor의 기물을 전부 반환합니다.</summary>
        public static List<Ont.Entity> FindActivePieces(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            List<Ont.Entity> lisResult = new List<Ont.Entity>();

            foreach (Ont.Entity objPiece in p_objContext.mv_lisEntities)
            {
                if (objPiece.mv_eColor == p_eColor && objPiece.mv_objLocatedZone.mv_strZoneID != ChessZoneId.Captured)
                {
                    lisResult.Add(objPiece);
                }
            }

            return lisResult;
        }

        /// <summary>p_eColor의 King을 찾습니다. 정상적인 대국에서는 항상 존재해야 합니다.</summary>
        public static Ont.Entity FindKing(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            foreach (Ont.Entity objPiece in FindActivePieces(p_objContext, p_eColor))
            {
                if (objPiece.mv_strType == ChessPieceType.King)
                {
                    return objPiece;
                }
            }

            throw new InvalidOperationException($"{p_eColor}의 King을 찾을 수 없습니다 — 대국 상태가 손상되었습니다.");
        }
    }
}
