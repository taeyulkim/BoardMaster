namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// PlayRound() 한 번이 실제로 무엇을 했는지 보여주는 요약입니다. Table Zone은 라운드가 끝나면
    /// 곧바로 비워지므로(카드가 승자의 Pile이나 WarPool로 옮겨짐), PlayRound()가 끝난 뒤에는 어느
    /// 카드가 공개됐었는지 GameContext만 봐서는 더 이상 알 수 없습니다 — 그래서 WarGameSession이
    /// 라운드 도중에 직접 관찰해 이 값을 만들어 둡니다(UI가 "누가 무슨 카드를 냈는지" 보여주려면
    /// 이 정보가 꼭 필요합니다).
    /// </summary>
    public sealed class WarRoundResult
    {
        /// <summary>Black이 이번 라운드에 낸 카드의 랭크("2".."14"). Black이 낼 카드가 없었으면 null.</summary>
        public string? BlackCardRank { get; }

        /// <summary>White가 이번 라운드에 낸 카드의 랭크. White가 낼 카드가 없었으면 null.</summary>
        public string? WhiteCardRank { get; }

        /// <summary>두 랭크가 같아 전쟁(War)으로 이어졌는지 — 이번 라운드는 승부가 나지 않고 WarPool에 쌓인다.</summary>
        public bool WasWar { get; }

        /// <summary>이번 라운드에서 카드를 가져간 쪽. 전쟁(비김)이거나 한쪽이 카드를 못 냈으면 null.</summary>
        public Ont.E_PlayerColor? Winner { get; }

        public WarRoundResult(string? p_strBlackCardRank, string? p_strWhiteCardRank, bool p_bWasWar, Ont.E_PlayerColor? p_eWinner)
        {
            BlackCardRank = p_strBlackCardRank;
            WhiteCardRank = p_strWhiteCardRank;
            WasWar = p_bWasWar;
            Winner = p_eWinner;
        }
    }
}
