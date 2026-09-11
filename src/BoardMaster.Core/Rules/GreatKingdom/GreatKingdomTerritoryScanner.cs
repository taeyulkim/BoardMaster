namespace BoardMaster.Core.Rules.GreatKingdom
{
    /// <summary>
    /// 빈 칸으로 이어진 영역 하나를 BFS로 훑어, 그 영역이 어느 진영의 "완성된 영토"인지 분류합니다.
    /// 한 색에만 접하고 중립 성이나 반대 색에는 전혀 접하지 않아야 그 색의 영토입니다 — 중립 성에
    /// 접하기만 해도(중립은 어느 진영도 아니므로) 그 영역은 누구의 영토도 아닙니다(GreatKingdomCell.Empty
    /// 로 표시).
    ///
    /// 이 계산은 두 곳에서 똑같이 필요합니다: (1) Cond_NotOpponentTerritory — 착수하려는 칸이 속한
    /// 영역이 상대의 완성된 영토인지(매 후보 착수마다), (2) GreatKingdomScoreCalculator — 종국 시
    /// 반상 전체의 영토 합계. 로직이 갈라지는 걸 막으려고 이 클래스 하나로 모았습니다.
    ///
    /// 매 호출마다 새로 배열을 할당합니다(Go의 Cond_NotSuperko가 원래 그랬듯) — 실측 결과 성능이
    /// 문제가 되면 Go처럼 나중에 최적화할 수 있는 지점입니다.
    /// </summary>
    internal static class GreatKingdomTerritoryScanner
    {
        internal readonly struct RegionResult
        {
            public readonly int Size;
            public readonly int OwnerCellValue; // Player1, Player2, 또는 Empty(=누구의 영토도 아님)

            public RegionResult(int p_nSize, int p_nOwnerCellValue)
            {
                Size = p_nSize;
                OwnerCellValue = p_nOwnerCellValue;
            }
        }

        internal static RegionResult ClassifyRegionContaining(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nStartX, int p_nStartY)
        {
            bool[] a_bVisited = new bool[p_nWidth * p_nHeight];
            return ClassifyRegion(p_a_nGrid, p_nWidth, p_nHeight, p_nStartX, p_nStartY, a_bVisited);
        }

        /// <summary>
        /// 반상 전체를 훑어 Player1/Player2 각자의 완성된 영토 크기 합계를 반환합니다. 종국 계가에서만
        /// 씁니다(GreatKingdomScoreCalculator).
        /// </summary>
        internal static (int Player1Territory, int Player2Territory) CalculateAllTerritory(int[,] p_a_nGrid, int p_nWidth, int p_nHeight)
        {
            bool[] a_bVisited = new bool[p_nWidth * p_nHeight];
            int nPlayer1Territory = 0;
            int nPlayer2Territory = 0;

            for (int nY = 0; nY < p_nHeight; nY++)
            {
                for (int nX = 0; nX < p_nWidth; nX++)
                {
                    int nIndex = (nY * p_nWidth) + nX;
                    if (a_bVisited[nIndex] || p_a_nGrid[nX, nY] != GreatKingdomCell.Empty)
                    {
                        continue;
                    }

                    RegionResult stRegion = ClassifyRegion(p_a_nGrid, p_nWidth, p_nHeight, nX, nY, a_bVisited);

                    if (stRegion.OwnerCellValue == GreatKingdomCell.Player1)
                    {
                        nPlayer1Territory += stRegion.Size;
                    }
                    else if (stRegion.OwnerCellValue == GreatKingdomCell.Player2)
                    {
                        nPlayer2Territory += stRegion.Size;
                    }
                }
            }

            return (nPlayer1Territory, nPlayer2Territory);
        }

        private static RegionResult ClassifyRegion(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nStartX, int p_nStartY, bool[] p_a_bVisited)
        {
            Queue<int> quPending = new Queue<int>();
            int nStartIndex = (p_nStartY * p_nWidth) + p_nStartX;
            p_a_bVisited[nStartIndex] = true;
            quPending.Enqueue(nStartIndex);

            int nSize = 0;
            bool bTouchesPlayer1 = false;
            bool bTouchesPlayer2 = false;
            bool bTouchesNeutral = false;

            while (quPending.Count > 0)
            {
                int nCurrentIndex = quPending.Dequeue();
                int nCurrentX = nCurrentIndex % p_nWidth;
                int nCurrentY = nCurrentIndex / p_nWidth;
                nSize++;

                VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX - 1, nCurrentY, p_a_bVisited, quPending, ref bTouchesPlayer1, ref bTouchesPlayer2, ref bTouchesNeutral);
                VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX + 1, nCurrentY, p_a_bVisited, quPending, ref bTouchesPlayer1, ref bTouchesPlayer2, ref bTouchesNeutral);
                VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY - 1, p_a_bVisited, quPending, ref bTouchesPlayer1, ref bTouchesPlayer2, ref bTouchesNeutral);
                VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY + 1, p_a_bVisited, quPending, ref bTouchesPlayer1, ref bTouchesPlayer2, ref bTouchesNeutral);
            }

            int nOwner = (bTouchesPlayer1 && !bTouchesPlayer2 && !bTouchesNeutral) ? GreatKingdomCell.Player1
                : (bTouchesPlayer2 && !bTouchesPlayer1 && !bTouchesNeutral) ? GreatKingdomCell.Player2
                : GreatKingdomCell.Empty;

            return new RegionResult(nSize, nOwner);
        }

        private static void VisitNeighbor(
            int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, bool[] p_a_bVisited, Queue<int> p_quPending,
            ref bool p_bTouchesPlayer1, ref bool p_bTouchesPlayer2, ref bool p_bTouchesNeutral)
        {
            if (p_nX < 0 || p_nX >= p_nWidth || p_nY < 0 || p_nY >= p_nHeight)
            {
                return;
            }

            int nCellValue = p_a_nGrid[p_nX, p_nY];

            if (nCellValue == GreatKingdomCell.Player1)
            {
                p_bTouchesPlayer1 = true;
                return;
            }

            if (nCellValue == GreatKingdomCell.Player2)
            {
                p_bTouchesPlayer2 = true;
                return;
            }

            if (nCellValue == GreatKingdomCell.Neutral)
            {
                p_bTouchesNeutral = true;
                return;
            }

            int nIndex = (p_nY * p_nWidth) + p_nX;
            if (!p_a_bVisited[nIndex])
            {
                p_a_bVisited[nIndex] = true;
                p_quPending.Enqueue(nIndex);
            }
        }
    }
}
