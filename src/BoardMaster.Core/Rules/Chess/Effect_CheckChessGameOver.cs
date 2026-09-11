namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 턴 전환 직후(그래서 mv_stCurrentState.m_eActiveColor는 이제 "다음에 둘 색") 그 색이 둘 수
    /// 있는 합법수가 하나도 없는지 확인합니다. 없다면: 체크 상태면 체크메이트(방금 둔 쪽이 승리,
    /// PlayerState.mv_nScore에 1점을 기록해 Go/War와 같은 방식으로 승자를 표시), 체크가 아니면
    /// 스테일메이트(무승부, 아무도 점수를 얻지 않음)로 종국 처리합니다.
    ///
    /// 새 캐슬링 권리/앙파상 대상 칸은 "이번 수를 반영한 다음 상태"라 GameContext에 아직 없는
    /// 값이므로(Cond_LegalChessMove가 쓰는 "이번 수 검증용" 이전 상태와는 다릅니다) ChessGameSession이
    /// 미리 계산해 생성자로 주입합니다.
    ///
    /// 참고: 이 구현은 체크메이트/스테일메이트만 종국 조건으로 봅니다 — 쓰리폴드 반복이나 50수
    /// 규칙 같은 무승부 조건은 의도적으로 생략했습니다(War가 전쟁 서브 배틀을 생략한 것과 같은 성격의
    /// 단순화입니다). 실전에서는 거의 항상 체크메이트/스테일메이트로 게임이 끝나므로 큰 제약은
    /// 아니지만, 완전한 FIDE 규칙은 아닙니다.
    /// </summary>
    public sealed class Effect_CheckChessGameOver : Ont.IEffect
    {
        private readonly ChessCastlingRights m_stNewRights;
        private readonly (int X, int Y)? m_stNewEnPassantTarget;

        internal Effect_CheckChessGameOver(ChessCastlingRights p_stNewRights, (int X, int Y)? p_stNewEnPassantTarget)
        {
            m_stNewRights = p_stNewRights;
            m_stNewEnPassantTarget = p_stNewEnPassantTarget;
        }

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            Ont.E_PlayerColor eColorToMoveNext = p_objContext.mv_stCurrentState.m_eActiveColor;
            if (ChessMoveGenerator.HasAnyLegalMove(p_objContext, eColorToMoveNext, m_stNewRights, m_stNewEnPassantTarget))
            {
                return p_objContext;
            }

            p_objContext.mv_isGameOver = true;

            if (ChessMoveGenerator.IsInCheck(p_objContext, eColorToMoveNext))
            {
                Ont.E_PlayerColor eWinner = eColorToMoveNext == Ont.E_PlayerColor.White ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
                Ont.PlayerState? objWinnerState = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == eWinner);
                if (objWinnerState is not null)
                {
                    objWinnerState.mv_nScore = 1; // 체크메이트: 방금 둔 쪽이 승리.
                }
            }
            // 체크가 아니면 스테일메이트 — 무승부이므로 점수는 그대로 둔다.

            return p_objContext;
        }
    }
}
