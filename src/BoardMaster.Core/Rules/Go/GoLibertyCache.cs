namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// GetLegalMoves()의 자충수(Suicide) 판정을 위한 "그룹별 활로 개수" 캐시입니다.
    ///
    /// 배경: GetLegalMoves()는 매 롤아웃 스텝마다 빈 칸 최대 81개를 검사하는데, 기존에는 후보마다
    /// Cond_NotSuicide가 BFS로 관련 그룹을 처음부터 다시 훑었습니다(계측 결과 롤아웃 시간의 약 85%).
    /// 두 수 사이에 실제로 바뀌는 건 반상의 일부뿐인데 매번 전체를 다시 도는 게 낭비입니다.
    ///
    /// 핵심 아이디어: "착수 후 내 그룹에 활로가 하나라도 남는가"는 불리언 하나만 필요하므로, 그룹마다
    /// 활로 "집합"(비트마스크로 합집합 연산)까지는 필요 없고 "개수"만으로 충분합니다.
    /// - 이웃 그룹 중 하나라도 활로 개수가 1보다 크면(=착수 지점 말고 다른 진짜 활로가 있다는 뜻) 합법.
    ///   이 활로는 다른 그룹과 합쳐지더라도 절대 사라지지 않으므로, 그룹별로 독립적으로만 확인해도
    ///   안전합니다(여러 그룹이 같은 활로를 "이중 계산"해도 최소 하나는 실재).
    /// - 이웃 상대 그룹의 활로 개수가 정확히 1이면(=착수 지점이 유일한 활로) 그 그룹을 따내므로 합법.
    /// 그래서 Union-Find/비트마스크 없이 그룹당 정수 하나(활로 개수)만 유지하면 됩니다.
    ///
    /// 이 캐시는 실제 착수(PlayStone)가 일어날 때마다 통째로 다시 계산합니다(O(width*height), 후보당이
    /// 아니라 한 판에 많아야 수백 번). GoGameSession.ComputeCurrentPositionHash와 정확히 같은 전략입니다.
    /// GetLegalMoves()의 빠른 경로에서만 쓰이고, 실제 착수 검증(PlayStone → Cond_NotSuicide)과 그
    /// 단위 테스트들은 여전히 기존 BFS 버전을 그대로 씁니다 — 이 캐시가 조금이라도 틀려도 "진실 판정
    /// 로직"은 오염되지 않도록 범위를 좁혀 리스크를 낮췄습니다. 정확성은
    /// GoLibertyCacheDifferentialTests(무작위 대국을 여러 판 돌리며 이 캐시 기반 결과와 기존 BFS 기반
    /// 결과를 매 수마다 통째로 비교)로 검증합니다.
    ///
    /// 스레드 안전성: 재사용 버퍼 때문에 이 인스턴스는 스레드 세이프하지 않습니다.
    /// </summary>
    internal sealed class GoLibertyCache
    {
        private int[] m_a_nGroupId = Array.Empty<int>();
        private int[] m_a_nGroupLiberty = Array.Empty<int>();
        private int[] m_a_nLibertyStamp = Array.Empty<int>();
        private int m_nLibertyGeneration;
        private readonly Queue<int> m_quBfsQueue = new Queue<int>();

        /// <summary>
        /// p_a_nGrid의 현재 배치를 기준으로 그룹 ID와 그룹별 활로 개수를 처음부터 다시 계산합니다.
        /// 실제 착수가 성공했을 때만 호출하세요(패스나 후보 검증에는 호출할 필요가 없습니다).
        /// </summary>
        internal void Rebuild(int[,] p_a_nGrid, int p_nWidth, int p_nHeight)
        {
            int nCellCount = p_nWidth * p_nHeight;
            EnsureCapacity(nCellCount);

            for (int i = 0; i < nCellCount; i++)
            {
                m_a_nGroupId[i] = -1;
            }

            int nNextGroupId = 0;

            for (int nY = 0; nY < p_nHeight; nY++)
            {
                for (int nX = 0; nX < p_nWidth; nX++)
                {
                    int nStartIndex = (nY * p_nWidth) + nX;
                    if (m_a_nGroupId[nStartIndex] != -1)
                    {
                        continue;
                    }

                    Ont.E_PlayerColor eColor = (Ont.E_PlayerColor)p_a_nGrid[nX, nY];
                    if (eColor == Ont.E_PlayerColor.None)
                    {
                        continue;
                    }

                    int nGroupId = nNextGroupId++;
                    m_a_nGroupLiberty[nGroupId] = FloodFillGroup(p_a_nGrid, p_nWidth, p_nHeight, nStartIndex, eColor, nGroupId);
                }
            }
        }

        /// <summary>
        /// (p_nX, p_nY)에 p_eColor로 두었을 때 자충수가 아닌지(=착수 후 내 그룹에 활로가 하나라도
        /// 남거나, 인접 상대 그룹을 따내는지) 판정합니다. Rebuild가 이미 현재 배치를 반영하고 있다고
        /// 가정합니다 — (p_nX, p_nY) 자체는 아직 빈 칸이어야 합니다(호출자가 보장).
        /// </summary>
        internal bool IsNonSuicidePlacement(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            return CheckNeighbor(p_a_nGrid, p_nWidth, p_nHeight, p_nX - 1, p_nY, p_eColor)
                || CheckNeighbor(p_a_nGrid, p_nWidth, p_nHeight, p_nX + 1, p_nY, p_eColor)
                || CheckNeighbor(p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY - 1, p_eColor)
                || CheckNeighbor(p_a_nGrid, p_nWidth, p_nHeight, p_nX, p_nY + 1, p_eColor);
        }

        private bool CheckNeighbor(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nNeighborX, int p_nNeighborY, Ont.E_PlayerColor p_eColor)
        {
            if (p_nNeighborX < 0 || p_nNeighborX >= p_nWidth || p_nNeighborY < 0 || p_nNeighborY >= p_nHeight)
            {
                return false;
            }

            Ont.E_PlayerColor eNeighborColor = (Ont.E_PlayerColor)p_a_nGrid[p_nNeighborX, p_nNeighborY];
            if (eNeighborColor == Ont.E_PlayerColor.None)
            {
                return true; // 빈 이웃 = 착수 즉시 확보되는 활로.
            }

            int nGroupId = m_a_nGroupId[(p_nNeighborY * p_nWidth) + p_nNeighborX];
            int nLiberties = m_a_nGroupLiberty[nGroupId];

            if (eNeighborColor == p_eColor)
            {
                return nLiberties > 1; // 착수 지점 말고 다른 활로가 이미 있는 아군 그룹.
            }

            return nLiberties == 1; // 착수 지점이 유일한 활로인 상대 그룹 = 따낼 수 있음.
        }

        private int FloodFillGroup(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nStartIndex, Ont.E_PlayerColor p_eColor, int p_nGroupId)
        {
            m_nLibertyGeneration++;
            if (m_nLibertyGeneration == int.MaxValue)
            {
                Array.Clear(m_a_nLibertyStamp, 0, m_a_nLibertyStamp.Length);
                m_nLibertyGeneration = 1;
            }

            m_quBfsQueue.Clear();
            m_a_nGroupId[p_nStartIndex] = p_nGroupId;
            m_quBfsQueue.Enqueue(p_nStartIndex);

            int nLiberties = 0;

            while (m_quBfsQueue.Count > 0)
            {
                int nCurrentIndex = m_quBfsQueue.Dequeue();
                int nCurrentX = nCurrentIndex % p_nWidth;
                int nCurrentY = nCurrentIndex / p_nWidth;

                nLiberties += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX - 1, nCurrentY, p_eColor, p_nGroupId);
                nLiberties += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX + 1, nCurrentY, p_eColor, p_nGroupId);
                nLiberties += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY - 1, p_eColor, p_nGroupId);
                nLiberties += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY + 1, p_eColor, p_nGroupId);
            }

            return nLiberties;
        }

        private int VisitNeighbor(int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nNeighborX, int p_nNeighborY, Ont.E_PlayerColor p_eGroupColor, int p_nGroupId)
        {
            if (p_nNeighborX < 0 || p_nNeighborX >= p_nWidth || p_nNeighborY < 0 || p_nNeighborY >= p_nHeight)
            {
                return 0;
            }

            int nNeighborIndex = (p_nNeighborY * p_nWidth) + p_nNeighborX;
            Ont.E_PlayerColor eNeighborColor = (Ont.E_PlayerColor)p_a_nGrid[p_nNeighborX, p_nNeighborY];

            if (eNeighborColor == Ont.E_PlayerColor.None)
            {
                if (m_a_nLibertyStamp[nNeighborIndex] == m_nLibertyGeneration)
                {
                    return 0; // 그룹 내 다른 돌과 공유하는 활로를 중복 집계하지 않는다.
                }

                m_a_nLibertyStamp[nNeighborIndex] = m_nLibertyGeneration;
                return 1;
            }

            if (eNeighborColor == p_eGroupColor && m_a_nGroupId[nNeighborIndex] == -1)
            {
                m_a_nGroupId[nNeighborIndex] = p_nGroupId;
                m_quBfsQueue.Enqueue(nNeighborIndex);
            }

            return 0;
        }

        private void EnsureCapacity(int p_nCellCount)
        {
            if (m_a_nGroupId.Length < p_nCellCount)
            {
                m_a_nGroupId = new int[p_nCellCount];
                m_a_nGroupLiberty = new int[p_nCellCount]; // 최악의 경우(돌마다 별개 그룹) 그룹 수 상한 = 칸 수.
                m_a_nLibertyStamp = new int[p_nCellCount];
                m_nLibertyGeneration = 0;
            }
        }
    }
}
