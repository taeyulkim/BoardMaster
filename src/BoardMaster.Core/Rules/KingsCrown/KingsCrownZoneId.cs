namespace BoardMaster.Core.Rules.KingsCrown
{
    /// <summary>
    /// 5x5 반상 칸의 Zone ID를 만드는 헬퍼입니다. 체스/나인 나이츠와 같은 이유로 칸을 동적으로
    /// 만들고 좌표는 Zone.mv_nX/mv_nY에서 직접 읽습니다. 이 장르는 반상 밖(예비/포획) Zone이
    /// 필요 없습니다 — 아직 놓지 않은 숫자칩은 GameContext의 Zone/Entity가 아니라
    /// KingsCrownGameSession만 아는 비공개 상태(각자 보유한 숫자 목록)로 다룹니다.
    /// </summary>
    internal static class KingsCrownZoneId
    {
        public static string Square(int p_nX, int p_nY)
        {
            return $"Sq_{p_nX}_{p_nY}";
        }
    }
}
