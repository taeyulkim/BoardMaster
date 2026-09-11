namespace BoardMaster.Core.AI.Chess
{
    /// <summary>
    /// ChessMctsSearcher.Search가 반환하는, 루트에서 실제로 펼쳐본 후보 수 하나에 대한 탐색 통계입니다.
    /// Go의 GoMctsCandidateStat과 같은 목적이며, Move가 좌표 하나(X,Y,IsPass) 대신 출발/도착 좌표
    /// 쌍이라는 점만 다릅니다 — 체스는 패스가 없으므로 IsPass에 해당하는 값도 없습니다.
    /// </summary>
    public sealed class ChessMctsCandidateStat
    {
        public (int FromX, int FromY, int ToX, int ToY) Move { get; }
        public int VisitCount { get; }
        public double WinRate { get; }

        public ChessMctsCandidateStat((int FromX, int FromY, int ToX, int ToY) p_stMove, int p_nVisitCount, double p_dWinRate)
        {
            Move = p_stMove;
            VisitCount = p_nVisitCount;
            WinRate = p_dWinRate;
        }
    }
}
