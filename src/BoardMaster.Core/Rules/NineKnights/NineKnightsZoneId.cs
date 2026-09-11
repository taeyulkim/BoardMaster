namespace BoardMaster.Core.Rules.NineKnights
{
    /// <summary>
    /// 체스와 같은 이유(칸이 64/81개라 War/GuryongTu식 고정 Zone 목록으로는 부족)로, 9x9 칸을
    /// 동적으로 만들고 좌표는 Zone.mv_nX/mv_nY에서 직접 읽습니다. 예비(아직 배치 안 한) 기물은
    /// 플레이어별 예비 Zone에 둡니다 — 좌표가 의미 없으므로 (-1,-1)에 둡니다.
    /// </summary>
    internal static class NineKnightsZoneId
    {
        public const string ReservePlayer1 = "Reserve_Player1";
        public const string ReservePlayer2 = "Reserve_Player2";
        public const string Captured = "Captured";

        public static string Square(int p_nX, int p_nY)
        {
            return $"Sq_{p_nX}_{p_nY}";
        }
    }
}
