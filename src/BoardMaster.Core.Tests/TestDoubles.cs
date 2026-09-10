namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// ICondition을 델리게이트 하나로 즉석에서 구성할 수 있는 테스트 더블입니다.
    /// </summary>
    internal sealed class FakeCondition : Ont.ICondition
    {
        private readonly Func<Ont.GameContext, DomainAction, bool> m_fnPredicate;

        public FakeCondition(Func<Ont.GameContext, DomainAction, bool> p_fnPredicate)
        {
            m_fnPredicate = p_fnPredicate;
        }

        public bool IsSatisfied(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            return m_fnPredicate(p_objContext, p_objAction);
        }
    }

    /// <summary>
    /// IEffect를 델리게이트 하나로 즉석에서 구성할 수 있는 테스트 더블입니다.
    /// </summary>
    internal sealed class FakeEffect : Ont.IEffect
    {
        private readonly Func<Ont.GameContext, DomainAction, Ont.GameContext> m_fnApply;

        public FakeEffect(Func<Ont.GameContext, DomainAction, Ont.GameContext> p_fnApply)
        {
            m_fnApply = p_fnApply;
        }

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            return m_fnApply(p_objContext, p_objAction);
        }
    }

    /// <summary>
    /// ActionDispatcher가 실제로 로그를 남기는지 검증하기 위한 스파이 로거입니다.
    /// </summary>
    internal sealed class SpyActionLogger : OntDyn.IActionLogger
    {
        public List<(string Message, Exception Exception)> mv_lisEntries { get; } = new();

        public void LogError(string p_strMessage, Exception p_objException)
        {
            mv_lisEntries.Add((p_strMessage, p_objException));
        }
    }

    /// <summary>
    /// IGamePhase를 즉석에서 구성할 수 있는 테스트 더블입니다. CompletesAfterUpdateCount로 몇 번째
    /// OnPhaseUpdate 이후 완료 처리할지 조정하고, 각 훅 호출 횟수를 세어 검증에 쓸 수 있게 합니다.
    /// </summary>
    internal sealed class FakeGamePhase : OntDyn.IGamePhase
    {
        public string mv_strPhaseName { get; }
        public int EnterCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int ExitCount { get; private set; }
        public int CompletesAfterUpdateCount { get; set; } = int.MaxValue;

        public FakeGamePhase(string p_strName)
        {
            mv_strPhaseName = p_strName;
        }

        public void OnPhaseEnter(Ont.GameContext p_objContext)
        {
            EnterCount++;
        }

        public void OnPhaseUpdate(Ont.GameContext p_objContext)
        {
            UpdateCount++;
        }

        public void OnPhaseExit(Ont.GameContext p_objContext)
        {
            ExitCount++;
        }

        public bool IsPhaseCompleted(Ont.GameContext p_objContext)
        {
            return UpdateCount >= CompletesAfterUpdateCount;
        }
    }

    /// <summary>
    /// 테스트 시나리오 전반에서 재사용하는 최소 GameContext 조립 헬퍼입니다.
    /// </summary>
    internal static class TestFixtures
    {
        public static Ont.GameContext CreateContext(
            int p_nSize = 9,
            int p_nTurnNumber = 1,
            Ont.E_PlayerColor p_eActiveColor = Ont.E_PlayerColor.Black)
        {
            return CreateContext(p_nSize, p_nSize, p_nTurnNumber, p_eActiveColor);
        }

        public static Ont.GameContext CreateContext(
            int p_nWidth,
            int p_nHeight,
            int p_nTurnNumber = 1,
            Ont.E_PlayerColor p_eActiveColor = Ont.E_PlayerColor.Black)
        {
            Ont.ST_BoardState stState = new Ont.ST_BoardState(
                p_nTurnNumber,
                p_eActiveColor,
                new int[p_nWidth, p_nHeight],
                0,
                0);

            return new Ont.GameContext(stState);
        }

        public static void SetGrid(Ont.GameContext p_objContext, int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            p_objContext.mv_stCurrentState.m_a_nBoardGrid[p_nX, p_nY] = (int)p_eColor;
        }
    }
}
