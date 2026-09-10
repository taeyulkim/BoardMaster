namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// boardmaster-unit-test-guide.md 2장의 5개 xUnit 시나리오를 실제 BoardMaster.Core API로 번역한 버전입니다.
    /// 가이드의 예시 코드는 뮤터블 객체 초기화 구문(`new GameContext { mv_lisPlayers = ... }`, `AddCondition()`)을
    /// 전제로 하지만, 실제 구현은 null-safety를 위해 필수 필드를 생성자로 강제하고 ST_BoardState를
    /// readonly struct로 만들어 진짜 불변성을 보장합니다. 따라서 여기서는 문법만 실제 API에 맞게 바꾸고,
    /// 가이드가 검증하려던 불변성(값 타입 복사, Validate 성공/실패, Execute 성공 시 전이·롤백)은 그대로 유지했습니다.
    /// 테스트 이름(Test_01~05)과 시나리오 순서는 가이드와 1:1 대응되도록 유지했습니다.
    /// </summary>
    internal static class BoardMasterCoreRuntimeTests
    {
        /// <summary>
        /// 가이드의 CreateDefaultTestContext()에 대응. 생성자 기반 API로 동일한 초기 상태를 조립합니다.
        /// </summary>
        private static Ont.GameContext CreateDefaultTestContext()
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(
                1,
                Ont.E_PlayerColor.Black,
                new int[19, 19],
                0,
                0);

            Ont.GameContext objContext = new Ont.GameContext(stInitialState);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_A", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_B", Ont.E_PlayerColor.White));

            return objContext;
        }

        public static void Test_01_ValueType_ST_BoardState_Immutability()
        {
            Ont.ST_BoardState stStateA = new Ont.ST_BoardState(10, Ont.E_PlayerColor.Black, new int[1, 1], 0, 0);

            // 값 형식 복사. readonly struct라 필드를 그 자리에서 고칠 수 없으므로,
            // 가이드의 "stStateB.m_nTurnNumber = 20" 대입은 새 인스턴스 생성으로 번역한다.
            Ont.ST_BoardState stStateB = stStateA;
            stStateB = new Ont.ST_BoardState(20, Ont.E_PlayerColor.White, stStateB.m_a_nBoardGrid, stStateB.m_nBlackPrisoners, stStateB.m_nWhitePrisoners);

            Assert.AreEqual(10, stStateA.m_nTurnNumber, "원본(stStateA)은 복사본 수정의 영향을 받지 않아야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.Black, stStateA.m_eActiveColor, "원본(stStateA)의 색상도 보존되어야 한다");
            Assert.AreEqual(20, stStateB.m_nTurnNumber, "복사본(stStateB)에는 새 값이 반영되어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.White, stStateB.m_eActiveColor, "복사본(stStateB)의 색상도 새 값이어야 한다");
        }

        public static void Test_02_Action_Validation_ShouldPass_WhenAllConditionsMet()
        {
            Ont.GameContext objContext = CreateDefaultTestContext();
            DomainAction objAction = new DomainAction("Action_PlayStone", new Ont.ST_ActionData(3, 4, false, Ont.E_PlayerColor.Black));

            objAction.mv_lisConditions.Add(new MockCondition(true));
            objAction.mv_lisConditions.Add(new MockCondition(true));

            bool bIsSuccess = objAction.Validate(objContext);

            Assert.IsTrue(bIsSuccess, "등록된 모든 사전 룰 검증을 충족했을 때 Validate 결과는 true여야 합니다");
        }

        public static void Test_03_Action_Validation_ShouldFail_WhenAnyConditionFails()
        {
            Ont.GameContext objContext = CreateDefaultTestContext();
            DomainAction objAction = new DomainAction("Action_PlayStone", new Ont.ST_ActionData(3, 4, false, Ont.E_PlayerColor.Black));

            objAction.mv_lisConditions.Add(new MockCondition(true));
            objAction.mv_lisConditions.Add(new MockCondition(false)); // 이 조건으로 인해 실패해야 함

            bool bIsSuccess = objAction.Validate(objContext);

            Assert.IsTrue(!bIsSuccess, "검증 리스트 중 단 하나의 조건이라도 false를 반환하면 행동은 위법(false) 처리되어야 합니다");
        }

        public static void Test_04_Action_Execute_ShouldAdvanceState_OnSuccess()
        {
            Ont.GameContext objContext = CreateDefaultTestContext();
            DomainAction objAction = new DomainAction("Action_IncrementTurn", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));

            objAction.mv_lisConditions.Add(new MockCondition(true));
            objAction.mv_lisEffects.Add(new MockIncrementTurnEffect()); // 성공 시 턴 수를 1 올리는 효과

            Ont.GameContext objNextContext = objAction.Execute(objContext);

            Assert.AreEqual(1, objContext.mv_stCurrentState.m_nTurnNumber, "원본 컨텍스트 상태는 보존되어야 한다");
            Assert.AreEqual(2, objNextContext.mv_stCurrentState.m_nTurnNumber, "반환된 컨텍스트는 상태 전이가 완료되어야 한다");
        }

        public static void Test_05_Action_Execute_ShouldThrowException_AndRollback_OnFailure()
        {
            Ont.GameContext objContext = CreateDefaultTestContext();
            int nOriginalTurnNumber = objContext.mv_stCurrentState.m_nTurnNumber;

            DomainAction objAction = new DomainAction("Action_FailedTurn", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new MockCondition(false)); // 무조건 거부될 검증 필터 주입
            objAction.mv_lisEffects.Add(new MockIncrementTurnEffect());

            InvalidOperationException objException = Assert.Throws<InvalidOperationException>(
                () => objAction.Execute(objContext),
                "검증 실패 시 InvalidOperationException이 발생해야 합니다");

            Assert.AreEqual(nOriginalTurnNumber, objContext.mv_stCurrentState.m_nTurnNumber, "원본 컨텍스트는 롤백되어 이전 턴 수를 유지해야 한다");
            Assert.IsTrue(objException.Message.Contains("동작 실행이 거부되었습니다"), "예외 메시지에 거부 사유 문구가 포함되어야 한다");
        }
    }

    /// <summary>
    /// 외부 주입식으로 항상 성공/실패 여부를 조정할 수 있는 모의 조건 검증기 (가이드의 MockCondition 번역).
    /// </summary>
    internal sealed class MockCondition : Ont.ICondition
    {
        private readonly bool m_isPass;

        public MockCondition(bool p_isPass)
        {
            m_isPass = p_isPass;
        }

        public bool IsSatisfied(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            return m_isPass;
        }
    }

    /// <summary>
    /// 상태 전이가 실제로 일어나는지 검증하기 위해 턴 수만 강제로 올리는 모의 효과 (가이드의 MockIncrementTurnEffect 번역).
    /// ST_BoardState가 readonly struct이므로, 필드를 그 자리에서 증가시키는 대신 새 값으로 struct를 재구성한다.
    /// </summary>
    internal sealed class MockIncrementTurnEffect : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            Ont.ST_BoardState stCurrent = p_objContext.mv_stCurrentState;
            Ont.ST_BoardState stNextState = new Ont.ST_BoardState(
                stCurrent.m_nTurnNumber + 1,
                stCurrent.m_eActiveColor,
                stCurrent.m_a_nBoardGrid,
                stCurrent.m_nBlackPrisoners,
                stCurrent.m_nWhitePrisoners);

            p_objContext.mv_stCurrentState = stNextState;
            return p_objContext;
        }
    }
}
