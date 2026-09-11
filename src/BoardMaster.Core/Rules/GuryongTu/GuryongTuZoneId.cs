namespace BoardMaster.Core.Rules.GuryongTu
{
    /// <summary>
    /// 구룡투(九龍鬪)가 쓰는 고정 Zone ID들입니다. War의 WarZoneId와 같은 이유로 상수화했습니다.
    /// </summary>
    internal static class GuryongTuZoneId
    {
        public const string HandBlack = "Hand_Black";
        public const string HandWhite = "Hand_White";
        public const string PendingBlack = "Pending_Black";
        public const string PendingWhite = "Pending_White";
        public const string Discard = "Discard";
    }
}
