namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 바둑의 자충수(Suicide) 금지 규칙 검증기입니다.
    /// 착수 지점에 돌을 두었을 때 (1) 자신의 그룹이 활로를 1개 이상 확보하거나,
    /// (2) 확보하지 못하더라도 인접한 상대 그룹 중 하나 이상을 활로 0으로 만들어 따낼 수 있다면 합법수로 판정합니다.
    ///
    /// BFS 활로 계산 자체는 GoGroupScanner에 위임합니다 — Effect_CaptureStones도 같은 스캐너를 써서
    /// 그룹 탐색 로직이 두 곳에서 따로 구현되며 갈라지는 것을 막았습니다.
    ///
    /// 스레드 안전성: 스캐너가 재사용 버퍼를 갖고 있어 이 인스턴스는 스레드 세이프하지 않습니다.
    /// 규칙 조립 시점에 한 번만 생성해 두고 계속 재사용하세요(착수 후보마다 새로 만들면 재사용 이점이 사라집니다).
    /// 병렬 MCTS 워커라면 워커별로 별도 인스턴스를 두세요.
    /// </summary>
    public sealed class Cond_NotSuicide : Ont.ICondition
    {
        private readonly GoGroupScanner m_objScanner = new GoGroupScanner();

        public bool IsSatisfied(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            int nX = p_objAction.mv_stActionData.m_nX;
            int nY = p_objAction.mv_stActionData.m_nY;
            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            // 1. 내가 돌을 둠으로써 아군 그룹의 활로가 1개 이상 확보되는가?
            int nMyLiberties = CalculateLiberties(a_nGrid, nWidth, nHeight, nX, nY, eColor, nX, nY, eColor);
            if (nMyLiberties > 0)
            {
                return true;
            }

            // 2. 내 활로가 0개라면, 이 착수로 인접한 상대편 돌 그룹을 활로 0으로 만들어 따낼 수 있는가?
            Ont.E_PlayerColor eOpponentColor =
                (eColor == Ont.E_PlayerColor.Black) ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;

            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX - 1, nY, eOpponentColor, nX, nY, eColor)) return true;
            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX + 1, nY, eOpponentColor, nX, nY, eColor)) return true;
            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX, nY - 1, eOpponentColor, nX, nY, eColor)) return true;
            if (CapturesOpponentGroup(a_nGrid, nWidth, nHeight, nX, nY + 1, eOpponentColor, nX, nY, eColor)) return true;

            return false; // 활로도 없고 상대방을 따낼 수도 없으므로 자충수(Suicide) 규칙 위반
        }

        private bool CapturesOpponentGroup(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nNeighborX,
            int p_nNeighborY,
            Ont.E_PlayerColor p_eOpponentColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor)
        {
            if (p_nNeighborX < 0 || p_nNeighborX >= p_nWidth || p_nNeighborY < 0 || p_nNeighborY >= p_nHeight)
            {
                return false;
            }

            Ont.E_PlayerColor eNeighborColor = (p_nNeighborX == p_nOverrideX && p_nNeighborY == p_nOverrideY)
                ? p_eOverrideColor
                : (Ont.E_PlayerColor)p_a_nGrid[p_nNeighborX, p_nNeighborY];

            if (eNeighborColor != p_eOpponentColor)
            {
                return false;
            }

            int nOpponentLiberties = CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nNeighborX, p_nNeighborY, p_eOpponentColor,
                p_nOverrideX, p_nOverrideY, p_eOverrideColor);

            return nOpponentLiberties == 0;
        }

        /// <summary>
        /// (p_nStartX, p_nStartY)를 포함한 p_eGroupColor 동색 그룹 전체의 활로 개수를 BFS(Flood Fill)로 계산합니다.
        /// (p_nOverrideX, p_nOverrideY)는 아직 실제 격자에 쓰이지 않은 가상 착수 좌표이며, 이 좌표를 읽을 때만
        /// p_eOverrideColor로 간주합니다. 가상 치환이 필요 없다면 격자 범위를 벗어난 좌표(-1, -1)를 넘기면 됩니다.
        /// internal로 노출해 테스트 프로젝트가 활로 중복 제거 로직을 직접 검증할 수 있게 했습니다.
        /// GoGroupScanner에 그대로 위임하는 얇은 래퍼입니다.
        /// </summary>
        internal int CalculateLiberties(
            int[,] p_a_nGrid,
            int p_nWidth,
            int p_nHeight,
            int p_nStartX,
            int p_nStartY,
            Ont.E_PlayerColor p_eGroupColor,
            int p_nOverrideX,
            int p_nOverrideY,
            Ont.E_PlayerColor p_eOverrideColor)
        {
            return m_objScanner.CalculateLiberties(
                p_a_nGrid, p_nWidth, p_nHeight, p_nStartX, p_nStartY, p_eGroupColor,
                p_nOverrideX, p_nOverrideY, p_eOverrideColor);
        }
    }
}
