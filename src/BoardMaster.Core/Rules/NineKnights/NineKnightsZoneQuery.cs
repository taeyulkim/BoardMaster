namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>War/GuryongTu/Chess의 ZoneQuery와 같은 이유(Entity → Zone 단방향 참조뿐)로 존재하는 조회 헬퍼입니다.</summary>
    internal static class NineKnightsZoneQuery
    {
        public static Ont.Entity? FindPieceAt(Ont.GameContext p_objContext, int p_nX, int p_nY)
        {
            // 예비/포획 Zone은 전부 좌표가 (-1,-1)이라 반상 좌표(항상 0 이상)와는 절대 안 겹친다 —
            // 그래도 의도를 분명히 하려고 명시적으로 좌표만 비교한다.
            foreach (Ont.Entity objPiece in p_objContext.mv_lisEntities)
            {
                Ont.Zone objZone = objPiece.mv_objLocatedZone;
                if (objZone.mv_nX == p_nX && objZone.mv_nY == p_nY && p_nX >= 0 && p_nY >= 0)
                {
                    return objPiece;
                }
            }

            return null;
        }

        public static List<Ont.Entity> FindActivePiecesOnBoard(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            List<Ont.Entity> lisResult = new List<Ont.Entity>();

            foreach (Ont.Entity objPiece in p_objContext.mv_lisEntities)
            {
                if (objPiece.mv_eColor != p_eColor)
                {
                    continue;
                }

                if (objPiece.mv_objLocatedZone.mv_nX >= 0 && objPiece.mv_objLocatedZone.mv_nY >= 0)
                {
                    lisResult.Add(objPiece);
                }
            }

            return lisResult;
        }

        public static bool IsEliminated(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            return FindActivePiecesOnBoard(p_objContext, p_eColor).Count == 0 && FindReservePieces(p_objContext, p_eColor).Count == 0;
        }

        public static List<Ont.Entity> FindReservePieces(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            string strReserveZoneId = p_eColor == Ont.E_PlayerColor.Black ? NineKnightsZoneId.ReservePlayer1 : NineKnightsZoneId.ReservePlayer2;
            List<Ont.Entity> lisResult = new List<Ont.Entity>();

            foreach (Ont.Entity objPiece in p_objContext.mv_lisEntities)
            {
                if (objPiece.mv_eColor == p_eColor && objPiece.mv_objLocatedZone.mv_strZoneID == strReserveZoneId)
                {
                    lisResult.Add(objPiece);
                }
            }

            return lisResult;
        }
    }
}
