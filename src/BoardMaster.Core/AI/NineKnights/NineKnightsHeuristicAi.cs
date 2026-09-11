namespace BoardMaster.Core.AI.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using NkRules = BoardMaster.Core.Rules.NineKnights;

    /// <summary>
    /// 나인 나이츠는 상대 기물의 번호가 전투 전까지 계속 비공개인 게임입니다 — GoMctsSearcher/
    /// ChessMctsSearcher 같은 표준 MCTS는 "지금 국면이 완전히 공개되어 있다"고 전제하므로 그대로
    /// 쓸 수 없습니다(제대로 하려면 Information-Set MCTS 같은 훨씬 큰 별도 연구가 필요합니다).
    /// 그렇다고 AI가 상대 기물의 실제 번호를 몰래 들여다보게 하는 건 "정직한 AI"라는 이 프로젝트의
    /// 원칙에 어긋나므로, 여기서는 한 수 앞만 내다보는 정직한 기대값 휴리스틱을 씁니다:
    ///   - 내 기물의 번호(항상 앎), 모든 기물의 위치(항상 공개), 그리고 session.IsRevealed로 걸러진
    ///     "전투로 이미 공개된 적 기물의 번호"만 참고합니다.
    ///   - 아직 공개되지 않은 적 기물을 공격할지는, "상대가 이미 공개한 번호들을 빼고 남은 1~9 중
    ///     그 적이 무엇일지 균등하게 있을 수 있다"는 정직한 확률 추정으로 기대 승률을 계산해
    ///     판단합니다(진짜 숫자를 알아서가 아니라 통계적으로 추정한 것입니다).
    /// 상대의 비밀 임무/히든 토큰 번호는 이 클래스가 아예 건드리지 않습니다 — session에서 그 값을
    /// 조회하는 메서드(GetMissionNumber/GetHiddenNumber)는 "내 색"으로만 호출합니다.
    /// </summary>
    public sealed class NineKnightsHeuristicAi
    {
        public (int FromX, int FromY, int ToX, int ToY)? ChooseMove(
            NkRules.NineKnightsGameSession p_objSession, Ont.E_PlayerColor p_eMyColor, Random p_objRandom)
        {
            List<(int FromX, int FromY, int ToX, int ToY)> lisCandidates = p_objSession.GetAllLegalMoves(p_eMyColor);
            if (lisCandidates.Count == 0)
            {
                return null;
            }

            int nMyMission = p_objSession.GetMissionNumber(p_eMyColor);
            int nMyHidden = p_objSession.GetHiddenNumber(p_eMyColor);
            Ont.E_PlayerColor eOpponentColor = NkRules.NineKnightsBoardGeometry.Opponent(p_eMyColor);
            List<int> lisUnknownOpponentNumbers = BuildUnknownOpponentNumbers(p_objSession, eOpponentColor);
            int nOpponentRemainingCount = p_objSession.GetRemainingPieceCount(eOpponentColor);

            (int FromX, int FromY, int ToX, int ToY) stBestMove = lisCandidates[0];
            double dBestScore = double.NegativeInfinity;

            foreach ((int FromX, int FromY, int ToX, int ToY) stMove in lisCandidates)
            {
                double dScore = EvaluateMove(
                    p_objSession, stMove, p_eMyColor, nMyMission, nMyHidden, lisUnknownOpponentNumbers, nOpponentRemainingCount, p_objRandom);

                if (dScore > dBestScore)
                {
                    dBestScore = dScore;
                    stBestMove = stMove;
                }
            }

            return stBestMove;
        }

        private static double EvaluateMove(
            NkRules.NineKnightsGameSession p_objSession,
            (int FromX, int FromY, int ToX, int ToY) p_stMove,
            Ont.E_PlayerColor p_eMyColor,
            int p_nMyMission,
            int p_nMyHidden,
            List<int> p_lisUnknownOpponentNumbers,
            int p_nOpponentRemainingCount,
            Random p_objRandom)
        {
            Ont.Entity objMover = p_objSession.GetPieceAt(p_stMove.FromX, p_stMove.FromY)!;
            int nMoverNumber = int.Parse(objMover.mv_strType);
            double dJitter = p_objRandom.NextDouble() * 2.0;

            bool bReachesOpponentBackRow = p_stMove.ToY == NkRules.NineKnightsBoardGeometry.OpponentBackRow(p_eMyColor);
            bool bWouldWinByMission = bReachesOpponentBackRow && nMoverNumber == p_nMyMission;

            Ont.Entity? objDefender = p_objSession.GetPieceAt(p_stMove.ToX, p_stMove.ToY);

            if (objDefender is null)
            {
                if (bWouldWinByMission)
                {
                    return 10000.0; // 빈 상대 뒷줄에 내 임무 기사가 도착 — 확정 승리.
                }

                double dProgress = 0.0;
                if (nMoverNumber == p_nMyMission)
                {
                    int nRowsToGo = Math.Abs(p_stMove.ToY - NkRules.NineKnightsBoardGeometry.OpponentBackRow(p_eMyColor));
                    dProgress = (8 - nRowsToGo) * 3.0; // 임무 기사는 목표 줄에 가까워질수록 우대한다.
                }

                return dProgress + dJitter;
            }

            bool bWouldEliminateOpponent = p_nOpponentRemainingCount == 1;

            if (p_objSession.IsRevealed(objDefender.mv_strEntityID))
            {
                int nDefenderNumber = int.Parse(objDefender.mv_strType);
                bool bWins = NkRules.NineKnightsCombatRules.AttackerWins(nMoverNumber, nDefenderNumber, p_nMyHidden);

                if (!bWins)
                {
                    return -10000.0; // 확정 패배는 피한다.
                }

                if (bWouldWinByMission || bWouldEliminateOpponent)
                {
                    return 10000.0;
                }

                return 500.0 + nDefenderNumber; // 확정 승리 — 더 값진(번호 높은) 적을 우선 잡는다.
            }

            // 아직 공개되지 않은 적 — 남은 미확인 번호 풀에서 균등 분포라고 정직하게 가정하고
            // 기대 승률을 계산한다(진짜 번호를 보고 판단하는 게 아니다).
            double dWinProbability = EstimateWinProbability(nMoverNumber, p_nMyHidden, p_lisUnknownOpponentNumbers);
            double dScore = (dWinProbability - 0.5) * 800.0;

            if (bWouldWinByMission || bWouldEliminateOpponent)
            {
                dScore += dWinProbability * 5000.0; // 이기기만 하면 승리로 이어지므로 확신도에 비례해 가산한다.
            }

            return dScore + dJitter;
        }

        private static double EstimateWinProbability(int p_nAttackerNumber, int p_nAttackerHidden, List<int> p_lisUnknownOpponentNumbers)
        {
            if (p_lisUnknownOpponentNumbers.Count == 0)
            {
                return 0.5; // 정보가 전혀 없으면(이론상 거의 안 생김) 중립으로 취급한다.
            }

            int nWinCount = 0;
            foreach (int nCandidateNumber in p_lisUnknownOpponentNumbers)
            {
                if (NkRules.NineKnightsCombatRules.AttackerWins(p_nAttackerNumber, nCandidateNumber, p_nAttackerHidden))
                {
                    nWinCount++;
                }
            }

            return (double)nWinCount / p_lisUnknownOpponentNumbers.Count;
        }

        /// <summary>
        /// 상대가 아직 한 번도 전투에 참여하지 않은 기물들의 번호가 "1~9 중 상대가 이미 공개한
        /// 번호를 뺀 나머지"라는, 정직한 추론입니다 — 실제 번호를 직접 읽지 않고, session.IsRevealed로
        /// 걸러진 기물만 참고합니다. 반상 위 기물뿐 아니라 이미 잡혀서 치워진 기물도 훑습니다 —
        /// 잡힐 당시 전투로 이미 번호가 공개됐었기 때문입니다(반상에 없다고 정보가 사라지지 않습니다).
        /// </summary>
        private static List<int> BuildUnknownOpponentNumbers(NkRules.NineKnightsGameSession p_objSession, Ont.E_PlayerColor p_eOpponentColor)
        {
            HashSet<int> setKnownNumbers = new HashSet<int>();

            foreach (Ont.Entity objPiece in p_objSession.mv_objCurrentContext.mv_lisEntities)
            {
                if (objPiece.mv_eColor == p_eOpponentColor && p_objSession.IsRevealed(objPiece.mv_strEntityID))
                {
                    setKnownNumbers.Add(int.Parse(objPiece.mv_strType));
                }
            }

            List<int> lisUnknown = new List<int>();
            for (int n = 1; n <= 9; n++)
            {
                if (!setKnownNumbers.Contains(n))
                {
                    lisUnknown.Add(n);
                }
            }

            return lisUnknown;
        }
    }
}
