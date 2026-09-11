namespace BoardMaster.Core.Rules.NineKnights
{
    /// <summary>
    /// 나인 나이츠 전투 상성 판정입니다(namu.wiki 기술 및 자체 검증 기준):
    ///   1. 숫자가 같으면 공격한 쪽이 이긴다.
    ///   2. 방어자가 8이고 공격자의 숫자가 공격자 자신의 비밀 히든 토큰 번호와 같으면 공격자가 이긴다
    ///      (8은 원래 무적에 가깝지만, 각 플레이어가 게임 시작 시 비공개로 뽑아 둔 1~5 사이의 숫자
    ///      하나만은 예외적으로 상대의 8을 잡을 수 있다).
    ///   3. 1은 9를 이긴다(9는 1에게 진다) — 유일한 "순환" 예외.
    ///   4. 두 숫자 차이가 정확히 1이면 낮은 숫자가 이긴다.
    ///   5. 그 외에는 숫자가 높은 쪽이 이긴다.
    /// 이 순서 그대로 판정해야 합니다 — 예를 들어 8 vs 9는 규칙 4(차이 1, 낮은 쪽인 8 승리)가
    /// 규칙 5(높은 쪽 승리)보다 먼저 적용되어야 합니다.
    /// </summary>
    internal static class NineKnightsCombatRules
    {
        public static bool AttackerWins(int p_nAttackerNumber, int p_nDefenderNumber, int p_nAttackerHiddenNumber)
        {
            if (p_nAttackerNumber == p_nDefenderNumber)
            {
                return true;
            }

            if (p_nDefenderNumber == 8 && p_nAttackerNumber == p_nAttackerHiddenNumber)
            {
                return true;
            }

            if (p_nAttackerNumber == 1 && p_nDefenderNumber == 9)
            {
                return true;
            }

            if (p_nDefenderNumber == 1 && p_nAttackerNumber == 9)
            {
                return false;
            }

            if (Math.Abs(p_nAttackerNumber - p_nDefenderNumber) == 1)
            {
                return p_nAttackerNumber < p_nDefenderNumber;
            }

            return p_nAttackerNumber > p_nDefenderNumber;
        }
    }
}
