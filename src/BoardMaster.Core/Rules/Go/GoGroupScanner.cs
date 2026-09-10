namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 바둑판 위 동색 그룹에 대한 Flood Fill(BFS) 그래프 탐색을 제공하는 재사용 가능한 zero-alloc 엔진입니다.
    /// 활로 판정(Cond_NotSuicide)과 사석 제거(Effect_CaptureStones)가 같은 그래프 탐색 로직을 공유하도록
    /// 이 클래스로 분리했습니다 — 두 곳에서 BFS를 독립적으로 각자 구현하면 언젠가 로직이 갈라질 위험이 있습니다.
    ///
    /// 성능 설계 (Cond_NotSuicide에서 그대로 옮겨온 것):
    /// - 실제 격자를 Clone()하지 않고, GetCellColor가 착수 좌표만 가상으로 치환해 읽는다.
    /// - 좌표는 (y * width + x) 정수 인덱스로 다루고, 방문/활로/그룹멤버 판정은 "세대 스탬프" 배열로 처리해
    ///   Array.Clear 없이 세대 번호 증가만으로 O(1) 리셋한다.
    /// - BFS 큐와 스탬프 배열, 그룹 멤버 출력 버퍼는 인스턴스 필드로 재사용되어 정상 동작 구간에서
    ///   호출당 힙 할당이 없다.
    ///
    /// 스레드 안전성: 재사용 버퍼 때문에 이 인스턴스는 스레드 세이프하지 않습니다. 규칙/효과 조립 시점에
    /// 한 번만 만들어 재사용하세요.
    /// </summary>
    internal sealed class GoGroupScanner
    {
        private int[] m_a_nVisitedStamp = Array.Empty<int>();
        private int[] m_a_nLibertyStamp = Array.Empty<int>();
        private int m_nVisitedGeneration;
        private int m_nLibertyGeneration;
        private readonly Queue<int> m_quBfsQueue = new Queue<int>();

        /// <summary>
        /// (p_nStartX, p_nStartY)를 포함한 p_eGroupColor 동색 그룹 전체의 활로 개수를 계산합니다.
        /// (p_nOverrideX, p_nOverrideY)는 아직 실제 격자에 쓰이지 않은 가상 착수 좌표이며, 이 좌표를 읽을 때만
        /// p_eOverrideColor로 간주합니다. 가상 치환이 필요 없다면 격자 범위를 벗어난 좌표(-1, -1)를 넘기면 됩니다.
        /// </summary>
        internal int CalculateLiberties(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nStartX,
            int p_nStartY,
            Ont.E_PlayerColor p_eGroupColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor)
        {
            return Scan(
                p_a_nGrid, p_nWidth, p_nHeight, p_nStartX, p_nStartY, p_eGroupColor,
                p_nOverrideX, p_nOverrideY, p_eOverrideColor, null, out _);
        }

        /// <summary>
        /// (p_nStartX, p_nStartY)를 포함한 동색 그룹 전체의 좌표(인덱스 = y*width+x)를
        /// p_a_nOutMemberIndices에 채워 넣고, 실제로 채운 멤버 개수를 반환합니다.
        /// 버퍼는 호출자가 최소 width*height 크기로 미리 마련해 재사용해야 합니다(사석 제거 시 사용).
        /// </summary>
        internal int CollectGroupMembers(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nStartX,
            int p_nStartY,
            Ont.E_PlayerColor p_eGroupColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor,
            int[] p_a_nOutMemberIndices)
        {
            Scan(
                p_a_nGrid, p_nWidth, p_nHeight, p_nStartX, p_nStartY, p_eGroupColor,
                p_nOverrideX, p_nOverrideY, p_eOverrideColor, p_a_nOutMemberIndices, out int nMemberCount);
            return nMemberCount;
        }

        private int Scan(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nStartX,
            int p_nStartY,
            Ont.E_PlayerColor p_eGroupColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor,
            int[]? p_a_nOutMemberIndices,
            out int p_nMemberCount)
        {
            EnsureBufferCapacity(p_nWidth * p_nHeight);

            m_nVisitedGeneration++;
            m_nLibertyGeneration++;
            if (m_nVisitedGeneration == int.MaxValue || m_nLibertyGeneration == int.MaxValue)
            {
                Array.Clear(m_a_nVisitedStamp, 0, m_a_nVisitedStamp.Length);
                Array.Clear(m_a_nLibertyStamp, 0, m_a_nLibertyStamp.Length);
                m_nVisitedGeneration = 1;
                m_nLibertyGeneration = 1;
            }

            m_quBfsQueue.Clear();

            int nStartIndex = (p_nStartY * p_nWidth) + p_nStartX;
            m_a_nVisitedStamp[nStartIndex] = m_nVisitedGeneration;
            m_quBfsQueue.Enqueue(nStartIndex);

            int nMemberCount = 0;
            if (p_a_nOutMemberIndices != null)
            {
                p_a_nOutMemberIndices[nMemberCount] = nStartIndex;
            }

            nMemberCount++;

            int nLibertyCount = 0;

            while (m_quBfsQueue.Count > 0)
            {
                int nCurrentIndex = m_quBfsQueue.Dequeue();
                int nCurrentX = nCurrentIndex % p_nWidth;
                int nCurrentY = nCurrentIndex / p_nWidth;

                nLibertyCount += VisitNeighbor(
                    p_a_nGrid, p_nWidth, p_nHeight, nCurrentX - 1, nCurrentY, p_eGroupColor,
                    p_nOverrideX, p_nOverrideY, p_eOverrideColor, p_a_nOutMemberIndices, ref nMemberCount);
                nLibertyCount += VisitNeighbor(
                    p_a_nGrid, p_nWidth, p_nHeight, nCurrentX + 1, nCurrentY, p_eGroupColor,
                    p_nOverrideX, p_nOverrideY, p_eOverrideColor, p_a_nOutMemberIndices, ref nMemberCount);
                nLibertyCount += VisitNeighbor(
                    p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY - 1, p_eGroupColor,
                    p_nOverrideX, p_nOverrideY, p_eOverrideColor, p_a_nOutMemberIndices, ref nMemberCount);
                nLibertyCount += VisitNeighbor(
                    p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY + 1, p_eGroupColor,
                    p_nOverrideX, p_nOverrideY, p_eOverrideColor, p_a_nOutMemberIndices, ref nMemberCount);
            }

            p_nMemberCount = nMemberCount;
            return nLibertyCount;
        }

        private int VisitNeighbor(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nNeighborX,
            int p_nNeighborY,
            Ont.E_PlayerColor p_eGroupColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor,
            int[]? p_a_nOutMemberIndices,
            ref int p_nMemberCount)
        {
            if (!IsInBounds(p_nNeighborX, p_nNeighborY, p_nWidth, p_nHeight))
            {
                return 0;
            }

            int nNeighborIndex = (p_nNeighborY * p_nWidth) + p_nNeighborX;
            Ont.E_PlayerColor eNeighborColor =
                GetCellColor(p_a_nGrid, p_nNeighborX, p_nNeighborY, p_nOverrideX, p_nOverrideY, p_eOverrideColor);

            if (eNeighborColor == Ont.E_PlayerColor.None)
            {
                if (m_a_nLibertyStamp[nNeighborIndex] == m_nLibertyGeneration)
                {
                    return 0; // 그룹 내 다른 돌과 공유하는 활로를 중복 집계하지 않는다.
                }

                m_a_nLibertyStamp[nNeighborIndex] = m_nLibertyGeneration;
                return 1;
            }

            if (eNeighborColor == p_eGroupColor && m_a_nVisitedStamp[nNeighborIndex] != m_nVisitedGeneration)
            {
                m_a_nVisitedStamp[nNeighborIndex] = m_nVisitedGeneration;
                m_quBfsQueue.Enqueue(nNeighborIndex);

                if (p_a_nOutMemberIndices != null)
                {
                    p_a_nOutMemberIndices[p_nMemberCount] = nNeighborIndex;
                }

                p_nMemberCount++;
            }

            return 0;
        }

        private static Ont.E_PlayerColor GetCellColor(
            int[,] p_a_nGrid, int p_nX, int p_nY, int p_nOverrideX, int p_nOverrideY, Ont.E_PlayerColor p_eOverrideColor)
        {
            if (p_nX == p_nOverrideX && p_nY == p_nOverrideY)
            {
                return p_eOverrideColor;
            }

            return (Ont.E_PlayerColor)p_a_nGrid[p_nX, p_nY];
        }

        private static bool IsInBounds(int p_nX, int p_nY, int p_nWidth, int p_nHeight)
        {
            return p_nX >= 0 && p_nX < p_nWidth && p_nY >= 0 && p_nY < p_nHeight;
        }

        private void EnsureBufferCapacity(int p_nCellCount)
        {
            if (m_a_nVisitedStamp.Length < p_nCellCount)
            {
                m_a_nVisitedStamp = new int[p_nCellCount];
                m_a_nLibertyStamp = new int[p_nCellCount];
                m_nVisitedGeneration = 0;
                m_nLibertyGeneration = 0;
            }
        }
    }
}
