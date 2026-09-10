namespace BoardMaster.Core.Ontology.Dynamic
{
    /// <summary>
    /// IGamePhase 시퀀스를 순서대로 진행시키는 범용 페이즈 상태 머신입니다. 장르에 대해 아무것도
    /// 모르며, 미리 정해진 IGamePhase 목록만으로 동작합니다.
    ///
    /// GameContext는 필드로 저장하지 않고 Start()/Update() 호출마다 파라미터로 받습니다 — 이
    /// 엔진 전반의 관례(Action.Execute, ActionDispatcher.Dispatch 등)가 상태 전이마다 새 GameContext
    /// 인스턴스를 반환하는 불변 스냅샷 방식이라, 호출자가 참조를 계속 교체하기 때문입니다. 생성자에서
    /// GameContext를 붙들어 두면 호출자가 새 인스턴스로 갈아탄 뒤에도 이 머신은 옛 스냅샷만 계속
    /// 들여다보게 되어 mv_isGameOver 같은 변경을 영영 못 보는 버그가 됩니다(ICondition.IsSatisfied,
    /// IEffect.Apply가 GameContext를 필드가 아니라 파라미터로 받는 것과 같은 이유입니다).
    ///
    /// 생명주기: Start(ctx)가 첫 페이즈에 진입하고, Update(ctx)를 호출할 때마다 현재 페이즈의
    /// OnPhaseUpdate를 한 번 실행합니다. Start()/Update() 모두 그 직후 IsPhaseCompleted를 확인해,
    /// 완료된 페이즈는 OnPhaseExit → 다음 페이즈 OnPhaseEnter 순으로 즉시 전이시킵니다(한 번의 호출로
    /// 여러 페이즈가 연쇄적으로 이미 완료 상태라면 안정될 때까지 전부 진행합니다).
    /// 마지막 페이즈까지 완료되면 IsFinished가 true가 되고 더 이상 아무 페이즈도 실행되지 않습니다.
    /// </summary>
    public sealed class GamePhaseManager
    {
        private readonly IReadOnlyList<IGamePhase> m_lisPhases;
        private int m_nCurrentPhaseIndex;
        private bool m_bHasStarted;

        public GamePhaseManager(IReadOnlyList<IGamePhase> p_lisPhases)
        {
            if (p_lisPhases is null || p_lisPhases.Count == 0)
            {
                throw new ArgumentException("페이즈 목록은 최소 1개 이상이어야 합니다.", nameof(p_lisPhases));
            }

            m_lisPhases = p_lisPhases;
        }

        public bool IsFinished => m_nCurrentPhaseIndex >= m_lisPhases.Count;

        public IGamePhase CurrentPhase
        {
            get
            {
                if (IsFinished)
                {
                    throw new InvalidOperationException("모든 페이즈가 이미 종료되었습니다.");
                }

                return m_lisPhases[m_nCurrentPhaseIndex];
            }
        }

        /// <summary>
        /// 첫 페이즈에 진입합니다. 한 인스턴스당 한 번만 호출할 수 있습니다.
        /// </summary>
        public void Start(GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (m_bHasStarted)
            {
                throw new InvalidOperationException("이미 시작된 페이즈 상태 머신입니다.");
            }

            m_bHasStarted = true;
            CurrentPhase.OnPhaseEnter(p_objContext);
            AdvanceWhileCompleted(p_objContext);
        }

        /// <summary>
        /// 현재 페이즈의 OnPhaseUpdate를 한 번 호출한 뒤, 완료 조건을 만족하면 다음 페이즈로 전이합니다.
        /// 이미 모든 페이즈가 끝났다면 아무 일도 하지 않습니다. p_objContext는 호출 시점의 최신
        /// GameContext여야 합니다 — 오래된 스냅샷을 넘기면 완료 판정이 틀어집니다.
        /// </summary>
        public void Update(GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (!m_bHasStarted)
            {
                throw new InvalidOperationException("Update()보다 먼저 Start()를 호출해야 합니다.");
            }

            if (IsFinished)
            {
                return;
            }

            CurrentPhase.OnPhaseUpdate(p_objContext);
            AdvanceWhileCompleted(p_objContext);
        }

        private void AdvanceWhileCompleted(GameContext p_objContext)
        {
            while (!IsFinished && CurrentPhase.IsPhaseCompleted(p_objContext))
            {
                CurrentPhase.OnPhaseExit(p_objContext);
                m_nCurrentPhaseIndex++;

                if (!IsFinished)
                {
                    CurrentPhase.OnPhaseEnter(p_objContext);
                }
            }
        }
    }
}
