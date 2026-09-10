namespace BoardMaster.Core.Rules.War
{
    /// <summary>
    /// War(전쟁 카드 게임)이 쓰는 고정 Zone ID들입니다. 문자열을 여기저기 하드코딩하면서 오타로
    /// 틀리는 걸 막기 위해 상수로 모아뒀습니다.
    /// </summary>
    internal static class WarZoneId
    {
        public const string DeckBlack = "Deck_Black";
        public const string DeckWhite = "Deck_White";
        public const string PileBlack = "Pile_Black";
        public const string PileWhite = "Pile_White";
        public const string Table = "Table";
        public const string WarPool = "WarPool";
    }
}
