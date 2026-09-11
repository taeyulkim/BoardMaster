namespace BoardMaster.Core.AI.GreatKingdom
{
    /// <summary>Go의 GoMctsCandidateStat과 같은 목적입니다 — 이 장르도 수의 모양(X,Y,IsPass)이 Go와 같습니다.</summary>
    public sealed class GreatKingdomMctsCandidateStat
    {
        public (int X, int Y, bool IsPass) Move { get; }
        public int VisitCount { get; }
        public double WinRate { get; }

        public GreatKingdomMctsCandidateStat((int X, int Y, bool IsPass) p_stMove, int p_nVisitCount, double p_dWinRate)
        {
            Move = p_stMove;
            VisitCount = p_nVisitCount;
            WinRate = p_dWinRate;
        }
    }
}
