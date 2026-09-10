# [가이드라인] BoardMaster 코어 1~2단계 검증을 위한 xUnit 단위 테스트
**BoardMaster Core - Phase 1 & 2 Verification via xUnit (v1.0)**

본 가이드는 **BoardMaster** 엔진의 핵심 도메인 모델(Phase 1)과 ACE 규칙 처리 및 액션 디스패처(Phase 2)의 완벽한 런타임 신뢰성을 보장하기 위한 **단위 테스트(Unit Test) 설계 및 C# xUnit 실전 코드**를 제시합니다.

이 테스트 세트는 다른 AI 코딩 에이전트에게 뼈대 코드 구현을 위임할 때 함께 참조(Context Injection)시키면, 아키텍처 규칙과 헝가리안 명명 가이드라인을 극도로 정밀하게 준수한 프로덕션 레벨 코드를 얻어내는 강력한 '품질 게이트' 역할을 수행합니다.

---

## 🏛️ 1. 단위 테스트 설계 사상 (Test Design Philosophy)

BoardMaster 코어 엔진의 단위 테스트는 다음 3가지 핵심 불변성을 증명하는 데 집중합니다:

1. **상태 불변성 (Immutability Verification):** 
   * `ST_BoardState`와 `ST_ActionData`와 같은 경량 상태 정보가 구조체(Struct)로서 복사-전이(Value Semantics)될 때 원본 메모리가 무관하게 보존되는지 검증합니다.
2. **원자성 및 자동 롤백 (Atomic Rollback Verification):**
   * 액션 실행 과정 중 단 하나의 `ICondition`이라도 통과하지 못하면, `GameContext` 전체가 상태 전이 중간에 오염되지 않고 실행 직전의 상태로 완벽히 복구(Rollback)되는지 검증합니다.
3. **조립식 ACE 다형성 (Modular Rule Composition):**
   * Mocking 기법을 사용하여 엔진 코드를 수정하지 않고 다양한 조건(`ICondition`)과 효과(`IEffect`)가 동적으로 주입 및 평가되는지 검증합니다.

---

## 🧪 2. xUnit 단위 테스트 클래스 완전 구현 (C#)

설계서의 명명 규칙(헝가리안 및 파스칼 표기법)을 완벽히 준수하여 작성된 검증용 테스트 슈트입니다.

```csharp
using System;
using System.Collections.Generic;
using Xunit;
using BoardMaster.Core.Ontology;

namespace BoardMaster.Core.Tests
{
    // ==========================================================
    // 1. 테스트 유연성을 확보하기 위한 Mock 규칙 정의 (ICondition, IEffect)
    // ==========================================================

    /// <summary>
    /// 외부 주입식으로 항상 성공/실패 여부를 조정할 수 있는 모의 조건 검증기
    /// </summary>
    public class MockCondition : ICondition
    {
        private readonly bool m_isPass;

        public MockCondition(bool p_isPass)
        {
            m_isPass = p_isPass;
        }

        public bool IsSatisfied(GameContext p_objContext, Action p_objAction)
        {
            return m_isPass;
        }
    }

    /// <summary>
    /// 상태 전이가 실제로 일어나는지 검증하기 위해, 보드 턴 수만 강제로 올리는 모의 효과 반영기
    /// </summary>
    public class MockIncrementTurnEffect : IEffect
    {
        public GameContext Apply(GameContext p_objContext, Action p_objAction)
        {
            // C# 구조체(Value Type) 복사를 통한 불변성 보존 상태 전이 수행
            ST_BoardState stNextState = p_objContext.mv_stCurrentState;
            stNextState.m_nTurnNumber += 1;

            p_objContext.mv_stCurrentState = stNextState;
            return p_objContext;
        }
    }

    // ==========================================================
    // 2. 실전 xUnit 테스트 클래스
    // ==========================================================

    public class BoardMasterCoreRuntimeTests
    {
        /// <summary>
        /// 테스트 실행 시마다 독립적이고 유효한 초기 GameContext를 생성하는 팩토리 메서드
        /// </summary>
        private GameContext CreateDefaultTestContext()
        {
            ST_BoardState stInitialState = new ST_BoardState
            {
                m_nTurnNumber = 1,
                m_eActiveColor = E_PlayerColor.Black,
                m_a_nBoardGrid = new int[19, 19],
                m_nBlackPrisoners = 0,
                m_nWhitePrisoners = 0
            };

            return new GameContext
            {
                mv_stCurrentState = stInitialState,
                mv_lisPlayers = new List<PlayerState>
                {
                    new PlayerState { mv_strPlayerID = "Player_A", mv_eColor = E_PlayerColor.Black, mv_nPrisonerCount = 0 },
                    new PlayerState { mv_strPlayerID = "Player_B", mv_eColor = E_PlayerColor.White, mv_nPrisonerCount = 0 }
                },
                mv_dicZones = new Dictionary<string, Zone>(),
                mv_lisHistory = new List<ST_ActionData>(),
                mv_isGameOver = false
            };
        }

        [Fact]
        public void Test_01_ValueType_ST_BoardState_Immutability()
        {
            // Arrange
            ST_BoardState stStateA = new ST_BoardState { m_nTurnNumber = 10, m_eActiveColor = E_PlayerColor.Black };
            
            // Act
            ST_BoardState stStateB = stStateA; // 값 형식(Value Type) 깊은 복사 수행
            stStateB.m_nTurnNumber = 20;       // 복사본 수정
            stStateB.m_eActiveColor = E_PlayerColor.White;

            // Assert
            Assert.Equal(10, stStateA.m_nTurnNumber); // 원본은 수정사항의 영향을 받지 않고 보존되어야 함
            Assert.Equal(E_PlayerColor.Black, stStateA.m_eActiveColor);
            Assert.Equal(20, stStateB.m_nTurnNumber);
            Assert.Equal(E_PlayerColor.White, stStateB.m_eActiveColor);
        }

        [Fact]
        public void Test_02_Action_Validation_ShouldPass_WhenAllConditionsMet()
        {
            // Arrange
            GameContext objContext = CreateDefaultTestContext();
            Action objAction = new Action
            {
                mv_strActionType = "Action_PlayStone",
                mv_stActionData = new ST_ActionData { m_nX = 3, m_nY = 4, m_eColor = E_PlayerColor.Black }
            };

            // 복수 개의 성공 조건(ICondition) 등록
            objAction.AddCondition(new MockCondition(true));
            objAction.AddCondition(new MockCondition(true));

            // Act
            bool isSuccess = objAction.Validate(objContext);

            // Assert
            Assert.True(isSuccess, "등록된 모든 사전 룰 검증을 충족했을 때 Validate 결과는 True여야 합니다.");
        }

        [Fact]
        public void Test_03_Action_Validation_ShouldFail_WhenAnyConditionFails()
        {
            // Arrange
            GameContext objContext = CreateDefaultTestContext();
            Action objAction = new Action
            {
                mv_strActionType = "Action_PlayStone",
                mv_stActionData = new ST_ActionData { m_nX = 3, m_nY = 4, m_eColor = E_PlayerColor.Black }
            };

            // 하나라도 실패 조건을 조립하여 주입
            objAction.AddCondition(new MockCondition(true));
            objAction.AddCondition(new MockCondition(false)); // 이 조건으로 인해 실패해야 함

            // Act
            bool isSuccess = objAction.Validate(objContext);

            // Assert
            Assert.False(isSuccess, "검증 리스트 중 단 하나의 조건이라도 False를 반환하면 행동은 위법(False) 처리되어야 합니다.");
        }

        [Fact]
        public void Test_04_Action_Execute_ShouldAdvanceState_OnSuccess()
        {
            // Arrange
            GameContext objContext = CreateDefaultTestContext();
            Action objAction = new Action
            {
                mv_strActionType = "Action_IncrementTurn",
                mv_stActionData = new ST_ActionData { m_isPass = false }
            };

            objAction.AddCondition(new MockCondition(true));
            objAction.AddEffect(new MockIncrementTurnEffect()); // 성공 시 턴 수를 1 올리는 효과

            // Act
            GameContext objNextContext = objAction.Execute(objContext);

            // Assert
            Assert.Equal(1, objContext.mv_stCurrentState.m_nTurnNumber); // 원본 상태 보존
            Assert.Equal(2, objNextContext.mv_stCurrentState.m_nTurnNumber); // 반환된 컨텍스트 상태 전이 완료
        }

        [Fact]
        public void Test_05_Action_Execute_ShouldThrowException_AndRollback_OnFailure()
        {
            // Arrange
            GameContext objContext = CreateDefaultTestContext();
            int nOriginalTurnNumber = objContext.mv_stCurrentState.m_nTurnNumber;

            Action objAction = new Action
            {
                mv_strActionType = "Action_FailedTurn",
                mv_stActionData = new ST_ActionData { m_isPass = false }
            };

            objAction.AddCondition(new MockCondition(false)); // 무조건 거부될 검증 필터 주입
            objAction.AddEffect(new MockIncrementTurnEffect());

            // Act & Assert (예외 발생 여부 테스트)
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                objAction.Execute(objContext);
            });

            // 원본 컨텍스트 데이터가 롤백(이전 원본값)을 정상 보존하고 있는지 최종 2차 검증
            Assert.Equal(nOriginalTurnNumber, objContext.mv_stCurrentState.m_nTurnNumber);
            Assert.Contains("동작 실행이 거부되었습니다.", exception.Message);
        }
    }
}
```

---

## 🎯 3. 코드 구현 AI 연동 프롬프트 주입법 (xUnit 활용 시너지)

구현 단계에서 AI 코딩 에이전트(예: GitHub Copilot, Claude)에게 위의 테스트 코드를 주입하면 완벽한 품질 격리가 수행됩니다. 아래 양식에 맞추어 구현을 위임하세요:

```markdown
[Context]
BoardMaster 코어 엔진의 데이터 모델 및 액션 디스패처에 관한 단위 테스트 스펙(`boardmaster-unit-test-guide.md`)을 수립해 두었습니다.

[Task]
테스트 코드가 100% 에러 없이 컴파일되고 통과할 수 있도록, `boardmaster-static-design-v3.md` 명세를 충족하는 `BoardMaster.Core.Ontology` 네임스페이스 하위의 C# 소스 코드를 생성해 주세요.

[Constraint]
1. 테스트 클래스에서 호출하는 `Validate()`, `Execute()`, `ST_BoardState`, `GameContext` 내부 구조와 완벽히 1:1로 매핑되는 코드여야 합니다.
2. 예외 발생 시 전역 상태가 오염되지 않고 그대로 보존되는 불변 구조체 전이 흐름을 소스 코드 단에 완벽히 구축하세요.
```
