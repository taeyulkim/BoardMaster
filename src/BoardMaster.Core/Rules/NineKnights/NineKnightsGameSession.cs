namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 나인 나이츠(이세돌, WIZSTONE) 한 판을 진행하는 오케스트레이터입니다. Chess처럼 "칸을 좌표로
    /// 갖는 Zone + 그 위를 옮겨 다니는 Entity" 표현을 쓰지만, 이 장르만의 특징은 지속적인 은닉
    /// 정보입니다 — 각 기사의 번호는 전투로 맞부딪히기 전까지 상대에게 공개되지 않습니다.
    ///
    /// 비밀 임무 번호(자신의 몇 번 기사가 상대 뒷줄에 닿아야 이기는지)와 히든 토큰 번호(상대의 8을
    /// 잡을 수 있는 나만 아는 번호)는 GameContext에 없는 이 세션만의 비밀 상태입니다(Go의
    /// 패 금지점/Chess의 캐슬링 권리와 같은 위치) — Condition/Effect에는 "지금 두는 플레이어 자신의"
    /// 값만 생성자로 주입되므로, 상대의 비밀은 Effect 쪽에서도 볼 수 없습니다.
    ///
    /// 어느 기사가 "공개됐는지"는 좌표/타입 같은 반상 상태가 아니라 대국 진행상의 지식이라, Go의
    /// 슈퍼코 이력과 같은 방식으로 이 세션이 엔티티 ID 문자열 집합(m_setRevealedEntityIds)으로
    /// 따로 추적합니다 — 실제 착수(Dispatch) 전후로 "그 칸에 있던 기사"를 비교해서, 전투가
    /// 일어났다면(도착 칸에 원래 누가 있었다면) 그 두 기사를 공개 처리합니다.
    ///
    /// AI(NineKnightsHeuristicAi)는 이 세션의 GetPieceAt/GetAllLegalMoves 같은 "누구나 보는" 정보와
    /// IsRevealed로 걸러진 공개된 적 기물 정보만 쓰고, 상대의 비공개 번호나 비밀 임무/히든 번호는
    /// 절대 들여다보지 않습니다 — 정직한(cheating 없는) 탐색을 위한 설계입니다.
    /// </summary>
    public sealed class NineKnightsGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;
        private readonly Cond_LegalNineKnightsMove m_objMoveCondition = new Cond_LegalNineKnightsMove();
        private readonly DomainAction m_objLegalityProbeAction = new DomainAction(
            "LegalityProbe", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));
        private readonly HashSet<string> m_setRevealedEntityIds = new HashSet<string>();

        private readonly int m_nPlayer1MissionNumber;
        private readonly int m_nPlayer2MissionNumber;
        private readonly int m_nPlayer1HiddenNumber;
        private readonly int m_nPlayer2HiddenNumber;

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        public NineKnightsGameSession(Ont.GameContext p_objInitialContext, Random? p_objRandom = null, OntDyn.IActionLogger? p_objLogger = null)
            : this(p_objInitialContext, RollSecrets(p_objRandom ?? new Random()), p_objLogger)
        {
        }

        /// <summary>
        /// 하나의 Random 인스턴스에서 4개 값(임무 x2, 히든 x2)을 순서대로 뽑습니다 — 생성자 초기화식
        /// 안에서 "p_objRandom ?? new Random()"을 네 번 따로 평가하면 매번 다른 Random 인스턴스가
        /// 생겨(특히 null일 때) 값이 서로 상관관계를 가질 위험이 있어서, 이렇게 한 번만 해석해
        /// 재사용합니다.
        /// </summary>
        private static (int P1Mission, int P2Mission, int P1Hidden, int P2Hidden) RollSecrets(Random p_objRandom)
        {
            return (p_objRandom.Next(1, 10), p_objRandom.Next(1, 10), p_objRandom.Next(1, 6), p_objRandom.Next(1, 6));
        }

        /// <summary>
        /// 비밀 임무/히든 토큰 번호를 무작위가 아니라 직접 못박고 싶은 테스트 전용 진입점입니다
        /// (internal — InternalsVisibleTo로 Core.Tests만 접근 가능). 일반 사용처는 위의 Random 기반
        /// 공개 생성자를 씁니다.
        /// </summary>
        internal NineKnightsGameSession(
            Ont.GameContext p_objInitialContext,
            int p_nPlayer1MissionNumber, int p_nPlayer2MissionNumber, int p_nPlayer1HiddenNumber, int p_nPlayer2HiddenNumber,
            OntDyn.IActionLogger? p_objLogger = null)
            : this(p_objInitialContext, (p_nPlayer1MissionNumber, p_nPlayer2MissionNumber, p_nPlayer1HiddenNumber, p_nPlayer2HiddenNumber), p_objLogger)
        {
        }

        private NineKnightsGameSession(
            Ont.GameContext p_objInitialContext,
            (int P1Mission, int P2Mission, int P1Hidden, int P2Hidden) p_stSecrets,
            OntDyn.IActionLogger? p_objLogger)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));
            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);

            m_nPlayer1MissionNumber = p_stSecrets.P1Mission;
            m_nPlayer2MissionNumber = p_stSecrets.P2Mission;
            m_nPlayer1HiddenNumber = p_stSecrets.P1Hidden;
            m_nPlayer2HiddenNumber = p_stSecrets.P2Hidden;

            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new NineKnightsMainPlayPhase(), new NineKnightsGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        public int GetMissionNumber(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? m_nPlayer1MissionNumber : m_nPlayer2MissionNumber;
        }

        public int GetHiddenNumber(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.Black ? m_nPlayer1HiddenNumber : m_nPlayer2HiddenNumber;
        }

        /// <summary>이 기사의 번호가 상대에게 공개된 적이 있는지(=과거에 한 번이라도 전투에 참여했는지)입니다.</summary>
        public bool IsRevealed(string p_strEntityId)
        {
            return m_setRevealedEntityIds.Contains(p_strEntityId);
        }

        public Ont.Entity? GetPieceAt(int p_nX, int p_nY)
        {
            return NineKnightsZoneQuery.FindPieceAt(mv_objCurrentContext, p_nX, p_nY);
        }

        /// <summary>이 색이 반상 + 예비를 통틀어 지금 몇 명 남아있는지입니다(위치/존재 여부는 항상 공개 정보이므로,
        /// 번호를 몰라도 셀 수 있습니다).</summary>
        public int GetRemainingPieceCount(Ont.E_PlayerColor p_eColor)
        {
            return NineKnightsZoneQuery.FindActivePiecesOnBoard(mv_objCurrentContext, p_eColor).Count
                + NineKnightsZoneQuery.FindReservePieces(mv_objCurrentContext, p_eColor).Count;
        }

        public List<(int X, int Y)> GetLegalDestinations(int p_nFromX, int p_nFromY)
        {
            List<(int X, int Y)> lisDestinations = new List<(int X, int Y)>();
            Ont.Entity? objMover = GetPieceAt(p_nFromX, p_nFromY);
            if (objMover is null)
            {
                return lisDestinations;
            }

            for (int nDy = -1; nDy <= 1; nDy++)
            {
                for (int nDx = -1; nDx <= 1; nDx++)
                {
                    if (nDx == 0 && nDy == 0)
                    {
                        continue;
                    }

                    int nToX = p_nFromX + nDx;
                    int nToY = p_nFromY + nDy;

                    m_objLegalityProbeAction.mv_stActionData = new Ont.ST_ActionData(
                        NineKnightsActionCoding.EncodeSquare(p_nFromX, p_nFromY),
                        NineKnightsActionCoding.EncodeSquare(nToX, nToY),
                        false,
                        objMover.mv_eColor);

                    if (m_objMoveCondition.IsSatisfied(mv_objCurrentContext, m_objLegalityProbeAction))
                    {
                        lisDestinations.Add((nToX, nToY));
                    }
                }
            }

            return lisDestinations;
        }

        /// <summary>p_eColor가 지금 둘 수 있는 모든 (출발,도착) 합법수 쌍입니다.</summary>
        public List<(int FromX, int FromY, int ToX, int ToY)> GetAllLegalMoves(Ont.E_PlayerColor p_eColor)
        {
            List<(int FromX, int FromY, int ToX, int ToY)> lisMoves = new List<(int FromX, int FromY, int ToX, int ToY)>();

            foreach (Ont.Entity objPiece in NineKnightsZoneQuery.FindActivePiecesOnBoard(mv_objCurrentContext, p_eColor))
            {
                int nX = objPiece.mv_objLocatedZone.mv_nX;
                int nY = objPiece.mv_objLocatedZone.mv_nY;

                foreach ((int ToX, int ToY) in GetLegalDestinations(nX, nY))
                {
                    lisMoves.Add((nX, nY, ToX, ToY));
                }
            }

            return lisMoves;
        }

        public Ont.GameContext MovePiece(int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            EnsureMainPlayPhase();

            Ont.Entity? objMover = GetPieceAt(p_nFromX, p_nFromY);
            if (objMover is null)
            {
                throw new OntDyn.RuleViolationException("출발 칸에 기사가 없습니다.");
            }

            Ont.Entity? objDefenderBeforeMove = GetPieceAt(p_nToX, p_nToY);

            // 액션의 주체 색은 클릭된 기물 자신의 색(objMover.mv_eColor)이 아니라 "지금 활성 턴 색"으로
            // 못박는다 — 그래야 Cond_LegalNineKnightsMove의 "출발 칸 기물 색이 행동 색과 같은가" 검사가
            // 자기 차례가 아닌데 기물을 움직이려는 시도(상대 기물이든, 이론상 자기 기물이든)를 표준적인
            // 조건 실패(RuleViolationException)로 걸러준다. Chess의 CreateMoveAction과 같은 이유입니다.
            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            DomainAction objAction = CreateMoveAction(eActiveColor, p_nFromX, p_nFromY, p_nToX, p_nToY);
            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);

            if (objDefenderBeforeMove is not null)
            {
                // 전투가 벌어졌다 — 이겼든 졌든 맞붙은 두 기사의 번호는 이제 서로에게 공개된다.
                m_setRevealedEntityIds.Add(objMover.mv_strEntityID);
                m_setRevealedEntityIds.Add(objDefenderBeforeMove.mv_strEntityID);
            }

            m_objPhaseManager.Update(mv_objCurrentContext);
            return mv_objCurrentContext;
        }

        private void EnsureMainPlayPhase()
        {
            if (m_objPhaseManager.CurrentPhase is not NineKnightsMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 기사를 움직일 수 없습니다.");
            }
        }

        private DomainAction CreateMoveAction(Ont.E_PlayerColor p_eActiveColor, int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            DomainAction objAction = new DomainAction(
                "Action_MoveOrAttack",
                new Ont.ST_ActionData(
                    NineKnightsActionCoding.EncodeSquare(p_nFromX, p_nFromY),
                    NineKnightsActionCoding.EncodeSquare(p_nToX, p_nToY),
                    false,
                    p_eActiveColor));

            objAction.mv_lisConditions.Add(new Cond_LegalNineKnightsMove());

            objAction.mv_lisEffects.Add(new Effect_MovePieceAndResolveCombat(GetHiddenNumber(p_eActiveColor)));
            objAction.mv_lisEffects.Add(new Effect_SwitchTurn());
            objAction.mv_lisEffects.Add(new Effect_CheckMissionAndEliminationWin(GetMissionNumber(p_eActiveColor)));

            return objAction;
        }
    }
}
