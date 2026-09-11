namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// "왕관 놓는 규칙"의 단일 판정 로직입니다 — 조건(Cond_LegalKingsCrownPlacement), 세션의 합법수
    /// 조회(GetLegalPlacements), 상대가 더 이상 놓을 수 없는지 확인하는 효과
    /// (Effect_CheckBingoAndStalemateWin)가 모두 이 함수 하나를 공유합니다.
    ///
    /// 규칙서 원문: 아무 왕관도 인접하지 않은 칸(가운데 제외)에는 아무 숫자든 놓을 수 있다. 다른
    /// 왕관과 상하좌우로 인접한 칸에는, 그 인접한 왕관들 중 하나라도 "같은 색이면서 연속된 숫자"
    /// 또는 "다른 색이면서 같은 숫자"를 만족해야 놓을 수 있다(대각선은 인접으로 치지 않는다).
    /// </summary>
    internal static class KingsCrownPlacementRules
    {
        private static readonly (int Dx, int Dy)[] s_a_stOrthogonalDirs =
        {
            (0, 1), (0, -1), (1, 0), (-1, 0)
        };

        public static bool CanPlace(Ont.GameContext p_objContext, int p_nX, int p_nY, Ont.E_PlayerColor p_eColor, int p_nNumber)
        {
            if (p_nX < 0 || p_nX >= KingsCrownGameFactory.BOARD_SIZE || p_nY < 0 || p_nY >= KingsCrownGameFactory.BOARD_SIZE)
            {
                return false;
            }

            if (KingsCrownBoardGeometry.IsCenter(p_nX, p_nY))
            {
                return false;
            }

            if (KingsCrownZoneQuery.FindPieceAt(p_objContext, p_nX, p_nY) is not null)
            {
                return false;
            }

            bool bHasAnyNeighbor = false;
            foreach ((int Dx, int Dy) in s_a_stOrthogonalDirs)
            {
                Ont.Entity? objNeighbor = KingsCrownZoneQuery.FindPieceAt(p_objContext, p_nX + Dx, p_nY + Dy);
                if (objNeighbor is null)
                {
                    continue;
                }

                bHasAnyNeighbor = true;

                int nNeighborNumber = int.Parse(objNeighbor.mv_strType);
                bool bSameColor = objNeighbor.mv_eColor == p_eColor;
                if (bSameColor && Math.Abs(nNeighborNumber - p_nNumber) == 1)
                {
                    return true;
                }
                if (!bSameColor && nNeighborNumber == p_nNumber)
                {
                    return true;
                }
            }

            return !bHasAnyNeighbor;
        }
    }
}
