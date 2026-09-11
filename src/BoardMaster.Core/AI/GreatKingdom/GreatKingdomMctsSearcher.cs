namespace BoardMaster.Core.AI.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using GkRules = BoardMaster.Core.Rules.GreatKingdom;

    /// <summary>
    /// GoMctsSearcher와 구조가 거의 동일한 표준 UCT 순수 MCTS입니다(정책/가치망 없음, 무작위 롤아웃).
    /// 수의 모양(X,Y,IsPass)이 Go와 같아서 트리 노드/선택/확장/역전파 로직은 Go와 사실상 동일하고,
    /// DetermineWinner만 이 장르의 종국 규칙에 맞춰 달라집니다:
    ///   - 실제로 mv_isGameOver가 true인 경우(포위 즉시 승리든 패스 후 영토 계가든), 이미
    ///     PlayerState.mv_nScore에 승자(1)/패자(0)가 정확히 채워져 있으므로 그걸 그대로 믿습니다.
    ///     포위로 끝난 국면은 영토가 근소해도 잡은 쪽이 무조건 이기므로, 영토를 다시 계산해
    ///     판단하면 틀릴 수 있습니다.
    ///   - 롤아웃이 한도(m_nMaxRolloutMoves)에 걸려 아직 안 끝난 경우에만
    ///     GreatKingdomScoreCalculator로 "지금 이대로 끝난다면"의 영토 우세를 추정합니다 — Go가
    ///     GoScoreCalculator를 중간 평가로도 쓰는 것과 같은 발상입니다.
    /// </summary>
    public sealed class GreatKingdomMctsSearcher
    {
        private const double DEFAULT_EXPLORATION_CONSTANT = 1.41421356237; // sqrt(2), UCB1 표준값

        private readonly double m_dExplorationConstant;
        private readonly int m_nMaxRolloutMoves;
        private readonly GkRules.GreatKingdomScoreCalculator m_objScoreCalculator = new GkRules.GreatKingdomScoreCalculator();

        public GreatKingdomMctsSearcher(double p_dExplorationConstant = DEFAULT_EXPLORATION_CONSTANT, int p_nMaxRolloutMoves = 200)
        {
            m_dExplorationConstant = p_dExplorationConstant;
            m_nMaxRolloutMoves = p_nMaxRolloutMoves;
        }

        public (int X, int Y, bool IsPass)? FindBestMove(GkRules.GreatKingdomGameSession p_objRootSession, int p_nIterations, Random? p_objRandom = null)
        {
            return Search(p_objRootSession, p_nIterations, p_objRandom).BestMove;
        }

        public GreatKingdomMctsSearchResult Search(GkRules.GreatKingdomGameSession p_objRootSession, int p_nIterations, Random? p_objRandom = null)
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
            GreatKingdomMctsNode objRoot = CreateNode(null, default, Ont.E_PlayerColor.None, p_objRootSession.Clone());

            if (objRoot.IsTerminal || objRoot.UntriedMoves.Count == 0)
            {
                return new GreatKingdomMctsSearchResult(null, Array.Empty<GreatKingdomMctsCandidateStat>());
            }

            for (int i = 0; i < p_nIterations; i++)
            {
                GreatKingdomMctsNode objSelected = SelectToExpand(objRoot);
                GreatKingdomMctsNode objExpanded = objSelected.IsTerminal ? objSelected : Expand(objSelected, objRandom);

                Ont.E_PlayerColor? eWinner = objExpanded.IsTerminal
                    ? DetermineWinner(objExpanded.Session.mv_objCurrentContext)
                    : Simulate(objExpanded, objRandom);

                Backpropagate(objExpanded, eWinner);
            }

            if (objRoot.Children.Count == 0)
            {
                return new GreatKingdomMctsSearchResult(null, Array.Empty<GreatKingdomMctsCandidateStat>());
            }

            List<GreatKingdomMctsCandidateStat> lisCandidates = new List<GreatKingdomMctsCandidateStat>(objRoot.Children.Count);
            GreatKingdomMctsNode objBestChild = objRoot.Children[0];

            foreach (GreatKingdomMctsNode objChild in objRoot.Children)
            {
                double dWinRate = objChild.VisitCount > 0 ? objChild.TotalReward / objChild.VisitCount : 0.0;
                lisCandidates.Add(new GreatKingdomMctsCandidateStat(objChild.Move, objChild.VisitCount, dWinRate));

                if (objChild.VisitCount > objBestChild.VisitCount)
                {
                    objBestChild = objChild;
                }
            }

            lisCandidates.Sort((a, b) => b.VisitCount.CompareTo(a.VisitCount));

            return new GreatKingdomMctsSearchResult(objBestChild.Move, lisCandidates);
        }

        private GreatKingdomMctsNode SelectToExpand(GreatKingdomMctsNode p_objNode)
        {
            GreatKingdomMctsNode objCurrent = p_objNode;

            while (!objCurrent.IsTerminal && objCurrent.UntriedMoves.Count == 0 && objCurrent.Children.Count > 0)
            {
                objCurrent = SelectBestChildByUcb(objCurrent);
            }

            return objCurrent;
        }

        private GreatKingdomMctsNode SelectBestChildByUcb(GreatKingdomMctsNode p_objNode)
        {
            GreatKingdomMctsNode? objBest = null;
            double dBestScore = double.NegativeInfinity;

            for (int i = 0; i < p_objNode.Children.Count; i++)
            {
                GreatKingdomMctsNode objChild = p_objNode.Children[i];
                double dExploitation = objChild.TotalReward / objChild.VisitCount;
                double dExploration = m_dExplorationConstant * Math.Sqrt(Math.Log(p_objNode.VisitCount) / objChild.VisitCount);
                double dScore = dExploitation + dExploration;

                if (dScore > dBestScore)
                {
                    dBestScore = dScore;
                    objBest = objChild;
                }
            }

            return objBest!;
        }

        private static GreatKingdomMctsNode Expand(GreatKingdomMctsNode p_objNode, Random p_objRandom)
        {
            int nIndex = p_objRandom.Next(p_objNode.UntriedMoves.Count);
            (int X, int Y, bool IsPass) stMove = p_objNode.UntriedMoves[nIndex];
            p_objNode.UntriedMoves.RemoveAt(nIndex);

            GkRules.GreatKingdomGameSession objChildSession = p_objNode.Session.Clone();
            Ont.E_PlayerColor eMoverColor = objChildSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;

            if (stMove.IsPass)
            {
                objChildSession.Pass();
            }
            else
            {
                objChildSession.PlaceStone(stMove.X, stMove.Y);
            }

            GreatKingdomMctsNode objChild = CreateNode(p_objNode, stMove, eMoverColor, objChildSession);
            p_objNode.Children.Add(objChild);
            return objChild;
        }

        private Ont.E_PlayerColor? Simulate(GreatKingdomMctsNode p_objNode, Random p_objRandom)
        {
            GkRules.GreatKingdomGameSession objRolloutSession = p_objNode.Session.Clone();
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
                    int nChoice = p_objRandom.Next(lisLegalMoves.Count + 1);
                    if (nChoice == lisLegalMoves.Count)
                    {
                        objRolloutSession.Pass();
                    }
                    else
                    {
                        (int X, int Y) = lisLegalMoves[nChoice];
                        objRolloutSession.PlaceStone(X, Y);
                    }
                }

                nMoveCount++;
            }

            return DetermineWinner(objRolloutSession.mv_objCurrentContext);
        }

        private Ont.E_PlayerColor? DetermineWinner(Ont.GameContext p_objContext)
        {
            if (p_objContext.mv_isGameOver)
            {
                Ont.PlayerState objPlayer1 = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
                Ont.PlayerState objPlayer2 = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;

                if (objPlayer1.mv_nScore > objPlayer2.mv_nScore)
                {
                    return Ont.E_PlayerColor.Black;
                }

                if (objPlayer2.mv_nScore > objPlayer1.mv_nScore)
                {
                    return Ont.E_PlayerColor.White;
                }

                return null;
            }

            GkRules.ST_GreatKingdomScoreResult stResult = m_objScoreCalculator.CalculateScore(p_objContext);
            return stResult.m_bPlayer1Wins ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
        }

        private static void Backpropagate(GreatKingdomMctsNode p_objLeaf, Ont.E_PlayerColor? p_eWinner)
        {
            GreatKingdomMctsNode? objCurrent = p_objLeaf;

            while (objCurrent != null)
            {
                objCurrent.VisitCount++;

                if (p_eWinner is null)
                {
                    objCurrent.TotalReward += 0.5;
                }
                else if (objCurrent.MoverColor == p_eWinner.Value)
                {
                    objCurrent.TotalReward += 1.0;
                }

                objCurrent = objCurrent.Parent;
            }
        }

        private static GreatKingdomMctsNode CreateNode(
            GreatKingdomMctsNode? p_objParent,
            (int X, int Y, bool IsPass) p_stMove,
            Ont.E_PlayerColor p_eMoverColor,
            GkRules.GreatKingdomGameSession p_objSession)
        {
            return new GreatKingdomMctsNode
            {
                Parent = p_objParent,
                Move = p_stMove,
                MoverColor = p_eMoverColor,
                Session = p_objSession,
                UntriedMoves = BuildCandidateMoves(p_objSession)
            };
        }

        private static List<(int X, int Y, bool IsPass)> BuildCandidateMoves(GkRules.GreatKingdomGameSession p_objSession)
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

            lisMoves.Add((0, 0, true));

            return lisMoves;
        }

        private sealed class GreatKingdomMctsNode
        {
            public GreatKingdomMctsNode? Parent;
            public List<GreatKingdomMctsNode> Children = new();
            public (int X, int Y, bool IsPass) Move;
            public Ont.E_PlayerColor MoverColor;
            public GkRules.GreatKingdomGameSession Session = null!;
            public List<(int X, int Y, bool IsPass)> UntriedMoves = null!;
            public int VisitCount;
            public double TotalReward;

            public bool IsTerminal => Session.mv_objCurrentContext.mv_isGameOver;
        }
    }
}
