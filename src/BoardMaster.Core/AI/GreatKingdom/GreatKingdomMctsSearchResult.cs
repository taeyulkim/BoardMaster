namespace BoardMaster.Core.AI.GreatKingdom
{
    /// <summary>Go의 GoMctsSearchResult와 같은 목적입니다.</summary>
    public sealed class GreatKingdomMctsSearchResult
    {
        public (int X, int Y, bool IsPass)? BestMove { get; }
        public IReadOnlyList<GreatKingdomMctsCandidateStat> CandidateMoves { get; }

        public GreatKingdomMctsSearchResult((int X, int Y, bool IsPass)? p_stBestMove, IReadOnlyList<GreatKingdomMctsCandidateStat> p_lisCandidateMoves)
        {
            BestMove = p_stBestMove;
            CandidateMoves = p_lisCandidateMoves;
        }
    }
}
