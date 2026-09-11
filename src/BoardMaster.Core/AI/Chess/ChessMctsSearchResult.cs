namespace BoardMaster.Core.AI.Chess
{
    /// <summary>
    /// ChessMctsSearcher.Search 한 번의 전체 결과입니다. Go의 GoMctsSearchResult와 같은 목적입니다.
    /// </summary>
    public sealed class ChessMctsSearchResult
    {
        public (int FromX, int FromY, int ToX, int ToY)? BestMove { get; }
        public IReadOnlyList<ChessMctsCandidateStat> CandidateMoves { get; }

        public ChessMctsSearchResult((int FromX, int FromY, int ToX, int ToY)? p_stBestMove, IReadOnlyList<ChessMctsCandidateStat> p_lisCandidateMoves)
        {
            BestMove = p_stBestMove;
            CandidateMoves = p_lisCandidateMoves;
        }
    }
}
