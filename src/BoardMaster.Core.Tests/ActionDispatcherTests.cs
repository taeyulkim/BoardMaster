namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    internal static class ActionDispatcherTests
    {
        public static void Dispatch_Throws_RuleViolationException_WhenGameOver()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext();
            objContext.mv_isGameOver = true;
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));
            OntDyn.ActionDispatcher objDispatcher = new OntDyn.ActionDispatcher();

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objDispatcher.Dispatch(objContext, objAction),
                "게임 종료 후 Dispatch는 RuleViolationException을 던져야 한다");
        }

        public static void Dispatch_WrapsValidationFailure_AsRuleViolationException_AndLogs()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext();
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => false));

            SpyActionLogger objSpyLogger = new SpyActionLogger();
            OntDyn.ActionDispatcher objDispatcher = new OntDyn.ActionDispatcher(objSpyLogger);

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objDispatcher.Dispatch(objContext, objAction),
                "검증 실패는 RuleViolationException으로 변환되어야 한다");
            Assert.AreEqual(1, objSpyLogger.mv_lisEntries.Count, "검증 실패는 정확히 한 번 로그로 남아야 한다");
        }

        public static void Dispatch_LogsAndRethrows_WhenEffectThrows_OriginalContextUnaffected()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nTurnNumber: 3);
            DomainAction objAction = new DomainAction("Place", new Ont.ST_ActionData(1, 1, false, Ont.E_PlayerColor.White));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));
            objAction.mv_lisEffects.Add(new FakeEffect((c, a) => throw new NullReferenceException("의도적인 Effect 버그 시뮬레이션")));

            SpyActionLogger objSpyLogger = new SpyActionLogger();
            OntDyn.ActionDispatcher objDispatcher = new OntDyn.ActionDispatcher(objSpyLogger);

            Assert.Throws<NullReferenceException>(
                () => objDispatcher.Dispatch(objContext, objAction),
                "InvalidOperationException이 아닌 예외는 원래 타입 그대로 재전파되어야 한다");
            Assert.AreEqual(1, objSpyLogger.mv_lisEntries.Count, "Effect 실패도 정확히 한 번 로그로 남아야 한다");
            Assert.AreEqual(3, objContext.mv_stCurrentState.m_nTurnNumber, "Dispatch 실패 후에도 원본 컨텍스트는 그대로 유지되어야 한다");
        }

        public static void Dispatch_ReturnsNewContext_OnSuccess()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nTurnNumber: 1);
            DomainAction objAction = new DomainAction("Pass", new Ont.ST_ActionData(0, 0, true, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new FakeCondition((c, a) => true));
            objAction.mv_lisEffects.Add(new FakeEffect((c, a) => c));

            OntDyn.ActionDispatcher objDispatcher = new OntDyn.ActionDispatcher();
            Ont.GameContext objResult = objDispatcher.Dispatch(objContext, objAction);

            Assert.IsTrue(!ReferenceEquals(objContext, objResult), "성공 시 Dispatch는 새 컨텍스트를 반환해야 한다");
            Assert.AreEqual(1, objResult.mv_lisHistory.Count, "성공한 행동은 새 컨텍스트의 히스토리에 남아야 한다");
        }
    }
}
