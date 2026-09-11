namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 구룡투(九龍鬪 / Showdown Tactics, tvN 더 지니어스의 "흑과 백")를 진행하는 오케스트레이터입니다.
    /// Go(반상)/War(자동 진행 카드)에 이은 세 번째 장르로, 이번엔 "동시 비공개 선택(블라인드 비딩)"을
    /// 증명합니다 — ActionDispatcher는 여전히 한 번에 하나씩만 처리하므로, "동시성"은 두 플레이어의
    /// 선택을 각각 별도의 Action으로 커밋받되 상대가 이미 낸 타일을 볼 수 없게(Pending Zone을
    /// Hidden으로 두고, 둘 다 찼을 때만 비교) 구성해서 시뮬레이션합니다.
    ///
    /// 규칙: 양쪽이 1~9 타일을 하나씩(자기 소유 9장) 갖고 시작해 9라운드를 치릅니다. 매 라운드
    /// 양쪽이 비공개로 타일 하나씩을 내고, 더 큰 숫자가 이겨 1점을 얻습니다 — 단, "1은 9를 이긴다"는
    /// 예외가 있습니다(Effect_ResolveComparisonAndScore 참고). 9라운드 후 더 많이 이긴 쪽이 승자이고,
    /// 이긴 라운드 수가 같으면 무승부입니다.
    ///
    /// CommitTile()은 내부적으로 두 개의 Action을 나눠 디스패치합니다: 먼저 "커밋"만(Pending으로
    /// 이동), 그다음 양쪽 Pending이 모두 찼는지 세션이 직접 확인해서 찼을 때만 "정산" Action을
    /// 추가로 디스패치합니다. War의 PlayRound가 draw/resolve를 나누는 것과 똑같은 이유입니다 —
    /// 정산 Effect가 끝나자마자 Pending을 비워버리므로, 그 사이에 한 번 멈춰서 관찰해 두지 않으면
    /// LastRoundResult(UI가 "무슨 타일끼리 붙었는지" 보여주는 데 필요)를 만들 수 없습니다.
    /// </summary>
    public sealed class GuryongTuGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        /// <summary>가장 최근에 정산된 라운드의 요약입니다. 아직 한 라운드도 끝나지 않았으면 null입니다.</summary>
        public GuryongTuRoundResult? LastRoundResult { get; private set; }

        public GuryongTuGameSession(Ont.GameContext p_objInitialContext, OntDyn.IActionLogger? p_objLogger = null)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));
            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);

            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new GuryongTuMainPlayPhase(), new GuryongTuGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        /// <summary>이 플레이어가 지금까지 이긴 라운드 수입니다(=PlayerState.mv_nScore).</summary>
        public int CountRoundsWon(Ont.E_PlayerColor p_eColor)
        {
            Ont.PlayerState? objState = mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == p_eColor);
            return objState?.mv_nScore ?? 0;
        }

        /// <summary>이 플레이어의 Hand에 아직 남아 있는(=아직 낸 적 없는) 타일 랭크 목록입니다(오름차순).</summary>
        public List<int> GetRemainingTiles(Ont.E_PlayerColor p_eColor)
        {
            string strHandZoneId = p_eColor == Ont.E_PlayerColor.Black ? GuryongTuZoneId.HandBlack : GuryongTuZoneId.HandWhite;
            List<int> lisRanks = new List<int>();

            foreach (Ont.Entity objTile in GuryongTuZoneQuery.FindEntitiesInZone(mv_objCurrentContext, strHandZoneId))
            {
                lisRanks.Add(int.Parse(objTile.mv_strType));
            }

            lisRanks.Sort();
            return lisRanks;
        }

        /// <summary>이 플레이어가 이번 라운드에 이미 타일을 냈는지(=상대의 커밋을 기다리는 중인지)입니다.</summary>
        public bool HasCommittedThisRound(Ont.E_PlayerColor p_eColor)
        {
            string strPendingZoneId = p_eColor == Ont.E_PlayerColor.Black ? GuryongTuZoneId.PendingBlack : GuryongTuZoneId.PendingWhite;
            return GuryongTuZoneQuery.FindEntitiesInZone(mv_objCurrentContext, strPendingZoneId).Count > 0;
        }

        /// <summary>
        /// p_eColor가 p_nRank 타일을 비공개로 냅니다. 상대가 아직 안 냈다면 그냥 Pending에 올려두고
        /// 기다리고, 상대도 이미 냈다면(이 호출로 둘 다 찼다면) 곧바로 그 자리에서 라운드를 정산하고
        /// LastRoundResult를 채웁니다.
        /// </summary>
        public Ont.GameContext CommitTile(Ont.E_PlayerColor p_eColor, int p_nRank)
        {
            if (m_objPhaseManager.CurrentPhase is not GuryongTuMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 타일을 낼 수 없습니다.");
            }

            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, CreateCommitAction(p_eColor, p_nRank));

            List<Ont.Entity> lisBlackPending = GuryongTuZoneQuery.FindEntitiesInZone(mv_objCurrentContext, GuryongTuZoneId.PendingBlack);
            List<Ont.Entity> lisWhitePending = GuryongTuZoneQuery.FindEntitiesInZone(mv_objCurrentContext, GuryongTuZoneId.PendingWhite);

            if (lisBlackPending.Count > 0 && lisWhitePending.Count > 0)
            {
                int nBlackRank = int.Parse(lisBlackPending[0].mv_strType);
                int nWhiteRank = int.Parse(lisWhitePending[0].mv_strType);

                mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, CreateResolveAction());
                LastRoundResult = new GuryongTuRoundResult(
                    nBlackRank, nWhiteRank, Effect_ResolveComparisonAndScore.DetermineWinner(nBlackRank, nWhiteRank));
            }

            m_objPhaseManager.Update(mv_objCurrentContext);
            return mv_objCurrentContext;
        }

        private static DomainAction CreateCommitAction(Ont.E_PlayerColor p_eColor, int p_nRank)
        {
            DomainAction objAction = new DomainAction(
                "Action_CommitTile",
                new Ont.ST_ActionData(p_nRank, 0, false, p_eColor));

            objAction.mv_lisConditions.Add(new Cond_TileInHand());
            objAction.mv_lisEffects.Add(new Effect_CommitTileToPending());

            return objAction;
        }

        private static DomainAction CreateResolveAction()
        {
            DomainAction objAction = new DomainAction(
                "Action_ResolveRound",
                new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));

            objAction.mv_lisEffects.Add(new Effect_ResolveComparisonAndScore());
            objAction.mv_lisEffects.Add(new Effect_CheckGuryongTuGameOver());

            return objAction;
        }
    }
}
