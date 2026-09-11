namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 빙고(5칸 일렬, 또는 가운데 칸을 포함한 4칸 일렬) 판정입니다. 가운데 칸은 실제 왕관이 놓이지
    /// 않지만 "이미 두 색 모두의 왕관이 놓인 것"으로 간주되는 와일드카드라, 줄 안의 각 칸을
    /// "가운데 칸이거나, 판정 대상 색의 왕관이 실제로 놓여 있음"으로 검사하는 것만으로 두 가지
    /// 빙고 형태(5칸/가운데 포함 4칸)를 하나의 로직으로 처리합니다.
    /// </summary>
    internal static class KingsCrownWinRules
    {
        public static bool HasBingo(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            foreach (IReadOnlyList<(int X, int Y)> lisLine in KingsCrownBoardGeometry.Lines)
            {
                if (IsLineComplete(p_objContext, lisLine, p_eColor))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>(p_nX,p_nY)에 p_eColor 왕관을 놓는다고 가정했을 때 즉시 빙고가 완성되는지입니다 —
        /// 실제로 GameContext를 바꾸지 않고 판정하므로 AI의 후보 평가에 씁니다.</summary>
        public static bool WouldCompleteBingo(Ont.GameContext p_objContext, int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            foreach (IReadOnlyList<(int X, int Y)> lisLine in KingsCrownBoardGeometry.Lines)
            {
                bool bContainsCell = false;
                foreach ((int X, int Y) in lisLine)
                {
                    if (X == p_nX && Y == p_nY)
                    {
                        bContainsCell = true;
                        break;
                    }
                }
                if (!bContainsCell)
                {
                    continue;
                }

                bool bComplete = true;
                foreach ((int X, int Y) in lisLine)
                {
                    if (X == p_nX && Y == p_nY)
                    {
                        continue; // 가정상 여기엔 내 왕관이 놓인다.
                    }
                    if (KingsCrownBoardGeometry.IsCenter(X, Y))
                    {
                        continue;
                    }

                    Ont.Entity? objPiece = KingsCrownZoneQuery.FindPieceAt(p_objContext, X, Y);
                    if (objPiece is null || objPiece.mv_eColor != p_eColor)
                    {
                        bComplete = false;
                        break;
                    }
                }

                if (bComplete)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLineComplete(Ont.GameContext p_objContext, IReadOnlyList<(int X, int Y)> p_lisLine, Ont.E_PlayerColor p_eColor)
        {
            foreach ((int X, int Y) in p_lisLine)
            {
                if (KingsCrownBoardGeometry.IsCenter(X, Y))
                {
                    continue;
                }

                Ont.Entity? objPiece = KingsCrownZoneQuery.FindPieceAt(p_objContext, X, Y);
                if (objPiece is null || objPiece.mv_eColor != p_eColor)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
