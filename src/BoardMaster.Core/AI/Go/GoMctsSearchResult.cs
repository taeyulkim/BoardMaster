namespace BoardMaster.Core.AI.Go
{
    /// <summary>
    /// GoMctsSearcher.Search 한 번의 전체 결과입니다. 최선의 수만 필요하면 FindBestMove를 쓰면 되고,
    /// 이건 "왜 그 수를 골랐는지"를 보여주고 싶을 때(예: UI의 후보수 통계 패널) 씁니다.
    /// </summary>
    public sealed class GoMctsSearchResult
    {
        public (int X, int Y, bool IsPass)? BestMove { get; }
        public IReadOnlyList<GoMctsCandidateStat> CandidateMoves { get; }

        public GoMctsSearchResult((int X, int Y, bool IsPass)? p_stBestMove, IReadOnlyList<GoMctsCandidateStat> p_lisCandidateMoves)
        {
            BestMove = p_stBestMove;
            CandidateMoves = p_lisCandidateMoves;
        }
    }
}
