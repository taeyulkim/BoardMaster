namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 표준 체스 한 판을 실제로 진행할 수 있는 오케스트레이터입니다. Go/War/GuryongTu에 이은 다섯
    /// 번째... 가 아니라 네 번째 장르로, "칸을 좌표로 갖는 Zone + 그 위를 옮겨 다니는 Entity"라는
    /// War/GuryongTu와 같은 표현 방식을 쓰지만, 기물마다 완전히 다른 이동 규칙을 갖는다는 점이
    /// 다릅니다(그래서 ChessMoveGenerator라는 전용 규칙 엔진이 필요합니다 — Go의 GoGroupScanner에
    /// 해당하는 존재입니다).
    ///
    /// 캐슬링 권리와 앙파상 대상 칸은 GameContext에 없는 장르 전용 파생 상태라(Go의
    /// m_stForbiddenKoPoint/m_setVisitedPositionHashes와 같은 위치) 이 세션이 들고 있다가, 매 수를
    /// 둘 때마다 Condition/Effect에 생성자로 주입합니다. 이번 수를 반영한 "다음" 캐슬링 권리/앙파상
    /// 대상 칸은 실제로 두기 전에 이미 순수 계산으로 알 수 있으므로(AfterMove가 좌표만으로 계산),
    /// War/GuryongTu처럼 두 번에 나눠 디스패치할 필요 없이 한 Action으로 끝납니다 — 다만 그 계산
    /// 결과는 디스패치가 성공한 뒤에만 세션 필드에 반영합니다(실패한 수는 아무 흔적도 남기면 안
    /// 되므로, Go/War가 실패 시 상태를 그대로 두는 것과 같은 원칙입니다).
    ///
    /// 체크메이트/스테일메이트만 종국 조건으로 봅니다 — 쓰리폴드 반복, 50수 규칙 같은 무승부 조건은
    /// 의도적으로 생략했습니다(Effect_CheckChessGameOver 참고).
    /// </summary>
    public sealed class ChessGameSession
    {
        private readonly OntDyn.ActionDispatcher m_objDispatcher;
        private readonly OntDyn.GamePhaseManager m_objPhaseManager;
        private ChessCastlingRights m_stCastlingRights;
        private (int X, int Y)? m_stEnPassantTarget;

        public Ont.GameContext mv_objCurrentContext { get; private set; }

        public ChessGameSession(Ont.GameContext p_objInitialContext, OntDyn.IActionLogger? p_objLogger = null)
        {
            mv_objCurrentContext = p_objInitialContext ?? throw new ArgumentNullException(nameof(p_objInitialContext));
            m_objDispatcher = new OntDyn.ActionDispatcher(p_objLogger);
            m_stCastlingRights = ChessCastlingRights.CreateInitial();
            m_stEnPassantTarget = null;

            m_objPhaseManager = new OntDyn.GamePhaseManager(
                new OntDyn.IGamePhase[] { new ChessMainPlayPhase(), new ChessGameOverPhase() });
            m_objPhaseManager.Start(mv_objCurrentContext);
        }

        public string CurrentPhaseName => m_objPhaseManager.CurrentPhase.mv_strPhaseName;

        public Ont.Entity? GetPieceAt(int p_nX, int p_nY)
        {
            return ChessZoneQuery.FindPieceAt(mv_objCurrentContext, p_nX, p_nY);
        }

        public bool IsInCheck(Ont.E_PlayerColor p_eColor)
        {
            return ChessMoveGenerator.IsInCheck(mv_objCurrentContext, p_eColor);
        }

        /// <summary>(p_nX, p_nY)의 기물이 지금 실제로 둘 수 있는 합법수 전부입니다. UI가 칸을 클릭했을
        /// 때 이동 가능한 칸을 미리 보여주는 데 씁니다 — 여기 나온 좌표는 곧바로 MovePiece에 넘겨도
        /// 항상 성공합니다.</summary>
        public List<ChessMove> GetLegalMoves(int p_nX, int p_nY)
        {
            return ChessMoveGenerator.GetLegalMoves(mv_objCurrentContext, p_nX, p_nY, m_stCastlingRights, m_stEnPassantTarget);
        }

        /// <summary>
        /// p_eColor가 지금 둘 수 있는 모든 합법수를 반상 전체를 훑어 모읍니다(출발/도착 좌표만).
        /// ChessMctsSearcher의 후보 생성과, 무작위 상대 AI/자기 대국 테스트가 공통으로 쓰는
        /// "이 색이 지금 뭘 둘 수 있나"라는 질문에 대한 단일 진입점입니다.
        /// </summary>
        public List<(int FromX, int FromY, int ToX, int ToY)> GetAllLegalMoves(Ont.E_PlayerColor p_eColor)
        {
            List<(int FromX, int FromY, int ToX, int ToY)> lisMoves = new List<(int FromX, int FromY, int ToX, int ToY)>();

            foreach (Ont.Entity objPiece in ChessZoneQuery.FindActivePieces(mv_objCurrentContext, p_eColor))
            {
                int nX = objPiece.mv_objLocatedZone.mv_nX;
                int nY = objPiece.mv_objLocatedZone.mv_nY;

                foreach (ChessMove stMove in GetLegalMoves(nX, nY))
                {
                    lisMoves.Add((nX, nY, stMove.ToX, stMove.ToY));
                }
            }

            return lisMoves;
        }

        /// <summary>
        /// 현재 상태(반상, 캐슬링 권리, 앙파상 대상 칸)를 완전히 격리된 새 ChessGameSession으로
        /// 복제합니다. Go/War의 Clone()과 같은 목적입니다 — ChessMctsSearcher가 하나의 국면에서
        /// 여러 가상의 미래를 서로 간섭 없이 탐색할 때 씁니다.
        /// </summary>
        public ChessGameSession Clone()
        {
            ChessGameSession objClone = new ChessGameSession(mv_objCurrentContext.Clone())
            {
                m_stCastlingRights = m_stCastlingRights,
                m_stEnPassantTarget = m_stEnPassantTarget
            };

            return objClone;
        }

        public Ont.GameContext MovePiece(int p_nFromX, int p_nFromY, int p_nToX, int p_nToY)
        {
            EnsureMainPlayPhase();

            Ont.Entity? objPiece = ChessZoneQuery.FindPieceAt(mv_objCurrentContext, p_nFromX, p_nFromY);
            if (objPiece is null)
            {
                throw new OntDyn.RuleViolationException("출발 칸에 기물이 없습니다.");
            }

            ChessCastlingRights stNewRights = m_stCastlingRights.AfterMove(
                objPiece.mv_eColor, objPiece.mv_strType, p_nFromX, p_nFromY, p_nToX, p_nToY);
            (int X, int Y)? stNewEnPassantTarget = ComputeNewEnPassantTarget(objPiece, p_nFromY, p_nToX, p_nToY);

            DomainAction objAction = CreateMoveAction(p_nFromX, p_nFromY, p_nToX, p_nToY, stNewRights, stNewEnPassantTarget);
            mv_objCurrentContext = m_objDispatcher.Dispatch(mv_objCurrentContext, objAction);

            // 여기까지 왔다는 건 디스패치가 성공했다는 뜻이므로, 이제 세션의 파생 상태를 갱신한다.
            m_stCastlingRights = stNewRights;
            m_stEnPassantTarget = stNewEnPassantTarget;

            m_objPhaseManager.Update(mv_objCurrentContext);
            return mv_objCurrentContext;
        }

        private void EnsureMainPlayPhase()
        {
            if (m_objPhaseManager.CurrentPhase is not ChessMainPlayPhase)
            {
                throw new OntDyn.RuleViolationException(
                    $"현재 페이즈({m_objPhaseManager.CurrentPhase.mv_strPhaseName})에서는 기물을 움직일 수 없습니다.");
            }
        }

        private static (int X, int Y)? ComputeNewEnPassantTarget(Ont.Entity p_objPiece, int p_nFromY, int p_nToX, int p_nToY)
        {
            if (p_objPiece.mv_strType == ChessPieceType.Pawn && Math.Abs(p_nToY - p_nFromY) == 2)
            {
                return (p_nToX, (p_nFromY + p_nToY) / 2);
            }

            return null;
        }

        /// <summary>
        /// 이동 주체의 색은 출발 칸에 있는 기물의 색이 아니라 "지금 활성 턴 색"으로 못박습니다 —
        /// 그래야 Cond_LegalChessMove의 "출발 칸 기물 색이 행동 색과 같은가" 검사가 "자기 차례가
        /// 아닌 기물(상대 기물이거나, 자기 기물이라도 순서가 아닌 경우는 애초에 없지만)을 움직이려는
        /// 시도"를 표준적인 조건 실패(RuleViolationException)로 자연스럽게 걸러줍니다.
        /// </summary>
        private DomainAction CreateMoveAction(
            int p_nFromX, int p_nFromY, int p_nToX, int p_nToY, ChessCastlingRights p_stNewRights, (int X, int Y)? p_stNewEnPassantTarget)
        {
            Ont.E_PlayerColor eActiveColor = mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;

            DomainAction objAction = new DomainAction(
                "Action_MoveChessPiece",
                new Ont.ST_ActionData(
                    ChessActionCoding.EncodeSquare(p_nFromX, p_nFromY),
                    ChessActionCoding.EncodeSquare(p_nToX, p_nToY),
                    false,
                    eActiveColor));

            objAction.mv_lisConditions.Add(new Cond_LegalChessMove(m_stCastlingRights, m_stEnPassantTarget));

            objAction.mv_lisEffects.Add(new Effect_MoveChessPieceAndCapture());
            objAction.mv_lisEffects.Add(new Effect_HandleEnPassantCapture(m_stEnPassantTarget));
            objAction.mv_lisEffects.Add(new Effect_HandleCastlingRookMove());
            objAction.mv_lisEffects.Add(new Effect_HandlePromotion());
            objAction.mv_lisEffects.Add(new Effect_SwitchChessTurn());
            objAction.mv_lisEffects.Add(new Effect_CheckChessGameOver(p_stNewRights, p_stNewEnPassantTarget));

            return objAction;
        }
    }
}
