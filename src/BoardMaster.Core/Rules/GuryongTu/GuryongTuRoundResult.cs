namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 한 라운드가 정산된 순간의 요약입니다. War의 WarRoundResult와 같은 이유로 존재합니다 — 정산
    /// Effect가 두 타일을 곧바로 Discard로 치워버리므로, 정산 직후에는 GameContext만 봐서 방금
    /// 어떤 랭크끼리 붙었는지 더 이상 알 수 없습니다.
    /// </summary>
    public sealed class GuryongTuRoundResult
    {
        public int BlackRank { get; }
        public int WhiteRank { get; }

        /// <summary>이번 라운드를 가져간 쪽. 동점이면 null.</summary>
        public Ont.E_PlayerColor? Winner { get; }

        public GuryongTuRoundResult(int p_nBlackRank, int p_nWhiteRank, Ont.E_PlayerColor? p_eWinner)
        {
            BlackRank = p_nBlackRank;
            WhiteRank = p_nWhiteRank;
            Winner = p_eWinner;
        }
    }
}
