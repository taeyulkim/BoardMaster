namespace BoardMaster.Core.AI.Chess
{
    using Ont = BoardMaster.Core.Ontology;
    using ChessRules = BoardMaster.Core.Rules.Chess;

    /// <summary>
    /// GoMctsSearcher와 같은 표준 UCT 순수 MCTS입니다 — 정책/가치망 없이 자식은 무작위로 고르고
    /// 롤아웃도 균등 무작위입니다. 트리 노드마다 ChessGameSession 복제본을 들고 있어 규칙 엔진
    /// (ChessMoveGenerator, 캐슬링/앙파상/승진/체크메이트 판정)을 전혀 재구현하지 않습니다.
    ///
    /// Go와 다른 점: 체스는 체크메이트/스테일메이트가 아니면 끝나지 않아서(쓰리폴드 반복/50수
    /// 규칙 없음), 롤아웃이 한도(m_nMaxRolloutMoves)에 걸려도 게임이 실제로는 안 끝났을 수
    /// 있습니다 — 이때는 Go의 GoScoreCalculator 같은 지형 기반 중간 평가가 없으므로, 대신 간단한
    /// 기물 점수 합(폰1/나이트3/비숍3/룩5/퀸9)으로 우세를 추정합니다(EstimateMaterialWinner).
    /// 실제 체크메이트/스테일메이트로 끝난 경우는 이 추정 없이 PlayerState.mv_nScore를 그대로 믿습니다.
    /// </summary>
    public sealed class ChessMctsSearcher
    {
        private const double DEFAULT_EXPLORATION_CONSTANT = 1.41421356237; // sqrt(2), UCB1 표준값

        private static readonly Dictionary<string, int> s_dicPieceValues = new()
        {
            [ChessRules.ChessPieceType.Pawn] = 1,
            [ChessRules.ChessPieceType.Knight] = 3,
            [ChessRules.ChessPieceType.Bishop] = 3,
            [ChessRules.ChessPieceType.Rook] = 5,
            [ChessRules.ChessPieceType.Queen] = 9,
            [ChessRules.ChessPieceType.King] = 0
        };

        private readonly double m_dExplorationConstant;
        private readonly int m_nMaxRolloutMoves;

        public ChessMctsSearcher(double p_dExplorationConstant = DEFAULT_EXPLORATION_CONSTANT, int p_nMaxRolloutMoves = 120)
        {
            m_dExplorationConstant = p_dExplorationConstant;
            m_nMaxRolloutMoves = p_nMaxRolloutMoves;
        }

        /// <summary>Search()를 돌려 최선의 수 하나만 필요로 하는 호출자를 위한 얇은 래퍼입니다.</summary>
        public (int FromX, int FromY, int ToX, int ToY)? FindBestMove(
            ChessRules.ChessGameSession p_objRootSession, int p_nIterations, Random? p_objRandom = null)
        {
            return Search(p_objRootSession, p_nIterations, p_objRandom).BestMove;
        }

        /// <summary>
        /// p_objRootSession의 현재 국면에서 p_nIterations번의 선택-확장-시뮬레이션-역전파를 수행한 뒤,
        /// 최선의 수와 루트에서 실제로 펼쳐본 모든 후보 수의 통계를 함께 반환합니다. p_objRootSession
        /// 자체는 절대 변경하지 않습니다. 이미 종료된 대국이면 BestMove가 null입니다.
        /// </summary>
        public ChessMctsSearchResult Search(ChessRules.ChessGameSession p_objRootSession, int p_nIterations, Random? p_objRandom = null)
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
            ChessMctsNode objRoot = CreateNode(null, default, Ont.E_PlayerColor.None, p_objRootSession.Clone());

            if (objRoot.IsTerminal || objRoot.UntriedMoves.Count == 0)
            {
                return new ChessMctsSearchResult(null, Array.Empty<ChessMctsCandidateStat>());
            }

            for (int i = 0; i < p_nIterations; i++)
            {
                ChessMctsNode objSelected = SelectToExpand(objRoot);
                ChessMctsNode objExpanded = objSelected.IsTerminal ? objSelected : Expand(objSelected, objRandom);

                Ont.GameContext objEvaluatedContext = objExpanded.IsTerminal
                    ? objExpanded.Session.mv_objCurrentContext
                    : Simulate(objExpanded, objRandom);

                Backpropagate(objExpanded, DetermineWinner(objEvaluatedContext));
            }

            if (objRoot.Children.Count == 0)
            {
                return new ChessMctsSearchResult(null, Array.Empty<ChessMctsCandidateStat>());
            }

            List<ChessMctsCandidateStat> lisCandidates = new List<ChessMctsCandidateStat>(objRoot.Children.Count);
            ChessMctsNode objBestChild = objRoot.Children[0];

            foreach (ChessMctsNode objChild in objRoot.Children)
            {
                double dWinRate = objChild.VisitCount > 0 ? objChild.TotalReward / objChild.VisitCount : 0.0;
                lisCandidates.Add(new ChessMctsCandidateStat(objChild.Move, objChild.VisitCount, dWinRate));

                if (objChild.VisitCount > objBestChild.VisitCount)
                {
                    objBestChild = objChild;
                }
            }

            lisCandidates.Sort((a, b) => b.VisitCount.CompareTo(a.VisitCount));

            return new ChessMctsSearchResult(objBestChild.Move, lisCandidates);
        }

        private ChessMctsNode SelectToExpand(ChessMctsNode p_objNode)
        {
            ChessMctsNode objCurrent = p_objNode;

            while (!objCurrent.IsTerminal && objCurrent.UntriedMoves.Count == 0 && objCurrent.Children.Count > 0)
            {
                objCurrent = SelectBestChildByUcb(objCurrent);
            }

            return objCurrent;
        }

        private ChessMctsNode SelectBestChildByUcb(ChessMctsNode p_objNode)
        {
            ChessMctsNode? objBest = null;
            double dBestScore = double.NegativeInfinity;

            for (int i = 0; i < p_objNode.Children.Count; i++)
            {
                ChessMctsNode objChild = p_objNode.Children[i];
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

        private static ChessMctsNode Expand(ChessMctsNode p_objNode, Random p_objRandom)
        {
            int nIndex = p_objRandom.Next(p_objNode.UntriedMoves.Count);
            (int FromX, int FromY, int ToX, int ToY) stMove = p_objNode.UntriedMoves[nIndex];
            p_objNode.UntriedMoves.RemoveAt(nIndex);

            ChessRules.ChessGameSession objChildSession = p_objNode.Session.Clone();
            Ont.E_PlayerColor eMoverColor = objChildSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;

            objChildSession.MovePiece(stMove.FromX, stMove.FromY, stMove.ToX, stMove.ToY);

            ChessMctsNode objChild = CreateNode(p_objNode, stMove, eMoverColor, objChildSession);
            p_objNode.Children.Add(objChild);
            return objChild;
        }

        private Ont.GameContext Simulate(ChessMctsNode p_objNode, Random p_objRandom)
        {
            ChessRules.ChessGameSession objRolloutSession = p_objNode.Session.Clone();
            int nMoveCount = 0;

            while (!objRolloutSession.mv_objCurrentContext.mv_isGameOver && nMoveCount < m_nMaxRolloutMoves)
            {
                Ont.E_PlayerColor eActiveColor = objRolloutSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
                List<(int FromX, int FromY, int ToX, int ToY)> lisLegalMoves = objRolloutSession.GetAllLegalMoves(eActiveColor);

                if (lisLegalMoves.Count == 0)
                {
                    break; // 체크메이트/스테일메이트인데 Effect_CheckChessGameOver가 이미 처리했어야 하지만, 방어적으로 둔다.
                }

                (int FromX, int FromY, int ToX, int ToY) stChoice = lisLegalMoves[p_objRandom.Next(lisLegalMoves.Count)];
                objRolloutSession.MovePiece(stChoice.FromX, stChoice.FromY, stChoice.ToX, stChoice.ToY);

                nMoveCount++;
            }

            return objRolloutSession.mv_objCurrentContext;
        }

        private static Ont.E_PlayerColor? DetermineWinner(Ont.GameContext p_objContext)
        {
            if (p_objContext.mv_isGameOver)
            {
                Ont.PlayerState objWhite = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.White)!;
                Ont.PlayerState objBlack = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;

                if (objWhite.mv_nScore > objBlack.mv_nScore)
                {
                    return Ont.E_PlayerColor.White;
                }

                if (objBlack.mv_nScore > objWhite.mv_nScore)
                {
                    return Ont.E_PlayerColor.Black;
                }

                return null;
            }

            return EstimateMaterialWinner(p_objContext);
        }

        private static Ont.E_PlayerColor? EstimateMaterialWinner(Ont.GameContext p_objContext)
        {
            int nWhiteValue = SumMaterialValue(p_objContext, Ont.E_PlayerColor.White);
            int nBlackValue = SumMaterialValue(p_objContext, Ont.E_PlayerColor.Black);

            if (nWhiteValue > nBlackValue)
            {
                return Ont.E_PlayerColor.White;
            }

            if (nBlackValue > nWhiteValue)
            {
                return Ont.E_PlayerColor.Black;
            }

            return null;
        }

        private static int SumMaterialValue(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            int nTotal = 0;
            foreach (Ont.Entity objPiece in ChessRules.ChessZoneQuery.FindActivePieces(p_objContext, p_eColor))
            {
                nTotal += s_dicPieceValues[objPiece.mv_strType];
            }

            return nTotal;
        }

        private static void Backpropagate(ChessMctsNode p_objLeaf, Ont.E_PlayerColor? p_eWinner)
        {
            ChessMctsNode? objCurrent = p_objLeaf;

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

        private static ChessMctsNode CreateNode(
            ChessMctsNode? p_objParent,
            (int FromX, int FromY, int ToX, int ToY) p_stMove,
            Ont.E_PlayerColor p_eMoverColor,
            ChessRules.ChessGameSession p_objSession)
        {
            return new ChessMctsNode
            {
                Parent = p_objParent,
                Move = p_stMove,
                MoverColor = p_eMoverColor,
                Session = p_objSession,
                UntriedMoves = BuildCandidateMoves(p_objSession)
            };
        }

        private static List<(int FromX, int FromY, int ToX, int ToY)> BuildCandidateMoves(ChessRules.ChessGameSession p_objSession)
        {
            if (p_objSession.mv_objCurrentContext.mv_isGameOver)
            {
                return new List<(int FromX, int FromY, int ToX, int ToY)>();
            }

            Ont.E_PlayerColor eActiveColor = p_objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
            return p_objSession.GetAllLegalMoves(eActiveColor);
        }

        private sealed class ChessMctsNode
        {
            public ChessMctsNode? Parent;
            public List<ChessMctsNode> Children = new();
            public (int FromX, int FromY, int ToX, int ToY) Move;
            public Ont.E_PlayerColor MoverColor;
            public ChessRules.ChessGameSession Session = null!;
            public List<(int FromX, int FromY, int ToX, int ToY)> UntriedMoves = null!;
            public int VisitCount;
            public double TotalReward;

            public bool IsTerminal => Session.mv_objCurrentContext.mv_isGameOver;
        }
    }
}
