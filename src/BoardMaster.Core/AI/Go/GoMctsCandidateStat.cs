namespace BoardMaster.Core.AI.Go
{
    /// <summary>
    /// GoMctsSearcher.Search가 반환하는, 루트에서 실제로 펼쳐본 후보 수 하나에 대한 탐색 통계입니다.
    /// WinRate은 그 수를 둔 플레이어(Move를 만든 쪽) 시점에서 추정한 승률(0.0~1.0)입니다.
    /// </summary>
    public sealed class GoMctsCandidateStat
    {
        public (int X, int Y, bool IsPass) Move { get; }
        public int VisitCount { get; }
        public double WinRate { get; }

        public GoMctsCandidateStat((int X, int Y, bool IsPass) p_stMove, int p_nVisitCount, double p_dWinRate)
        {
            Move = p_stMove;
            VisitCount = p_nVisitCount;
            WinRate = p_dWinRate;
        }
    }
}
