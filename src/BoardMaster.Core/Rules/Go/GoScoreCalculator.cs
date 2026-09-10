namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 중국식(면적) 계가 방식으로 최종 점수를 계산합니다.
    ///   점수(색) = 보드 위 그 색 돌의 개수 + 그 색에만 접한 빈 영역(집)의 크기.
    /// 양쪽 색 모두에 접하거나 어느 색에도 접하지 않는 빈 영역(공배)은 0점 처리합니다.
    ///
    /// 왜 중국식(면적)인가: 이 엔진에는 "사석 표시(dead stone marking)" 단계가 없습니다 — 자충수가 되는
    /// 순간 Effect_CaptureStones가 즉시 잡아내므로, 종국 시점에 남아있는 돌은 전부 활로를 가진 돌뿐입니다.
    /// 일본식(지역) 계가는 대국자가 사석을 합의해서 들어내는 절차가 필요한데 이 엔진엔 그 개념이 없어서,
    /// 사람의 개입 없이 규칙만으로 결정되는 면적 계가가 자연스럽게 맞습니다(AlphaGo/KataGo 등
    /// 대부분의 컴퓨터 바둑 엔진도 같은 이유로 면적 계가를 기본으로 씁니다).
    /// 포로 수(mv_nPrisonerCount)는 면적 계가에서 점수에 영향을 주지 않으므로 의도적으로 쓰지 않습니다.
    /// 덤(komi)도 적용하지 않습니다 — 필요해지면 호출부에서 White 점수에 더해 쓰면 됩니다.
    ///
    /// 종국 시점에 한 번만 호출되는 계산이라 Cond_NotSuicide/GoGroupScanner처럼 버퍼를 인스턴스에
    /// 재사용하지 않고, 매 호출마다 필요한 만큼만 할당합니다.
    /// </summary>
    public sealed class GoScoreCalculator
    {
        public ST_GoScoreResult CalculateScore(Ont.GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            int nBlackScore = 0;
            int nWhiteScore = 0;

            bool[] a_bVisited = new bool[nWidth * nHeight];
            Queue<int> quPending = new Queue<int>();

            for (int nY = 0; nY < nHeight; nY++)
            {
                for (int nX = 0; nX < nWidth; nX++)
                {
                    int nIndex = (nY * nWidth) + nX;
                    if (a_bVisited[nIndex])
                    {
                        continue;
                    }

                    Ont.E_PlayerColor eCellColor = (Ont.E_PlayerColor)a_nGrid[nX, nY];

                    if (eCellColor == Ont.E_PlayerColor.Black)
                    {
                        a_bVisited[nIndex] = true;
                        nBlackScore++;
                        continue;
                    }

                    if (eCellColor == Ont.E_PlayerColor.White)
                    {
                        a_bVisited[nIndex] = true;
                        nWhiteScore++;
                        continue;
                    }

                    int nRegionSize = FloodFillEmptyRegion(
                        a_nGrid, nWidth, nHeight, nX, nY, a_bVisited, quPending,
                        out bool bTouchesBlack, out bool bTouchesWhite);

                    if (bTouchesBlack && !bTouchesWhite)
                    {
                        nBlackScore += nRegionSize;
                    }
                    else if (bTouchesWhite && !bTouchesBlack)
                    {
                        nWhiteScore += nRegionSize;
                    }
                    // 둘 다 접하는 공배이거나(양쪽 다 안 접하는 경우는 빈 보드뿐) 어느 쪽에도 더하지 않는다.
                }
            }

            return new ST_GoScoreResult(nBlackScore, nWhiteScore);
        }

        /// <summary>
        /// (p_nStartX, p_nStartY)를 포함한 연결된 빈 칸 영역 전체를 BFS로 훑어 크기를 반환하고,
        /// 그 영역이 Black/White 중 어느 색(들)에 접하는지 out 파라미터로 알려줍니다.
        /// </summary>
        private static int FloodFillEmptyRegion(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nStartX,
            int p_nStartY,
            bool[] p_a_bVisited,
            Queue<int> p_quPending,
            out bool p_bTouchesBlack,
            out bool p_bTouchesWhite)
        {
            p_quPending.Clear();

            int nStartIndex = (p_nStartY * p_nWidth) + p_nStartX;
            p_a_bVisited[nStartIndex] = true;
            p_quPending.Enqueue(nStartIndex);

            int nRegionSize = 0;
            bool bTouchesBlack = false;
            bool bTouchesWhite = false;

            while (p_quPending.Count > 0)
            {
                int nCurrentIndex = p_quPending.Dequeue();
                int nCurrentX = nCurrentIndex % p_nWidth;
                int nCurrentY = nCurrentIndex / p_nWidth;
                nRegionSize++;

                VisitTerritoryNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX - 1, nCurrentY, p_a_bVisited, p_quPending, ref bTouchesBlack, ref bTouchesWhite);
                VisitTerritoryNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX + 1, nCurrentY, p_a_bVisited, p_quPending, ref bTouchesBlack, ref bTouchesWhite);
                VisitTerritoryNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY - 1, p_a_bVisited, p_quPending, ref bTouchesBlack, ref bTouchesWhite);
                VisitTerritoryNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY + 1, p_a_bVisited, p_quPending, ref bTouchesBlack, ref bTouchesWhite);
            }

            p_bTouchesBlack = bTouchesBlack;
            p_bTouchesWhite = bTouchesWhite;
            return nRegionSize;
        }

        private static void VisitTerritoryNeighbor(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nX,
            int p_nY,
            bool[] p_a_bVisited,
            Queue<int> p_quPending,
            ref bool p_bTouchesBlack,
            ref bool p_bTouchesWhite)
        {
            if (p_nX < 0 || p_nX >= p_nWidth || p_nY < 0 || p_nY >= p_nHeight)
            {
                return;
            }

            Ont.E_PlayerColor eColor = (Ont.E_PlayerColor)p_a_nGrid[p_nX, p_nY];

            if (eColor == Ont.E_PlayerColor.Black)
            {
                p_bTouchesBlack = true;
                return;
            }

            if (eColor == Ont.E_PlayerColor.White)
            {
                p_bTouchesWhite = true;
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
