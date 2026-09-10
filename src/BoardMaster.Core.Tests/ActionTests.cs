namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    internal static class ActionTests
    {
        public static void Validate_ReturnsFalse_WhenAnyConditionFails()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(1, 1, false, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => false));

            Assert.IsTrue(!objAction.Validate(objContext), "조건 중 하나라도 실패하면 Validate는 false여야 한다");
        }

        public static void Validate_ReturnsTrue_WhenAllConditionsPass()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(1, 1, false, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));

            Assert.IsTrue(objAction.Validate(objContext), "모든 조건이 통과하면 Validate는 true여야 한다");
        }

        public static void Execute_Throws_InvalidOperationException_WhenValidationFails_AndLeavesOriginalContextUntouched()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nTurnNumber: 5);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(1, 1, false, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => false));
            objAction.mv_lisEffects.Add(new FakeEffect((c, a) => throw new InvalidOperationException("이 Effect는 절대 호출되면 안 된다")));

            Assert.Throws<InvalidOperationException>(
                () => objAction.Execute(objContext),
                "검증 실패 시 InvalidOperationException을 던져야 한다");
            Assert.AreEqual(5, objContext.mv_stCurrentState.m_nTurnNumber, "검증 실패 후 원본 컨텍스트의 턴 번호는 변하지 않아야 한다");
            Assert.AreEqual(0, objContext.mv_lisHistory.Count, "검증 실패 후 원본 히스토리에는 아무것도 추가되면 안 된다");
        }

        public static void Execute_ReturnsNewContext_WithEffectsApplied_AndOriginalContextUnaffected()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nTurnNumber: 1);
            Ont.ST_ActionData stData = new Ont.ST_ActionData(2, 3, false, Ont.E_PlayerColor.Black);
            DomainAction objAction = new DomainAction("Place", stData);
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));
            objAction.mv_lisEffects.Add(new FakeEffect((c, a) =>
            {
                c.mv_stCurrentState.m_a_nBoardGrid[a.mv_stActionData.m_nX, a.mv_stActionData.m_nY] = (int)Ont.E_PlayerColor.Black;
                return c;
            }));

            Ont.GameContext objNewContext = objAction.Execute(objContext);

            Assert.IsTrue(!ReferenceEquals(objContext, objNewContext), "Execute는 원본과 다른 인스턴스를 반환해야 한다");
            Assert.AreEqual((int)Ont.E_PlayerColor.Black, objNewContext.mv_stCurrentState.m_a_nBoardGrid[2, 3], "새 컨텍스트에는 Effect가 반영되어야 한다");
            Assert.AreEqual(0, objContext.mv_stCurrentState.m_a_nBoardGrid[2, 3], "원본 컨텍스트의 보드 그리드는 오염되면 안 된다");
            Assert.AreEqual(1, objNewContext.mv_lisHistory.Count, "새 컨텍스트의 히스토리에는 실행된 행동이 기록되어야 한다");
            Assert.AreEqual(0, objContext.mv_lisHistory.Count, "원본 컨텍스트의 히스토리는 변하면 안 된다");
        }

        public static void Execute_RollsBack_WhenEffectThrows_OriginalContextUnaffected()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nTurnNumber: 7);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.White));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));
            objAction.mv_lisEffects.Add(new FakeEffect((c, a) =>
            {
                c.mv_stCurrentState.m_a_nBoardGrid[0, 0] = 99; // 클론 위에서만 변경됨
                throw new InvalidOperationException("시뮬레이션된 Effect 실패");
            }));

            Assert.Throws<InvalidOperationException>(
                () => objAction.Execute(objContext),
                "체인 도중 Effect가 던지면 예외가 전파되어야 한다");
            Assert.AreEqual(7, objContext.mv_stCurrentState.m_nTurnNumber, "Effect 실패 후에도 원본 턴 번호는 유지되어야 한다");
            Assert.AreEqual(0, objContext.mv_stCurrentState.m_a_nBoardGrid[0, 0], "Effect 실패 후에도 원본 보드 그리드는 오염되지 않아야 한다");
        }
    }
}
