namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 지정된 랭크의 타일을 그 플레이어의 Hand Zone에서 Pending Zone으로 옮깁니다. 이 시점에는
    /// 상대가 이미 커밋했는지 여부를 전혀 신경 쓰지 않습니다 — 라운드 정산은
    /// Effect_ResolveRoundIfBothCommitted가 별도로, 두 Pending이 모두 찼을 때만 담당합니다.
    /// </summary>
    public sealed class Effect_CommitTileToPending : Ont.IEffect
    {
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

            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;
            int nRank = p_objAction.mv_stActionData.m_nX;

            string strHandZoneId = eColor == Ont.E_PlayerColor.Black ? GuryongTuZoneId.HandBlack : GuryongTuZoneId.HandWhite;
            string strPendingZoneId = eColor == Ont.E_PlayerColor.Black ? GuryongTuZoneId.PendingBlack : GuryongTuZoneId.PendingWhite;
            Ont.Zone objPendingZone = p_objContext.mv_dicZones[strPendingZoneId];

            foreach (Ont.Entity objTile in GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, strHandZoneId))
            {
                if (objTile.mv_strType == nRank.ToString())
                {
                    objTile.mv_objLocatedZone = objPendingZone;
                    break;
                }
            }

            return p_objContext;
        }
    }
}
