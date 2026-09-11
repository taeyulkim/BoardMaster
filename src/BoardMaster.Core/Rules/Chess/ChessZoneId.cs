namespace BoardMaster.Core.Rules.Chess
{
    /// <summary>
    /// 체스는 War/GuryongTu처럼 고정된 Zone 목록이 아니라 8x8=64개의 "칸" Zone을 동적으로 만듭니다.
    /// 이 클래스는 (x, y) 좌표 <-> Zone ID 문자열 변환만 담당합니다. 좌표 자체(어느 칸이 어디
    /// 인접한지, 기물이 얼마나 움직였는지)는 Zone ID 문자열이 아니라 Zone.mv_nX/mv_nY에서 직접
    /// 읽습니다 — 문자열 파싱 없이 기하 연산을 하기 위해서입니다.
    ///
    /// x=0..7은 파일(a..h), y=0..7은 랭크(1..8)에 대응합니다.
    /// </summary>
    internal static class ChessZoneId
    {
        public const string Captured = "Captured";

        public static string Square(int p_nX, int p_nY)
        {
            return $"Sq_{p_nX}_{p_nY}";
        }
    }
}
