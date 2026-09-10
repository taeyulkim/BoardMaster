namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// War(전쟁 카드 게임)을 실제로 진행하는 오케스트레이터입니다. GoGameSession과 같은 핵심 인프라
    /// (ActionDispatcher, GamePhaseManager, Action/ICondition/IEffect)를 그대로 재사용하지만, 완전히
    /// 다른 방식으로 씁니다:
    ///   - War는 플레이어가 선택할 게 전혀 없는 게임입니다(양쪽 다 그냥 다음 카드를 낼 뿐). 그래서
    ///     PlayRound()에는 좌표나 선택지 파라미터가 없고, 조립하는 Action에는 ICondition이 하나도
    ///     없습니다(mv_lisConditions가 비어 있으면 Action.Validate()는 항상 true). 조립식 ACE 구조가
    ///     "이번엔 검증할 게 없다"는 장르에도 자연스럽게 들어맞는다는 걸 보여주는 지점입니다.
    ///   - Go는 반상(int[,])으로 상태를 표현하지만 War는 Zone/Entity 그래프로 표현합니다 — 카드가
    ///     Deck/Table/WarPool/Pile Zone 사이를 실제로 옮겨 다닙니다(Effect들이 Entity.mv_objLocatedZone을
    ///     바꿉니다). GameContext/Action/ICondition/IEffect/ActionDispatcher/GamePhaseManager 쪽은
    ///     한 줄도 안 건드리고 이 새 장르를 완전히 다른 표현 방식으로 얹었습니다.
    /// </summary>
    public sealed class WarGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;
        private readonly Effect_DrawTopCards m_objDrawEffect;

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        /// <summary>
        /// 가장 최근 PlayRound() 호출이 실제로 공개한 카드와 승자 요약입니다. 아직 한 번도 라운드를
        /// 진행하지 않았으면 null입니다.
        /// </summary>
        public WarRoundResult? LastRoundResult { get; private set; }

        public WarGameSession(Ont.GameContext p_objInitialContext, Random p_objRandom, OntDyn.IActionLogger? p_objLogger = null)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));

            if (p_objRandom is null)
            {
                throw new ArgumentNullException(nameof(p_objRandom));
            }

            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);
            m_objDrawEffect = new Effect_DrawTopCards(p_objRandom);

            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new WarMainPlayPhase(), new WarGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        public int CountCardsOwnedBy(Ont.E_PlayerColor p_eColor)
        {
            return WarZoneQuery.CountEntitiesOwnedBy(mv_objCurrentContext, p_eColor);
        }

        /// <summary>
        /// 한 라운드(양쪽 카드 공개 -> 비교 -> 승자 수거 -> 종국 판정)를 자동으로 진행합니다.
        ///
        /// 공개(Effect_DrawTopCards)와 정산(Effect_ResolveComparison + Effect_CheckWarGameOver)을
        /// 일부러 두 번의 개별 Dispatch로 나눴습니다 — 정산이 끝나면 Table Zone이 곧바로 비워지므로,
        /// 그 사이에 한 번 멈춰서 Table에 실제로 어떤 카드가 올라왔는지 관찰해 두지 않으면
        /// LastRoundResult(UI가 "누가 무슨 카드를 냈는지" 보여주는 데 필요)를 만들 방법이 없습니다.
        /// 두 Dispatch 사이에 phase 전이는 없습니다(m_objPhaseManager.Update는 맨 끝에 한 번만
        /// 호출됩니다) — 그래서 게임 규칙 자체는 예전처럼 "한 라운드 = 하나의 원자적 흐름"과
        /// 동일하게 동작하고, 달라지는 건 이력에 라운드당 Action이 하나가 아니라 둘 남는다는
        /// 것뿐입니다(War는 기보를 쓰지 않으므로 무해합니다).
        /// </summary>
        public Ont.GameContext PlayRound()
        {
            if (m_objPhaseManager.CurrentPhase is not WarMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 라운드를 진행할 수 없습니다.");
            }

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, CreateDrawAction());

            List<Ont.Entity> lisTableCards = WarZoneQuery.FindEntitiesInZone(mv_objCurrentContext, WarZoneId.Table);
            string? strBlackRank = FindCardRank(lisTableCards, Ont.E_PlayerColor.Black);
            string? strWhiteRank = FindCardRank(lisTableCards, Ont.E_PlayerColor.White);

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, CreateResolveAction());

            LastRoundResult = BuildRoundResult(strBlackRank, strWhiteRank);
            m_objPhaseManager.Update(mv_objCurrentContext);

            return mv_objCurrentContext;
        }

        /// <summary>
        /// 게임이 끝날 때까지(또는 p_nMaxRounds에 도달할 때까지) 라운드를 반복합니다. 실제로 무한
        /// 루프가 될 수는 없지만(카드는 매 라운드 최소 한 장 이상 승자에게 넘어가므로 유한하게
        /// 수렴합니다), 방어적으로 상한을 둡니다.
        /// </summary>
        public Ont.GameContext PlayUntilGameOver(int p_nMaxRounds = 10000)
        {
            int nRounds = 0;

            while (CurrentPhaseName != "GameOver" && nRounds < p_nMaxRounds)
            {
                PlayRound();
                nRounds++;
            }

            return mv_objCurrentContext;
        }

        private DomainAction CreateDrawAction()
        {
            DomainAction objAction = new DomainAction(
                "Action_DrawTopCards",
                new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));

            objAction.mv_lisEffects.Add(m_objDrawEffect);
            return objAction;
        }

        private static DomainAction CreateResolveAction()
        {
            DomainAction objAction = new DomainAction(
                "Action_ResolveRound",
                new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));

            // 순서 중요: 비교 결과를 보고서야(Effect_ResolveComparison) 누가 카드를 다 잃었는지
            // 판단할 수 있다(Effect_CheckWarGameOver).
            objAction.mv_lisEffects.Add(new Effect_ResolveComparison());
            objAction.mv_lisEffects.Add(new Effect_CheckWarGameOver());

            return objAction;
        }

        private static string? FindCardRank(List<Ont.Entity> p_lisCards, Ont.E_PlayerColor p_eColor)
        {
            foreach (Ont.Entity objCard in p_lisCards)
            {
                if (objCard.mv_eColor == p_eColor)
                {
                    return objCard.mv_strType;
                }
            }

            return null;
        }

        private static WarRoundResult BuildRoundResult(string? p_strBlackRank, string? p_strWhiteRank)
        {
            if (p_strBlackRank is null || p_strWhiteRank is null)
            {
                return new WarRoundResult(p_strBlackRank, p_strWhiteRank, p_bWasWar: false, p_eWinner: null);
            }

            int nBlackRank = int.Parse(p_strBlackRank);
            int nWhiteRank = int.Parse(p_strWhiteRank);

            if (nBlackRank == nWhiteRank)
            {
                return new WarRoundResult(p_strBlackRank, p_strWhiteRank, p_bWasWar: true, p_eWinner: null);
            }

            Ont.E_PlayerColor eWinner = nBlackRank > nWhiteRank ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
            return new WarRoundResult(p_strBlackRank, p_strWhiteRank, p_bWasWar: false, p_eWinner: eWinner);
        }
    }
}
