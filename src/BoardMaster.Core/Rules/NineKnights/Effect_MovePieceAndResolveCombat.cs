namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 기사를 도착 칸으로 옮깁니다. 도착 칸에 상대 기사가 있었다면 전투를 벌입니다
    /// (NineKnightsCombatRules.AttackerWins). 진 쪽은 반상에서 제거되어 Captured Zone으로 치워지고,
    /// 그 즉시(턴 소비 없이) 진 쪽의 예비 기사 중 가장 낮은 번호 하나가 자기 배치 줄의 빈 칸에
    /// 자동으로 소환됩니다(원작은 어느 예비를 어디에 둘지 플레이어가 직접 고르지만, 이 구현은
    /// 그 선택을 생략한 단순화입니다). 예비가 없거나 배치 줄에 빈 칸이 없으면 소환은 건너뜁니다.
    ///
    /// 공격자의 히든 토큰 번호는 GameContext에 없는(각 플레이어만 아는 비밀) 값이라
    /// NineKnightsGameSession이 생성자로 주입합니다.
    /// </summary>
    public sealed class Effect_MovePieceAndResolveCombat : Ont.IEffect
    {
        private readonly int m_nAttackerHiddenNumber;

        public Effect_MovePieceAndResolveCombat(int p_nAttackerHiddenNumber)
        {
            m_nAttackerHiddenNumber = p_nAttackerHiddenNumber;
        }

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            (int nFromX, int nFromY) = NineKnightsActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            (int nToX, int nToY) = NineKnightsActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nY);

            Ont.Entity objMover = NineKnightsZoneQuery.FindPieceAt(p_objContext, nFromX, nFromY)!;
            Ont.Entity? objDefender = NineKnightsZoneQuery.FindPieceAt(p_objContext, nToX, nToY);
            Ont.Zone objDestinationZone = p_objContext.mv_dicZones[NineKnightsZoneId.Square(nToX, nToY)];
            Ont.Zone objCapturedZone = p_objContext.mv_dicZones[NineKnightsZoneId.Captured];

            if (objDefender is null)
            {
                objMover.mv_objLocatedZone = objDestinationZone;
                return p_objContext;
            }

            int nAttackerNumber = int.Parse(objMover.mv_strType);
            int nDefenderNumber = int.Parse(objDefender.mv_strType);
            bool bAttackerWins = NineKnightsCombatRules.AttackerWins(nAttackerNumber, nDefenderNumber, m_nAttackerHiddenNumber);

            Ont.E_PlayerColor eLoserColor;
            if (bAttackerWins)
            {
                objDefender.mv_objLocatedZone = objCapturedZone;
                objMover.mv_objLocatedZone = objDestinationZone;
                eLoserColor = objDefender.mv_eColor;
            }
            else
            {
                objMover.mv_objLocatedZone = objCapturedZone;
                eLoserColor = objMover.mv_eColor;
            }

            TrySummonReserve(p_objContext, eLoserColor);

            return p_objContext;
        }

        private static void TrySummonReserve(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            List<Ont.Entity> lisReserves = NineKnightsZoneQuery.FindReservePieces(p_objContext, p_eColor);
            if (lisReserves.Count == 0)
            {
                return;
            }

            int nDeploymentRowY = NineKnightsBoardGeometry.DeploymentRow(p_eColor);
            for (int nX = 0; nX < NineKnightsGameFactory.BOARD_SIZE; nX++)
            {
                if (NineKnightsZoneQuery.FindPieceAt(p_objContext, nX, nDeploymentRowY) is not null)
                {
                    continue;
                }

                Ont.Entity objLowestReserve = lisReserves[0];
                foreach (Ont.Entity objReserve in lisReserves)
                {
                    if (int.Parse(objReserve.mv_strType) < int.Parse(objLowestReserve.mv_strType))
                    {
                        objLowestReserve = objReserve;
                    }
                }

                objLowestReserve.mv_objLocatedZone = p_objContext.mv_dicZones[NineKnightsZoneId.Square(nX, nDeploymentRowY)];
                return;
            }
        }
    }
}
