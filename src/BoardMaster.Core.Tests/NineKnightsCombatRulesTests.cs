namespace BoardMaster.Core.Tests
{
    using NK = BoardMaster.Core.Rules.NineKnights;

    internal static class NineKnightsCombatRulesTests
    {
        public static void AttackerWins_SameNumber_AttackerWins()
        {
            Assert.IsTrue(NK.NineKnightsCombatRules.AttackerWins(5, 5, 3), "같은 숫자끼리 부딪히면 공격한 쪽이 이겨야 한다");
        }

        public static void AttackerWins_OneBeatsNine_RegardlessOfRole()
        {
            Assert.IsTrue(NK.NineKnightsCombatRules.AttackerWins(1, 9, 3), "1이 9를 공격하면 1이 이겨야 한다");
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(9, 1, 3), "9가 1을 공격해도 1이 이겨야 한다(9가 져야 한다)");
        }

        public static void AttackerWins_AdjacentDifference_LowerNumberWins()
        {
            Assert.IsTrue(NK.NineKnightsCombatRules.AttackerWins(3, 4, 9), "3이 4를 공격하면 낮은 쪽인 3이 이겨야 한다");
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(4, 3, 9), "4가 3을 공격해도 낮은 쪽인 3이 이겨야 한다(4가 져야 한다)");
        }

        public static void AttackerWins_NonAdjacentDifference_HigherNumberWins()
        {
            Assert.IsTrue(NK.NineKnightsCombatRules.AttackerWins(7, 3, 9), "차이가 1보다 크면 높은 쪽인 7이 이겨야 한다");
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(3, 7, 9), "공격자가 낮아도 차이가 크면 방어자(7)가 이겨야 한다");
        }

        public static void AttackerWins_EightIsInvincible_ExceptSevenAndHiddenToken()
        {
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(2, 8, 9), "8은 인접(7)도 히든도 아닌 숫자에는 지지 않아야 한다");
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(9, 8, 1), "8은 9(인접, 낮은 쪽인 8이 이김)에도 지지 않아야 한다");
            Assert.IsTrue(NK.NineKnightsCombatRules.AttackerWins(7, 8, 9), "7이 8을 공격하면(인접, 낮은 쪽) 7이 이겨야 한다");
        }

        public static void AttackerWins_HiddenTokenBeatsEight_EvenWhenNormalRuleWouldLose()
        {
            // 3이 일반 규칙대로면 8에게 진다(차이가 5로 인접이 아니라 높은 쪽인 8이 이김) — 하지만
            // 공격자의 히든 토큰 번호가 3이면 예외적으로 3이 이겨야 한다.
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(3, 8, 9), "히든 토큰이 아니면 3은 8에게 져야 한다");
            Assert.IsTrue(NK.NineKnightsCombatRules.AttackerWins(3, 8, 3), "히든 토큰이 3이면 3이 8을 이겨야 한다");
        }

        public static void AttackerWins_HiddenTokenOnlyAppliesAgainstEight()
        {
            // 히든 토큰이 3이어도, 상대가 8이 아니면 평범한 상성표를 따라야 한다(3 vs 6은 일반 규칙상 6 승).
            Assert.IsTrue(!NK.NineKnightsCombatRules.AttackerWins(3, 6, 3), "히든 토큰은 8을 상대할 때만 적용되어야 한다");
        }

        public static void AttackerWins_FullWeaknessTable_MatchesPublishedList()
        {
            // 각 번호별 약점(그 번호가 지는 상대 번호 목록) — namu.wiki 기술 내용과 교차검증한 표.
            Dictionary<int, int[]> dicWeaknesses = new()
            {
                [1] = new[] { 3, 4, 5, 6, 7, 8 },
                [2] = new[] { 1, 4, 5, 6, 7, 8, 9 },
                [3] = new[] { 2, 5, 6, 7, 8, 9 },
                [4] = new[] { 3, 6, 7, 8, 9 },
                [5] = new[] { 4, 7, 8, 9 },
                [6] = new[] { 5, 8, 9 },
                [7] = new[] { 6, 9 },
                [8] = new[] { 7 }, // 히든 토큰 무관 조합만 쓰므로(hidden=99) 원래 약점 목록의 "히든" 항목은 제외한다.
                [9] = new[] { 8, 1 }
            };

            foreach ((int nDefender, int[] a_nAttackersWhoWin) in dicWeaknesses)
            {
                for (int nAttacker = 1; nAttacker <= 9; nAttacker++)
                {
                    if (nAttacker == nDefender)
                    {
                        continue; // 동률(공격자 승)은 이 표의 범위 밖이라 별도 테스트에서 다룬다.
                    }

                    bool bExpectedAttackerWins = Array.IndexOf(a_nAttackersWhoWin, nAttacker) >= 0;
                    bool bActualAttackerWins = NK.NineKnightsCombatRules.AttackerWins(nAttacker, nDefender, 99 /* 히든 토큰과 무관한 조합만 검증 */);

                    Assert.AreEqual(
                        bExpectedAttackerWins, bActualAttackerWins,
                        $"공격자 {nAttacker} vs 방어자 {nDefender}: 기대={bExpectedAttackerWins}, 실제={bActualAttackerWins}");
                }
            }
        }
    }
}
