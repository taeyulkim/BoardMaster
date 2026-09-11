namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 그레이트 킹덤(이세돌, WIZSTONE 시리즈) 한 판을 실제로 진행하는 오케스트레이터입니다. Go의
    /// GoGameSession과 뼈대가 거의 같습니다(격자 기반, BFS 활로/포획) — 다른 점 세 가지만 이
    /// 장르 전용으로 얹었습니다:
    ///   1. 반상 정중앙에 누구의 것도 아닌 중립 성이 하나 있고, 절대 잡히지 않습니다.
    ///   2. 이미 완성된 상대의 영토에는 착수할 수 없습니다(Cond_NotOpponentTerritory) — 바둑에는
    ///      없는 규칙입니다.
    ///   3. 성이 하나라도 포위되어 잡히면 그 즉시 대국이 끝나고 잡은 쪽이 승리합니다 — 바둑처럼
    ///      계가까지 가지 않습니다. 아무도 안 잡히고 양쪽이 패스로 물러나야만 영토 비교(선공이
    ///      후공보다 3 이상 많아야 선공 승)로 넘어갑니다.
    /// 바둑과 달리 패(Ko)에 해당하는 규칙은 없습니다 — 어차피 첫 포획이 곧바로 대국을 끝내므로
    /// 되따내기를 반복할 기회 자체가 없습니다.
    /// </summary>
    public sealed class GreatKingdomGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;
        private readonly Cond_NotSuicide m_objSuicideCondition = new Cond_NotSuicide();
        private readonly Cond_NotOpponentTerritory m_objTerritoryCondition = new Cond_NotOpponentTerritory();
        private readonly DomainAction m_objLegalityProbeAction = new DomainAction(
            "LegalityProbe", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        public GreatKingdomGameSession(Ont.GameContext p_objInitialContext, OntDyn.IActionLogger? p_objLogger = null)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));
            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);

            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new GreatKingdomMainPlayPhase(), new GreatKingdomGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        /// <summary>
        /// 현재 상태를 완전히 격리된 새 GreatKingdomGameSession으로 복제합니다. Go/Chess의 Clone()과
        /// 같은 목적입니다 — MCTS가 하나의 국면에서 여러 가상의 미래를 서로 간섭 없이 탐색할 때 씁니다.
        /// 이 장르는 세션 전용 파생 상태(패 금지점 같은)가 없으므로 GameContext.Clone()만으로 충분합니다.
        /// </summary>
        public GreatKingdomGameSession Clone()
        {
            return new GreatKingdomGameSession(mv_objCurrentContext.Clone());
        }

        /// <summary>
        /// 현재 활성 플레이어가 지금 합법적으로 둘 수 있는 모든 좌표를 반환합니다(패스는 포함하지 않음).
        /// </summary>
        public List<(int X, int Y)> GetLegalMoves()
        {
            List<(int X, int Y)> lisLegalMoves = new List<(int X, int Y)>();

            if (mv_objCurrentContext.mv_isGameOver)
            {
                return lisLegalMoves;
            }

            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            int[,] a_nGrid = mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            for (int nY = 0; nY < nHeight; nY++)
            {
                for (int nX = 0; nX < nWidth; nX++)
                {
                    if (a_nGrid[nX, nY] != GreatKingdomCell.Empty)
                    {
                        continue;
                    }

                    if (IsLegalPlacement(nX, nY, eActiveColor))
                    {
                        lisLegalMoves.Add((nX, nY));
                    }
                }
            }

            return lisLegalMoves;
        }

        public Ont.GameContext PlaceStone(int p_nX, int p_nY)
        {
            EnsureMainPlayPhase();

            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            DomainAction objAction = CreatePlaceStoneAction(p_nX, p_nY, eActiveColor);

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);
            m_objPhaseManager.Update(mv_objCurrentContext);

            return mv_objCurrentContext;
        }

        public Ont.GameContext Pass()
        {
            EnsureMainPlayPhase();

            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            DomainAction objAction = CreatePassAction(eActiveColor);

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);
            m_objPhaseManager.Update(mv_objCurrentContext);

            return mv_objCurrentContext;
        }

        private bool IsLegalPlacement(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            m_objLegalityProbeAction.mv_stActionData = new Ont.ST_ActionData(p_nX, p_nY, false, p_eColor);
            return m_objTerritoryCondition.IsSatisfied(mv_objCurrentContext, m_objLegalityProbeAction)
                && m_objSuicideCondition.IsSatisfied(mv_objCurrentContext, m_objLegalityProbeAction);
        }

        private void EnsureMainPlayPhase()
        {
            if (m_objPhaseManager.CurrentPhase is not GreatKingdomMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 착수/패스를 진행할 수 없습니다.");
            }
        }

        private static DomainAction CreatePlaceStoneAction(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            DomainAction objAction = new DomainAction(
                "Action_PlaceStone",
                new Ont.ST_ActionData(p_nX, p_nY, false, p_eColor));

            objAction.mv_lisConditions.Add(new Cond_EmptyCell());
            objAction.mv_lisConditions.Add(new Cond_NotOpponentTerritory());
            objAction.mv_lisConditions.Add(new Cond_NotSuicide());

            objAction.mv_lisEffects.Add(new Effect_PlaceStone());
            objAction.mv_lisEffects.Add(new Effect_CaptureStonesAndCheckSiege());
            objAction.mv_lisEffects.Add(new Effect_SwitchTurn());

            return objAction;
        }

        private static DomainAction CreatePassAction(Ont.E_PlayerColor p_eColor)
        {
            DomainAction objAction = new DomainAction(
                "Action_Pass",
                new Ont.ST_ActionData(0, 0, true, p_eColor));

            objAction.mv_lisEffects.Add(new Effect_SwitchTurn());
            objAction.mv_lisEffects.Add(new Effect_CheckConsecutivePassGameEnd());
            objAction.mv_lisEffects.Add(new Effect_FinalizeScore());

            return objAction;
        }
    }
}
