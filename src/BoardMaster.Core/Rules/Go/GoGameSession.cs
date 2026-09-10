namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 바둑 한 판을 실제로 진행할 수 있는 최소 오케스트레이터입니다. 매 착수/패스마다 Action을
    /// 조건(Cond_EmptySpace, Cond_NotKoRecapture, Cond_NotSuicide, Cond_NotSuperko)과 효과
    /// (Effect_SpawnEntity, Effect_CaptureStones, Effect_SwitchTurn, 패스의 경우 추가로
    /// Effect_CheckConsecutivePassGameEnd/Effect_FinalizeScore)로 조립해 ActionDispatcher에 위임하고,
    /// 반환된 새 GameContext로 내부 상태를 갱신합니다.
    /// 연속 2회 패스가 나오면 자동으로 종국 처리(mv_isGameOver = true)되고 중국식(면적) 계가로
    /// PlayerState.mv_nScore가 채워집니다(GoScoreCalculator 참고).
    ///
    /// 단순패(simple Ko)도 자동으로 막습니다: 매 PlayStone 성공 직후, 착수 전/후 반상을 비교해
    /// "정확히 한 칸이 상대 돌에서 빈 칸으로 바뀌었는지"를 판정하고, 그렇다면 그 좌표를
    /// m_stForbiddenKoPoint로 기억해 다음 Cond_NotKoRecapture 생성 시 주입합니다. Pass()나 캡처가
    /// 없는 착수는 이 값을 null로 되돌리므로, 제한은 항상 "바로 다음 한 수"에만 걸립니다.
    ///
    /// 위치 기반 슈퍼코(positional superko)도 막습니다: Zobrist 해시(GoZobristTable)로 반상 배치를
    /// 추적합니다 — m_ulCurrentPositionHash는 실제 착수가 성공할 때마다 갱신되고,
    /// m_setVisitedPositionHashes에 쌓입니다. Cond_NotSuperko는 "이 착수를 두면 나올 배치의 해시가
    /// 이미 나온 적 있는지"를 그 집합에서 찾아보는데, 격자를 복제하지 않고 바뀌는 칸만 XOR해서
    /// O(포획된 돌 수)로 계산합니다(예전에는 후보 하나마다 O(width*height) 복제+직렬화였습니다).
    /// 단순패가 막는 경우는 전부 이 검사에도 걸리지만(부분집합), 단순패 쪽이 좌표 비교 한 번으로
    /// 끝나는 훨씬 싼 검사라 먼저 돌립니다 — 삼패(triple ko)처럼 더 긴 순환은 슈퍼코 쪽만 잡아냅니다.
    /// 이런 식으로 GameContext 자체에는 바둑 전용 상태를 전혀 추가하지 않고, 세션 인스턴스에만
    /// 이 이력을 들고 있습니다.
    ///
    /// 페이즈 상태 머신(GamePhaseManager + IGamePhase, Ontology.Dynamic)도 내부에서 씁니다: 페이즈는
    /// [GoMainPlayPhase -> GoGameOverPhase] 두 단계뿐이고, PlayStone/Pass는 항상 EnsureMainPlayPhase로
    /// 시작해 현재 페이즈가 MainPlay가 아니면 즉시 거부합니다. 실제 종국 판정(연속 2패스)은 여전히
    /// Effect_CheckConsecutivePassGameEnd가 하고, 페이즈 머신은 그 결과(mv_isGameOver)를 보고 전이할
    /// 뿐입니다 — 바둑처럼 라운드 내 하위 단계가 없는 장르에서는 이 정도가 정직한 매핑입니다. 이
    /// 머신 자체는 어떤 장르에도 안 묶여 있어서(IGamePhase는 GameContext만 알 뿐 Go를 전혀 모름),
    /// 나중에 라운드마다 여러 하위 단계가 있는 다른 게임에도 그대로 재사용할 수 있습니다.
    ///
    /// 이 클래스는 "한 수를 실제로 두고 다음 턴으로 넘어가며, 단순패·슈퍼코를 막고, 두 번 연속
    /// 패스로 자동 종국·계가까지 처리한다"는 시나리오까지 책임집니다.
    ///
    /// Clone()과 GetLegalMoves()는 GoMctsSearcher가 이 세션을 트리 노드의 상태 표현 그 자체로 재사용할
    /// 수 있도록 추가한 것입니다 — MCTS는 같은 국면에서 여러 가상의 미래를 서로 다른 GoGameSession
    /// 복제본으로 독립적으로 탐색합니다. Clone()은 반상 배치 이력(m_setVisitedPositionHashes)도 통째로
    /// 복사해 각 복제본이 서로 다른 슈퍼코 이력을 독립적으로 쌓아갑니다.
    ///
    /// GetLegalMoves()의 자충수 판정은 GoLibertyCache(그룹별 활로 "개수"만 유지하는 캐시)로 O(1)에
    /// 처리합니다 — 실제 착수가 성공할 때마다 한 번씩(RebuildLibertyCache) 반상 전체를 다시 훑어
    /// 그룹/활로를 갱신해 두고, 후보 칸 하나당 이웃 4칸만 조회하면 됩니다(전에는 후보마다 BFS를
    /// 최대 5번 돌았습니다 — 계측 결과 롤아웃 시간의 약 85%). PlayStone()이 실제로 착수를 검증할
    /// 때는 여전히 BFS 기반 Cond_NotSuicide를 그대로 씁니다 — GoLibertyCache는 GetLegalMoves()만을
    /// 위한 지름길이라, 캐시가 잘못돼도 실제 착수 판정 자체는 오염되지 않습니다. 정확성은
    /// GoLibertyCacheDifferentialTests가 무작위 대국으로 두 경로의 결과를 매 수마다 대조해 보장합니다.
    ///
    /// 규칙 변형(Modifier): 생성자로 IRuleModifier 목록을 받으면, 매 착수 Action을 조립한 뒤 이
    /// 목록을 순서대로 적용해 기본 규칙을 끼워 넣거나 대체할 수 있습니다(예: GoNoSuperkoModifier로
    /// 슈퍼코를 끄고 단순패만 적용하는 변형 룰셋). 기본값(빈 목록)일 때는 GetLegalMoves()가 위의
    /// 빠른 경로를 쓰고, 모디파이어가 하나라도 등록되면 실제 Action 조립 결과로 검증하는 느린
    /// 경로로 자동 전환합니다 — PlayStone()과 GetLegalMoves()의 판정 기준이 모디파이어 유무와
    /// 상관없이 항상 일치하도록 보장하기 위해서입니다.
    /// </summary>
    public sealed class GoGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;
        private readonly Cond_NotSuicide m_objSuicideCondition = new Cond_NotSuicide();
        private readonly GoLibertyCache m_objLibertyCache = new GoLibertyCache();
        private readonly HashSet<ulong> m_setVisitedPositionHashes = new HashSet<ulong>();
        private readonly IReadOnlyList<OntDyn.IRuleModifier> m_lisRuleModifiers;
        private readonly DomainAction m_objLegalityProbeAction = new DomainAction(
            "LegalityProbe", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));
        private Cond_NotSuperko m_objSuperkoCondition;
        private ulong m_ulCurrentPositionHash;
        private (int X, int Y)? m_stForbiddenKoPoint;

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        public GoGameSession(
            Ont.GameContext p_objInitialContext,
            OntDyn.IActionLogger? p_objLogger = null,
            IReadOnlyList<OntDyn.IRuleModifier>? p_lisRuleModifiers = null)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));
            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);
            m_lisRuleModifiers = p_lisRuleModifiers ?? Array.Empty<OntDyn.IRuleModifier>();

            m_ulCurrentPositionHash = ComputeCurrentPositionHash();
            m_setVisitedPositionHashes.Add(m_ulCurrentPositionHash);
            m_objSuperkoCondition = new Cond_NotSuperko(m_ulCurrentPositionHash, m_setVisitedPositionHashes);
            RebuildLibertyCache();

            // 페이즈는 [본 플레이 -> 종국] 두 단계뿐이다. Start()가 즉시 IsPhaseCompleted를 확인하므로,
            // 이미 종료된 GameContext(예: 기보 재생으로 만들어진 것)로 세션을 열어도 곧바로
            // GoGameOverPhase에 안착한다 — 별도 분기 없이 자연히 일관된 상태가 된다.
            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new GoMainPlayPhase(), new GoGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        /// <summary>
        /// 현재 거시 페이즈 이름입니다("MainPlay" 또는 "GameOver"). PlayStone/Pass는 페이즈가
        /// MainPlay일 때만 허용됩니다.
        /// </summary>
        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        /// <summary>
        /// 현재 상태(반상, 패 제한, 슈퍼코 이력, 규칙 모디파이어)를 완전히 격리된 새 GoGameSession으로
        /// 복제합니다. MCTS가 하나의 국면에서 여러 가상의 미래를 서로 간섭 없이 탐색할 때 씁니다
        /// (GameContext.Clone()과 같은 목적의 세션 레벨 버전).
        /// </summary>
        public GoGameSession Clone()
        {
            GoGameSession objClone = new GoGameSession(mv_objCurrentContext.Clone(), null, m_lisRuleModifiers)
            {
                m_stForbiddenKoPoint = m_stForbiddenKoPoint
            };

            // 생성자가 "현재 한 배치"만으로 이력을 새로 시작해 두었으므로, 원본이 대국 시작부터
            // 쌓아온 전체 배치 이력으로 통째로 덮어써야 한다 — 그래야 복제본도 원본과 같은 슈퍼코
            // 제약을 물려받는다. m_ulCurrentPositionHash는 클론 생성자가 이미 같은 배치로부터
            // 올바르게 계산했으므로(원본과 정확히 같은 값) 따로 덮어쓸 필요가 없다.
            objClone.m_setVisitedPositionHashes.Clear();
            foreach (ulong ulHash in m_setVisitedPositionHashes)
            {
                objClone.m_setVisitedPositionHashes.Add(ulHash);
            }

            return objClone;
        }

        /// <summary>
        /// 현재 활성 플레이어가 지금 합법적으로 둘 수 있는 모든 좌표를 반환합니다(패스는 포함하지 않음 —
        /// 패스는 항상 가능하므로 호출자가 별도로 다룹니다). 종국된 대국이면 빈 리스트를 반환합니다.
        /// 실제 착수 검증(PlayStone)과 완전히 같은 기준으로 판정하므로, 여기 나온 좌표는 곧바로
        /// PlayStone에 넘겨도 항상 성공합니다.
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
                    if (a_nGrid[nX, nY] != (int)Ont.E_PlayerColor.None)
                    {
                        continue;
                    }

                    if (m_stForbiddenKoPoint is { } stForbidden && stForbidden.X == nX && stForbidden.Y == nY)
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

        /// <summary>
        /// 현재 활성 플레이어 색상으로 (p_nX, p_nY)에 착수를 시도합니다. 성공하면 내부 컨텍스트를
        /// 갱신하고 다음 턴으로 넘긴 뒤 그 결과를 반환합니다. 규칙 위반이면 RuleViolationException이
        /// 그대로 전파되며, mv_objCurrentContext와 패 상태는 손대지 않은 채 유지됩니다
        /// (ActionDispatcher/Action.Execute의 원자적 전이 보장 덕분입니다).
        /// </summary>
        public Ont.GameContext PlayStone(int p_nX, int p_nY)
        {
            EnsureMainPlayPhase();

            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            Ont.GameContext objContextBeforeMove = mv_objCurrentContext;
            DomainAction objAction = CreatePlaceStoneAction(p_nX, p_nY, eActiveColor);

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);
            m_stForbiddenKoPoint = DetectSingleStoneCapture(objContextBeforeMove, mv_objCurrentContext, p_nX, p_nY);

            // 실제로 반상이 바뀌었으니 현재 배치 해시를 다시 계산하고(이번 한 번만 O(width*height) —
            // 후보 검증 경로와 달리 실제 착수는 한 판에 많아야 수백 번뿐이라 문제 되지 않는다),
            // Cond_NotSuperko도 최신 해시로 다시 만든다(그 클래스는 해시를 값으로 캡처해 두므로
            // 참조를 바꿔주지 않으면 계속 옛 배치 기준으로 판정하게 된다).
            m_ulCurrentPositionHash = ComputeCurrentPositionHash();
            m_setVisitedPositionHashes.Add(m_ulCurrentPositionHash);
            m_objSuperkoCondition = new Cond_NotSuperko(m_ulCurrentPositionHash, m_setVisitedPositionHashes);
            RebuildLibertyCache();

            m_objPhaseManager.Update(mv_objCurrentContext);

            return mv_objCurrentContext;
        }

        /// <summary>
        /// 현재 활성 플레이어가 착수 없이 턴만 넘깁니다. 패스는 반상을 바꾸지 않으므로 패 제한과
        /// 슈퍼코 해시 모두 그대로 둡니다(패 제한만 해제합니다).
        /// </summary>
        public Ont.GameContext Pass()
        {
            EnsureMainPlayPhase();

            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            DomainAction objAction = CreatePassAction(eActiveColor);

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);
            m_stForbiddenKoPoint = null;
            m_objPhaseManager.Update(mv_objCurrentContext);

            return mv_objCurrentContext;
        }

        /// <summary>
        /// 지금까지의 참가자/수순을 GoKifuSerializer.Export로 그대로 위임하는 편의 메서드입니다.
        /// </summary>
        public string ExportKifu()
        {
            return GoKifuSerializer.Export(this);
        }

        /// <summary>
        /// p_nX, p_nY에 p_eColor로 두는 것이 지금 합법인지 확인합니다. 모디파이어가 없으면(기본값)
        /// GoLibertyCache로 자충수를 O(1)에 판정하는 빠른 경로를 쓰고, 모디파이어가 등록되어 있으면
        /// 실제 Action 조립 결과(모디파이어 반영)로 검증하는 느린 경로를 씁니다 — PlayStone()이
        /// 실제로 쓰는 조립 로직과 항상 같은 기준을 보장하기 위해서입니다.
        /// GoLibertyCache 기반 자충수 판정은 GetLegalMoves()의 지름길일 뿐입니다 — PlayStone()의 실제
        /// 검증(CreatePlaceStoneAction → Cond_NotSuicide)은 여전히 BFS 버전을 그대로 쓰므로, 이 캐시가
        /// 잘못돼도 실제 착수 판정에는 영향이 없습니다. 정확성은
        /// GoLibertyCacheDifferentialTests가 무작위 대국으로 두 경로의 결과를 대조해 보장합니다.
        /// </summary>
        private bool IsLegalPlacement(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            if (m_lisRuleModifiers.Count == 0)
            {
                int[,] a_nGrid = mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;
                int nWidth = a_nGrid.GetLength(0);
                int nHeight = a_nGrid.GetLength(1);

                if (!m_objLibertyCache.IsNonSuicidePlacement(a_nGrid, nWidth, nHeight, p_nX, p_nY, p_eColor))
                {
                    return false;
                }

                m_objLegalityProbeAction.mv_stActionData = new Ont.ST_ActionData(p_nX, p_nY, false, p_eColor);
                return m_objSuperkoCondition.IsSatisfied(mv_objCurrentContext, m_objLegalityProbeAction);
            }

            DomainAction objProbeAction = CreatePlaceStoneAction(p_nX, p_nY, p_eColor);
            return objProbeAction.Validate(mv_objCurrentContext);
        }

        /// <summary>
        /// 현재 페이즈가 MainPlay가 아니면(=GameOver) RuleViolationException을 던져 착수/패스를 막습니다.
        /// FR-1.2("현재 페이즈에 허용되지 않는 입력은 자동 차단")의 실제 집행 지점입니다. ActionDispatcher도
        /// mv_isGameOver를 별도로 검사하지만, 그건 GameContext를 직접 다루는 호출자를 위한 최후 방어선이고,
        /// GoGameSession을 통하는 정상 경로에서는 이 페이즈 검사가 먼저 걸립니다.
        /// </summary>
        private void EnsureMainPlayPhase()
        {
            if (m_objPhaseManager.CurrentPhase is not GoMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 착수/패스를 진행할 수 없습니다.");
            }
        }

        /// <summary>
        /// 현재 mv_objCurrentContext의 반상 배치 전체를 훑어 Zobrist 해시를 처음부터 계산합니다.
        /// 실제 착수가 성공했을 때(한 판에 많아야 수백 번)만 호출되므로 O(width*height) 비용이
        /// 문제 되지 않습니다 — 후보 수를 검증하는 뜨거운 경로(Cond_NotSuperko)는 이 값을 시작점
        /// 삼아 바뀌는 칸만 XOR하는 증분 방식이라 이 메서드를 다시 부르지 않습니다.
        /// </summary>
        private ulong ComputeCurrentPositionHash()
        {
            int[,] a_nGrid = mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);
            ulong ulHash = 0;

            for (int nY = 0; nY < nHeight; nY++)
            {
                for (int nX = 0; nX < nWidth; nX++)
                {
                    Ont.E_PlayerColor eColor = (Ont.E_PlayerColor)a_nGrid[nX, nY];
                    if (eColor != Ont.E_PlayerColor.None)
                    {
                        ulHash ^= GoZobristTable.GetValue(nX, nY, eColor);
                    }
                }
            }

            return ulHash;
        }

        /// <summary>
        /// GoLibertyCache를 현재 반상 배치로 처음부터 다시 계산합니다. ComputeCurrentPositionHash와
        /// 마찬가지로 실제 착수가 성공했을 때만 호출되므로 O(width*height) 비용이 문제 되지 않습니다.
        /// </summary>
        private void RebuildLibertyCache()
        {
            int[,] a_nGrid = mv_objCurrentContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);
            m_objLibertyCache.Rebuild(a_nGrid, nWidth, nHeight);
        }

        /// <summary>
        /// internal로 노출해 테스트 프로젝트가 모디파이어 적용 전/후의 조립 결과(조건/효과 목록)를
        /// 직접 들여다볼 수 있게 했습니다(예: GoNoSuperkoModifier가 실제로 Cond_NotSuperko를
        /// 빼는지). GetLegalMoves()의 느린 경로(모디파이어가 있을 때)도 이 메서드를 그대로 씁니다.
        /// </summary>
        internal DomainAction CreatePlaceStoneAction(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            DomainAction objAction = new DomainAction(
                "Action_PlayStone",
                new Ont.ST_ActionData(p_nX, p_nY, false, p_eColor));

            // 순서 중요: Cond_EmptySpace가 범위/점유 여부를 먼저 걸러야 한다. Cond_NotSuicide의 BFS는
            // 시작 좌표가 보드 범위 안이라고 가정하므로, 범위 밖 좌표가 먼저 걸러지지 않으면 인덱스 예외가 난다.
            // Cond_NotKoRecapture는 좌표 비교 한 번으로 끝나는 가장 싼 검사라 먼저 두고, Cond_NotSuperko는
            // (Zobrist로 최적화했어도) 상대 그룹 BFS가 필요할 수 있는 가장 비싼 검사라 마지막에 둔다 —
            // Cond_NotKoRecapture가 막는 경우는 전부 Cond_NotSuperko도 막지만(부분집합), 순서상 싼 쪽이
            // 먼저 걸러 준다.
            objAction.mv_lisConditions.Add(new Cond_EmptySpace());
            objAction.mv_lisConditions.Add(new Cond_NotKoRecapture(m_stForbiddenKoPoint));
            objAction.mv_lisConditions.Add(m_objSuicideCondition);
            objAction.mv_lisConditions.Add(m_objSuperkoCondition);

            // 순서 중요: 돌을 먼저 놓아야(Effect_SpawnEntity) 그 다음 상대 그룹의 활로가 실제로 0인지
            // Effect_CaptureStones가 정확히 판정할 수 있다. 턴 전환은 항상 마지막이다.
            objAction.mv_lisEffects.Add(new Effect_SpawnEntity());
            objAction.mv_lisEffects.Add(new Effect_CaptureStones());
            objAction.mv_lisEffects.Add(new Effect_SwitchTurn());

            // 등록된 모디파이어를 순서대로 적용한다 — 기본 조립 로직(위)은 손대지 않고, 필요하면
            // 모디파이어가 조건/효과를 추가하거나(mv_lisConditions.Add) 제거해(RemoveAll 등) 규칙을
            // 바꾼다. 기본값(빈 목록)일 때는 이 루프가 그냥 아무 일도 하지 않는다.
            foreach (OntDyn.IRuleModifier objModifier in m_lisRuleModifiers)
            {
                objModifier.Apply(objAction, mv_objCurrentContext);
            }

            return objAction;
        }

        private static DomainAction CreatePassAction(Ont.E_PlayerColor p_eColor)
        {
            DomainAction objAction = new DomainAction(
                "Action_Pass",
                new Ont.ST_ActionData(0, 0, true, p_eColor));

            // 순서 중요: 턴을 넘긴 뒤 -> 연속 2패스 여부로 종국을 판정한 뒤(mv_isGameOver 설정) ->
            // 그 결과를 보고 종국이면 계가한다(Effect_FinalizeScore는 mv_isGameOver가 false면 아무 것도 안 함).
            objAction.mv_lisEffects.Add(new Effect_SwitchTurn());
            objAction.mv_lisEffects.Add(new Effect_CheckConsecutivePassGameEnd());
            objAction.mv_lisEffects.Add(new Effect_FinalizeScore());

            return objAction;
        }

        /// <summary>
        /// 착수 전/후 반상을 비교해 "이번 착수 좌표를 제외하고, 정확히 한 칸이 점유 상태에서 빈 칸으로
        /// 바뀌었는지"를 판정합니다. 그렇다면 그 좌표가 이번 수의 유일한 포로이므로 패 후보가 됩니다.
        /// 캡처가 0개이거나 2개 이상이면(단순패 대상이 아님) null을 반환합니다.
        /// </summary>
        private static (int X, int Y)? DetectSingleStoneCapture(
            Ont.GameContext p_objBefore, Ont.GameContext p_objAfter, int p_nPlacedX, int p_nPlacedY)
        {
            int[,] a_nBeforeGrid = p_objBefore.mv_stCurrentState.m_a_nBoardGrid;
            int[,] a_nAfterGrid = p_objAfter.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nAfterGrid.GetLength(0);
            int nHeight = a_nAfterGrid.GetLength(1);

            int nCapturedX = -1;
            int nCapturedY = -1;
            int nCapturedCount = 0;

            for (int nY = 0; nY < nHeight; nY++)
            {
                for (int nX = 0; nX < nWidth; nX++)
                {
                    if (nX == p_nPlacedX && nY == p_nPlacedY)
                    {
                        continue; // 이번에 놓인 돌 자체는 캡처가 아니다.
                    }

                    bool bWasOccupied = a_nBeforeGrid[nX, nY] != (int)Ont.E_PlayerColor.None;
                    bool bIsNowEmpty = a_nAfterGrid[nX, nY] == (int)Ont.E_PlayerColor.None;

                    if (bWasOccupied && bIsNowEmpty)
                    {
                        nCapturedCount++;
                        nCapturedX = nX;
                        nCapturedY = nY;
                    }
                }
            }

            return nCapturedCount == 1 ? (nCapturedX, nCapturedY) : null;
        }
    }
}
