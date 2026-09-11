namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;

    internal static class KingsCrownZoneQuery
    {
        public static Ont.Entity? FindPieceAt(Ont.GameContext p_objContext, int p_nX, int p_nY)
        {
            foreach (Ont.Entity objPiece in p_objContext.mv_lisEntities)
            {
                Ont.Zone objZone = objPiece.mv_objLocatedZone;
                if (objZone.mv_nX == p_nX && objZone.mv_nY == p_nY)
                {
                    return objPiece;
                }
            }

            return null;
        }
    }
}
