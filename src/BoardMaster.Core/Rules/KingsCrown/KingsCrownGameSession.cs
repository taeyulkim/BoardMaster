namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 킹스 크라운(이세돌, WIZSTONE) 한 판을 진행하는 오케스트레이터입니다. 각 플레이어가 아직
    /// 놓지 않고 보유 중인 숫자칩 목록(m_lisPlayer1HeldChips/m_lisPlayer2HeldChips)은 GameContext에
    /// 없는 이 세션만의 비밀 상태입니다(Nine Knights의 비밀 임무/히든 번호와 같은 위치) — 상대의
    /// 목록은 절대 공개되지 않고, 놓는 순간 그 칸의 Entity로만 공개됩니다.
    ///
    /// KingsCrownHeuristicAi는 GetHeldChips(자기 색)와 GetPieceAt 같은 공개 정보만 쓰고, 상대의
    /// 보유 숫자칩은 절대 들여다보지 않습니다 — 정직한(cheating 없는) 탐색을 위한 설계입니다.
    /// </summary>
    public sealed class KingsCrownGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;

        private readonly List<int> m_lisPlayer1HeldChips;
        private readonly List<int> m_lisPlayer2HeldChips;

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        public KingsCrownGameSession(Ont.GameContext p_objInitialContext, Random? p_objRandom = null, OntDyn.IActionLogger? p_objLogger = null)
            : this(p_objInitialContext, RollChips(p_objRandom ?? new Random()), p_objLogger)
        {
        }

        /// <summary>
        /// 48개 숫자칩(1~24 각 2장)에서 12장씩 두 플레이어에게 나눠줍니다 — 생성자 초기화식 안에서
        /// "p_objRandom ?? new Random()"을 여러 번 따로 평가하면 매번 다른 Random 인스턴스가 생길
        /// 위험이 있어서(Nine Knights에서 실제로 겪은 버그), 이렇게 한 번만 해석해 재사용합니다.
        /// </summary>
        private static (List<int> P1, List<int> P2) RollChips(Random p_objRandom)
        {
            List<int> lisPool = new List<int>();
            for (int nNumber = 1; nNumber <= KingsCrownGameFactory.MAX_NUMBER; nNumber++)
            {
                lisPool.Add(nNumber);
                lisPool.Add(nNumber);
            }

            Shuffle(lisPool, p_objRandom);

            List<int> lisPlayer1 = lisPool.GetRange(0, KingsCrownGameFactory.CHIPS_PER_PLAYER);
            List<int> lisPlayer2 = lisPool.GetRange(KingsCrownGameFactory.CHIPS_PER_PLAYER, KingsCrownGameFactory.CHIPS_PER_PLAYER);
            return (lisPlayer1, lisPlayer2);
        }

        private static void Shuffle(List<int> p_lisItems, Random p_objRandom)
        {
            for (int i = p_lisItems.Count - 1; i > 0; i--)
            {
                int j = p_objRandom.Next(i + 1);
                (p_lisItems[i], p_lisItems[j]) = (p_lisItems[j], p_lisItems[i]);
            }
        }

        /// <summary>
        /// 보유 숫자칩을 무작위가 아니라 직접 못박고 싶은 테스트 전용 진입점입니다(internal —
        /// InternalsVisibleTo로 Core.Tests만 접근 가능). 일반 사용처는 위의 Random 기반 공개
        /// 생성자를 씁니다.
        /// </summary>
        internal KingsCrownGameSession(
            Ont.GameContext p_objInitialContext,
            List<int> p_lisPlayer1Chips, List<int> p_lisPlayer2Chips,
            OntDyn.IActionLogger? p_objLogger = null)
            : this(p_objInitialContext, (p_lisPlayer1Chips, p_lisPlayer2Chips), p_objLogger)
        {
        }

        private KingsCrownGameSession(
            Ont.GameContext p_objInitialContext,
            (List<int> P1, List<int> P2) p_stChips,
            OntDyn.IActionLogger? p_objLogger)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));
            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);

            m_lisPlayer1HeldChips = new List<int>(p_stChips.P1);
            m_lisPlayer2HeldChips = new List<int>(p_stChips.P2);

            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new KingsCrownMainPlayPhase(), new KingsCrownGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        /// <summary>이 색이 아직 놓지 않고 보유 중인 숫자칩 값 목록입니다(중복 가능 — 같은 숫자가 2장일 수 있음).</summary>
        public IReadOnlyList<int> GetHeldChips(Ont.E_PlayerColor p_eColor)
        {
            return GetHeldChipsMutable(p_eColor);
        }

        private List<int> GetHeldChipsMutable(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? m_lisPlayer1HeldChips : m_lisPlayer2HeldChips;
        }

        public Ont.Entity? GetPieceAt(int p_nX, int p_nY)
        {
            return KingsCrownZoneQuery.FindPieceAt(mv_objCurrentContext, p_nX, p_nY);
        }

        /// <summary>p_eColor가 지금 놓을 수 있는 모든 (숫자칩 값, 칸) 합법수 쌍입니다.</summary>
        public List<(int ChipValue, int X, int Y)> GetLegalPlacements(Ont.E_PlayerColor p_eColor)
        {
            List<(int ChipValue, int X, int Y)> lisResult = new List<(int ChipValue, int X, int Y)>();
            HashSet<int> setDistinctValues = new HashSet<int>(GetHeldChips(p_eColor));

            foreach (int nValue in setDistinctValues)
            {
                for (int nY = 0; nY < KingsCrownGameFactory.BOARD_SIZE; nY++)
                {
                    for (int nX = 0; nX < KingsCrownGameFactory.BOARD_SIZE; nX++)
                    {
                        if (KingsCrownPlacementRules.CanPlace(mv_objCurrentContext, nX, nY, p_eColor, nValue))
                        {
                            lisResult.Add((nValue, nX, nY));
                        }
                    }
                }
            }

            return lisResult;
        }

        public Ont.GameContext PlaceCrown(int p_nChipValue, int p_nX, int p_nY)
        {
            EnsureMainPlayPhase();

            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            List<int> lisMoverChips = GetHeldChipsMutable(eActiveColor);
            if (!lisMoverChips.Contains(p_nChipValue))
            {
                throw new OntDyn.RuleViolationException("보유하지 않은 숫자칩입니다.");
            }

            DomainAction objAction = CreateAction(eActiveColor, p_nChipValue, p_nX, p_nY);
            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);

            lisMoverChips.Remove(p_nChipValue);

            m_objPhaseManager.Update(mv_objCurrentContext);
            return mv_objCurrentContext;
        }

        private void EnsureMainPlayPhase()
        {
            if (m_objPhaseManager.CurrentPhase is not KingsCrownMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 왕관을 놓을 수 없습니다.");
            }
        }

        private DomainAction CreateAction(Ont.E_PlayerColor p_eActiveColor, int p_nChipValue, int p_nX, int p_nY)
        {
            DomainAction objAction = new DomainAction(
                "Action_PlaceCrown",
                new Ont.ST_ActionData(
                    KingsCrownActionCoding.EncodeSquare(p_nX, p_nY),
                    p_nChipValue,
                    false,
                    p_eActiveColor));

            objAction.mv_lisConditions.Add(new Cond_LegalKingsCrownPlacement());
            objAction.mv_lisEffects.Add(new Effect_PlaceCrown());
            objAction.mv_lisEffects.Add(new Effect_SwitchTurn());

            Ont.E_PlayerColor eOpponentColor = KingsCrownBoardGeometry.Opponent(p_eActiveColor);
            List<int> lisOpponentChipsSnapshot = new List<int>(GetHeldChips(eOpponentColor));
            objAction.mv_lisEffects.Add(new Effect_CheckBingoAndStalemateWin(lisOpponentChipsSnapshot));

            return objAction;
        }
    }
}
