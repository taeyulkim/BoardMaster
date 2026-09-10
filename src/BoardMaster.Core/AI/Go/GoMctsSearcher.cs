namespace BoardMaster.Core.AI.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using GoRules = BoardMaster.Core.Rules.Go;

    /// <summary>
    /// 표준 UCT(Upper Confidence bounds applied to Trees) 몬테카를로 트리 탐색기입니다.
    ///
    /// 범위: 정책/가치망은 이 프로젝트의 범위 밖(설계서 5장 텐서 어댑터는 신경망 "입력"만 정의)이므로,
    /// 여기서는 확장 시 자식을 무작위로 고르고 시뮬레이션(롤아웃)도 균등 무작위로 두는 "순수 MCTS"를
    /// 구현합니다. 경쟁력 있는 기력을 내는 것이 목적이 아니라, 지금까지 만든 규칙 엔진 전체
    /// (GoGameSession.Clone/GetLegalMoves, Cond_NotSuicide, Effect_CaptureStones, GoScoreCalculator)를
    /// 실제로 굴려서 검증하는 첫 번째 실전 사용처입니다.
    ///
    /// 트리 노드는 각각 자신만의 GoGameSession 복제본을 들고 있습니다 — 국면 전이 로직(캡처, 패,
    /// 종국, 계가)을 MCTS용으로 다시 구현하지 않고 이미 검증된 엔진을 그대로 재사용하기 위해서입니다.
    /// 이 방식은 트리 노드 하나마다 GameContext(격자 배열 포함) 하나씩을 할당하므로, "규칙 검증
    /// BFS/텐서 인코딩" 단계에서 지켰던 zero-allocation 원칙은 여기서는 적용하지 않았습니다 — 실제
    /// 병목이 되면 노드 풀링으로 최적화할 수 있는 지점이라고만 표시해 둡니다(NFR-3의 "객체 풀링"
    /// 대안).
    /// </summary>
    public sealed class GoMctsSearcher
    {
        private const double DEFAULT_EXPLORATION_CONSTANT = 1.41421356237; // sqrt(2), UCB1 표준값

        private readonly double m_dExplorationConstant;
        private readonly int m_nMaxRolloutMoves;

        public GoMctsSearcher(double p_dExplorationConstant = DEFAULT_EXPLORATION_CONSTANT, int p_nMaxRolloutMoves = 400)
        {
            m_dExplorationConstant = p_dExplorationConstant;
            m_nMaxRolloutMoves = p_nMaxRolloutMoves;
        }

        /// <summary>
        /// Search()를 돌려 최선의 수 하나만 필요로 하는 호출자를 위한 얇은 래퍼입니다.
        /// 후보수별 통계(왜 그 수를 골랐는지)까지 필요하면 Search()를 직접 쓰세요.
        /// </summary>
        public (int X, int Y, bool IsPass)? FindBestMove(GoRules.GoGameSession p_objRootSession, int p_nIterations, Random? p_objRandom = null)
        {
            return Search(p_objRootSession, p_nIterations, p_objRandom).BestMove;
        }

        /// <summary>
        /// p_objRootSession의 현재 국면에서 p_nIterations번의 선택-확장-시뮬레이션-역전파를 수행한 뒤,
        /// 최선의 수와 루트에서 실제로 펼쳐본 모든 후보 수의 통계(방문 횟수, 추정 승률)를 함께
        /// 반환합니다. 최선의 수는 방문 횟수가 가장 많은 루트 자식입니다(평균 승률보다 방문 횟수가
        /// 더 안정적인 최종 선택 기준이라는 것이 MCTS의 표준적인 결론입니다). p_objRootSession
        /// 자체는 절대 변경하지 않습니다 — 탐색은 항상 복제본 위에서만 이루어집니다.
        /// 이미 종료된 대국이거나 둘 곳이 전혀 없으면 BestMove가 null이고 CandidateMoves는 빈
        /// 목록입니다(둘 다 "패스밖에 없다"를 뜻합니다).
        /// </summary>
        public GoMctsSearchResult Search(GoRules.GoGameSession p_objRootSession, int p_nIterations, Random? p_objRandom = null)
        {
            if (p_objRootSession is null)
            {
                throw new ArgumentNullException(nameof(p_objRootSession));
            }

            if (p_nIterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(p_nIterations));
            }

            Random objRandom = p_objRandom ?? new Random();
            GoMctsNode objRoot = CreateNode(null, default, Ont.E_PlayerColor.None, p_objRootSession.Clone());

            if (objRoot.IsTerminal || objRoot.UntriedMoves.Count == 0)
            {
                return new GoMctsSearchResult(null, Array.Empty<GoMctsCandidateStat>());
            }

            for (int i = 0; i < p_nIterations; i++)
            {
                GoMctsNode objSelected = SelectToExpand(objRoot);
                GoMctsNode objExpanded = objSelected.IsTerminal ? objSelected : Expand(objSelected, objRandom);

                Ont.E_PlayerColor? eWinner = objExpanded.IsTerminal
                    ? DetermineWinner(objExpanded.Session.mv_objCurrentContext)
                    : Simulate(objExpanded, objRandom);

                Backpropagate(objExpanded, eWinner);
            }

            if (objRoot.Children.Count == 0)
            {
                return new GoMctsSearchResult(null, Array.Empty<GoMctsCandidateStat>());
            }

            List<GoMctsCandidateStat> lisCandidates = new List<GoMctsCandidateStat>(objRoot.Children.Count);
            GoMctsNode objBestChild = objRoot.Children[0];

            foreach (GoMctsNode objChild in objRoot.Children)
            {
                double dWinRate = objChild.VisitCount > 0 ? objChild.TotalReward / objChild.VisitCount : 0.0;
                lisCandidates.Add(new GoMctsCandidateStat(objChild.Move, objChild.VisitCount, dWinRate));

                if (objChild.VisitCount > objBestChild.VisitCount)
                {
                    objBestChild = objChild;
                }
            }

            lisCandidates.Sort((a, b) => b.VisitCount.CompareTo(a.VisitCount));

            return new GoMctsSearchResult(objBestChild.Move, lisCandidates);
        }

        private GoMctsNode SelectToExpand(GoMctsNode p_objNode)
        {
            GoMctsNode objCurrent = p_objNode;

            while (!objCurrent.IsTerminal && objCurrent.UntriedMoves.Count == 0 && objCurrent.Children.Count > 0)
            {
                objCurrent = SelectBestChildByUcb(objCurrent);
            }

            return objCurrent;
        }

        private GoMctsNode SelectBestChildByUcb(GoMctsNode p_objNode)
        {
            GoMctsNode? objBest = null;
            double dBestScore = double.NegativeInfinity;

            for (int i = 0; i < p_objNode.Children.Count; i++)
            {
                GoMctsNode objChild = p_objNode.Children[i];
                double dExploitation = objChild.TotalReward / objChild.VisitCount;
                double dExploration = m_dExplorationConstant * Math.Sqrt(Math.Log(p_objNode.VisitCount) / objChild.VisitCount);
                double dScore = dExploitation + dExploration;

                if (dScore > dBestScore)
                {
                    dBestScore = dScore;
                    objBest = objChild;
                }
            }

            return objBest!; // 호출 시점에 p_objNode.Children.Count > 0이 보장된다.
        }

        private static GoMctsNode Expand(GoMctsNode p_objNode, Random p_objRandom)
        {
            int nIndex = p_objRandom.Next(p_objNode.UntriedMoves.Count);
            (int X, int Y, bool IsPass) stMove = p_objNode.UntriedMoves[nIndex];
            p_objNode.UntriedMoves.RemoveAt(nIndex);

            GoRules.GoGameSession objChildSession = p_objNode.Session.Clone();
            Ont.E_PlayerColor eMoverColor = objChildSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;

            if (stMove.IsPass)
            {
                objChildSession.Pass();
            }
            else
            {
                objChildSession.PlayStone(stMove.X, stMove.Y);
            }

            GoMctsNode objChild = CreateNode(p_objNode, stMove, eMoverColor, objChildSession);
            p_objNode.Children.Add(objChild);
            return objChild;
        }

        private Ont.E_PlayerColor? Simulate(GoMctsNode p_objNode, Random p_objRandom)
        {
            GoRules.GoGameSession objRolloutSession = p_objNode.Session.Clone();
            int nMoveCount = 0;

            while (!objRolloutSession.mv_objCurrentContext.mv_isGameOver && nMoveCount < m_nMaxRolloutMoves)
            {
                List<(int X, int Y)> lisLegalMoves = objRolloutSession.GetLegalMoves();

                if (lisLegalMoves.Count == 0)
                {
                    objRolloutSession.Pass();
                }
                else
                {
                    // 후보 = 착수 후보 전체 + 패스 하나. 균등 무작위 선택이라 둘 곳이 많을수록 패스
                    // 확률은 낮아지고, 반상이 채워져 후보가 줄수록 자연히 패스가 잦아진다.
                    int nChoice = p_objRandom.Next(lisLegalMoves.Count + 1);
                    if (nChoice == lisLegalMoves.Count)
                    {
                        objRolloutSession.Pass();
                    }
                    else
                    {
                        (int X, int Y) = lisLegalMoves[nChoice];
                        objRolloutSession.PlayStone(X, Y);
                    }
                }

                nMoveCount++;
            }

            return DetermineWinner(objRolloutSession.mv_objCurrentContext);
        }

        /// <summary>
        /// 리프에서 루트까지 거슬러 올라가며 방문 횟수와 보상을 누적합니다. 각 노드의 보상은
        /// "그 노드를 만든 착수를 둔 플레이어(node.MoverColor)" 시점 기준입니다 — 그래서 자식을
        /// 고를 때는 부모 시점의 부호 반전 없이 자식의 보상을 그대로 최대화하면 됩니다.
        /// </summary>
        private static void Backpropagate(GoMctsNode p_objLeaf, Ont.E_PlayerColor? p_eWinner)
        {
            GoMctsNode? objCurrent = p_objLeaf;

            while (objCurrent != null)
            {
                objCurrent.VisitCount++;

                if (p_eWinner is null)
                {
                    objCurrent.TotalReward += 0.5; // 무승부
                }
                else if (objCurrent.MoverColor == p_eWinner.Value)
                {
                    objCurrent.TotalReward += 1.0;
                }
                // 패배는 보상 0 (더할 것 없음).

                objCurrent = objCurrent.Parent;
            }
        }

        private static Ont.E_PlayerColor? DetermineWinner(Ont.GameContext p_objContext)
        {
            GoRules.GoScoreCalculator objCalculator = new GoRules.GoScoreCalculator();
            GoRules.ST_GoScoreResult stResult = objCalculator.CalculateScore(p_objContext);

            if (stResult.m_nBlackScore > stResult.m_nWhiteScore)
            {
                return Ont.E_PlayerColor.Black;
            }

            if (stResult.m_nWhiteScore > stResult.m_nBlackScore)
            {
                return Ont.E_PlayerColor.White;
            }

            return null;
        }

        private static GoMctsNode CreateNode(
            GoMctsNode? p_objParent,
            (int X, int Y, bool IsPass) p_stMove,
            Ont.E_PlayerColor p_eMoverColor,
            GoRules.GoGameSession p_objSession)
        {
            return new GoMctsNode
            {
                Parent = p_objParent,
                Move = p_stMove,
                MoverColor = p_eMoverColor,
                Session = p_objSession,
                UntriedMoves = BuildCandidateMoves(p_objSession)
            };
        }

        private static List<(int X, int Y, bool IsPass)> BuildCandidateMoves(GoRules.GoGameSession p_objSession)
        {
            List<(int X, int Y, bool IsPass)> lisMoves = new List<(int X, int Y, bool IsPass)>();

            if (p_objSession.mv_objCurrentContext.mv_isGameOver)
            {
                return lisMoves;
            }

            foreach ((int X, int Y) stPlacement in p_objSession.GetLegalMoves())
            {
                lisMoves.Add((stPlacement.X, stPlacement.Y, false));
            }

            lisMoves.Add((0, 0, true)); // 패스는 항상 후보에 포함된다(좌표는 의미 없음).

            return lisMoves;
        }

        /// <summary>
        /// 탐색 트리의 한 노드입니다. GoMctsSearcher 밖에서는 쓰이지 않는 구현 세부 사항이라 nested
        /// private class로 감췄습니다.
        /// </summary>
        private sealed class GoMctsNode
        {
            public GoMctsNode? Parent;
            public List<GoMctsNode> Children = new();
            public (int X, int Y, bool IsPass) Move;
            public Ont.E_PlayerColor MoverColor;
            public GoRules.GoGameSession Session = null!;
            public List<(int X, int Y, bool IsPass)> UntriedMoves = null!;
            public int VisitCount;
            public double TotalReward;

            public bool IsTerminal => Session.mv_objCurrentContext.mv_isGameOver;
        }
    }
}
