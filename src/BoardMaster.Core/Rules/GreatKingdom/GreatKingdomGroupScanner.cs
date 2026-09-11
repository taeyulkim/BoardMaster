namespace BoardMaster.Core.Rules.GreatKingdom
{
    /// <summary>
    /// Go의 GoGroupScanner와 완전히 같은 기법(가상 치환 + 세대 스탬프, zero-alloc 재사용 버퍼)을 쓰는
    /// 동색 그룹 활로 계산기입니다. 독립적으로 다시 둔 이유는 War/GuryongTu/Chess와 같습니다 —
    /// 장르끼리 서로 몰라야(NFR-2) 한쪽만 바뀌어도 다른 쪽이 깨지지 않습니다.
    ///
    /// 격자 값은 Ont.E_PlayerColor가 아니라 GreatKingdomCell의 정수 상수입니다. 이웃 칸이 Empty면
    /// 활로, 내 색과 같으면 그룹에 합류, 그 외(상대 색이거나 중립 성)면 막힌 칸으로 처리합니다 —
    /// 그래서 중립 성은 아무 그룹에도 못 들어가고(시작점으로 스캔을 부르지 않으므로) 다른 그룹의
    /// 활로도 절대 되지 않는, 영구적인 벽처럼 자연스럽게 동작합니다.
    /// </summary>
    internal sealed class GreatKingdomGroupScanner
    {
        private int[] m_a_nVisitedStamp = Array.Empty<int>();
        private int[] m_a_nLibertyStamp = Array.Empty<int>();
        private int m_nVisitedGeneration;
        private int m_nLibertyGeneration;
        private readonly Queue<int> m_quBfsQueue = new Queue<int>();

        internal int CalculateLiberties(
            int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nStartX, int p_nStartY, int p_nGroupCellValue,
            int p_nOverrideX, int p_nOverrideY, int p_nOverrideCellValue)
        {
            return Scan(p_a_nGrid, p_nWidth, p_nHeight, p_nStartX, p_nStartY, p_nGroupCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue, null, out _);
        }

        internal int CollectGroupMembers(
            int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nStartX, int p_nStartY, int p_nGroupCellValue,
            int p_nOverrideX, int p_nOverrideY, int p_nOverrideCellValue, int[] p_a_nOutMemberIndices)
        {
            Scan(p_a_nGrid, p_nWidth, p_nHeight, p_nStartX, p_nStartY, p_nGroupCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue, p_a_nOutMemberIndices, out int nMemberCount);
            return nMemberCount;
        }

        private int Scan(
            int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nStartX, int p_nStartY, int p_nGroupCellValue,
            int p_nOverrideX, int p_nOverrideY, int p_nOverrideCellValue, int[]? p_a_nOutMemberIndices, out int p_nMemberCount)
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

                nLibertyCount += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX - 1, nCurrentY, p_nGroupCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue, p_a_nOutMemberIndices, ref nMemberCount);
                nLibertyCount += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX + 1, nCurrentY, p_nGroupCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue, p_a_nOutMemberIndices, ref nMemberCount);
                nLibertyCount += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY - 1, p_nGroupCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue, p_a_nOutMemberIndices, ref nMemberCount);
                nLibertyCount += VisitNeighbor(p_a_nGrid, p_nWidth, p_nHeight, nCurrentX, nCurrentY + 1, p_nGroupCellValue, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue, p_a_nOutMemberIndices, ref nMemberCount);
            }

            p_nMemberCount = nMemberCount;
            return nLibertyCount;
        }

        private int VisitNeighbor(
            int[,] p_a_nGrid, int p_nWidth, int p_nHeight, int p_nNeighborX, int p_nNeighborY, int p_nGroupCellValue,
            int p_nOverrideX, int p_nOverrideY, int p_nOverrideCellValue, int[]? p_a_nOutMemberIndices, ref int p_nMemberCount)
        {
            if (!IsInBounds(p_nNeighborX, p_nNeighborY, p_nWidth, p_nHeight))
            {
                return 0;
            }

            int nNeighborIndex = (p_nNeighborY * p_nWidth) + p_nNeighborX;
            int nNeighborValue = GetCellValue(p_a_nGrid, p_nNeighborX, p_nNeighborY, p_nOverrideX, p_nOverrideY, p_nOverrideCellValue);

            if (nNeighborValue == GreatKingdomCell.Empty)
            {
                if (m_a_nLibertyStamp[nNeighborIndex] == m_nLibertyGeneration)
                {
                    return 0;
                }

                m_a_nLibertyStamp[nNeighborIndex] = m_nLibertyGeneration;
                return 1;
            }

            if (nNeighborValue == p_nGroupCellValue && m_a_nVisitedStamp[nNeighborIndex] != m_nVisitedGeneration)
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

        private static int GetCellValue(int[,] p_a_nGrid, int p_nX, int p_nY, int p_nOverrideX, int p_nOverrideY, int p_nOverrideCellValue)
        {
            if (p_nX == p_nOverrideX && p_nY == p_nOverrideY)
            {
                return p_nOverrideCellValue;
            }

            return p_a_nGrid[p_nX, p_nY];
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
