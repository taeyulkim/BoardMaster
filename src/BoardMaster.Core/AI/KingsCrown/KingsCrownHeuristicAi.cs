namespace BoardMaster.Core.AI.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;
    using KcRules = BoardMaster.Core.Rules.KingsCrown;

    /// <summary>
    /// 킹스 크라운은 상대가 어떤 숫자칩을 보유하고 있는지 끝까지 비공개인 게임입니다(반상에 놓인
    /// 왕관만 공개 정보). 정직한(cheating 없는) AI 원칙에 따라, 이 클래스는 세션의
    /// GetHeldChips(자기 색)와 GetPieceAt 같은 공개 정보만 참고하고 상대의 보유 숫자칩은 절대
    /// 들여다보지 않는 한 수 앞 휴리스틱입니다. Nine Knights의 AI와 같은 설계 원칙입니다.
    ///
    /// 평가 기준: (1) 이번 수로 즉시 빙고가 완성되면 압도적으로 우선한다. (2) 그렇지 않으면 이
    /// 칸을 지나는 12개 줄 각각에 대해 "내 왕관으로 이미 채워진 칸 수(공격 가치, 상대 왕관이 하나라도
    /// 있으면 그 줄은 나에게 영원히 막혔으므로 0)"와 "상대 왕관으로 이미 채워진 칸 수(방어 가치 —
    /// 여기 놓으면 상대의 그 줄을 원천 봉쇄)"를 제곱 가중치로 합산한다. 둘 다 반상에 이미 공개된
    /// 정보만으로 계산되므로 상대의 숨겨진 패를 몰라도 정직하게 판단할 수 있다.
    /// </summary>
    public sealed class KingsCrownHeuristicAi
    {
        private const double DEFENSE_WEIGHT = 1.5;

        public (int ChipValue, int X, int Y)? ChooseMove(
            KcRules.KingsCrownGameSession p_objSession, Ont.E_PlayerColor p_eMyColor, Random p_objRandom)
        {
            List<(int ChipValue, int X, int Y)> lisCandidates = p_objSession.GetLegalPlacements(p_eMyColor);
            if (lisCandidates.Count == 0)
            {
                return null;
            }

            (int ChipValue, int X, int Y) stBestMove = lisCandidates[0];
            double dBestScore = double.NegativeInfinity;

            foreach ((int ChipValue, int X, int Y) stMove in lisCandidates)
            {
                double dScore = EvaluateMove(p_objSession.mv_objCurrentContext, stMove, p_eMyColor, p_objRandom);
                if (dScore > dBestScore)
                {
                    dBestScore = dScore;
                    stBestMove = stMove;
                }
            }

            return stBestMove;
        }

        private static double EvaluateMove(
            Ont.GameContext p_objContext, (int ChipValue, int X, int Y) p_stMove, Ont.E_PlayerColor p_eMyColor, Random p_objRandom)
        {
            if (KcRules.KingsCrownWinRules.WouldCompleteBingo(p_objContext, p_stMove.X, p_stMove.Y, p_eMyColor))
            {
                return 10000.0;
            }

            double dScore = 0.0;

            foreach (IReadOnlyList<(int X, int Y)> lisLine in KcRules.KingsCrownBoardGeometry.Lines)
            {
                if (!ContainsCell(lisLine, p_stMove.X, p_stMove.Y))
                {
                    continue;
                }

                (int nMySupport, int nOpponentSupport) = CountLineSupport(p_objContext, lisLine, p_stMove.X, p_stMove.Y, p_eMyColor);

                if (nOpponentSupport == 0)
                {
                    dScore += nMySupport * nMySupport;
                }
                else
                {
                    dScore += nOpponentSupport * nOpponentSupport * DEFENSE_WEIGHT;
                }
            }

            dScore += p_objRandom.NextDouble() * 2.0;
            return dScore;
        }

        /// <summary>이 줄에서 (X,Y)에 내 왕관을 놓는다고 가정했을 때, 내 왕관(가운데 칸 포함)으로 채워진
        /// 칸 수와 상대 왕관으로 채워진 칸 수를 센다. 상대 왕관이 하나라도 있으면 내 공격 가치는
        /// 의미가 없다(그 줄은 나에게 영원히 막혔다) — 호출부에서 판단한다.</summary>
        private static (int MySupport, int OpponentSupport) CountLineSupport(
            Ont.GameContext p_objContext, IReadOnlyList<(int X, int Y)> p_lisLine, int p_nMoveX, int p_nMoveY, Ont.E_PlayerColor p_eMyColor)
        {
            int nMySupport = 0;
            int nOpponentSupport = 0;

            foreach ((int X, int Y) in p_lisLine)
            {
                if (X == p_nMoveX && Y == p_nMoveY)
                {
                    nMySupport++;
                    continue;
                }
                if (KcRules.KingsCrownBoardGeometry.IsCenter(X, Y))
                {
                    nMySupport++;
                    continue;
                }

                Ont.Entity? objPiece = KcRules.KingsCrownZoneQuery.FindPieceAt(p_objContext, X, Y);
                if (objPiece is null)
                {
                    continue;
                }

                if (objPiece.mv_eColor == p_eMyColor)
                {
                    nMySupport++;
                }
                else
                {
                    nOpponentSupport++;
                }
            }

            return (nMySupport, nOpponentSupport);
        }

        private static bool ContainsCell(IReadOnlyList<(int X, int Y)> p_lisLine, int p_nX, int p_nY)
        {
            foreach ((int X, int Y) in p_lisLine)
            {
                if (X == p_nX && Y == p_nY)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
