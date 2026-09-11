namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 커밋하려는 타일 랭크가 실제로 그 플레이어의 Hand Zone에 남아 있고(이미 낸 적 없고), 아직
    /// 이번 라운드에 커밋한 적이 없는지(Pending Zone이 비어 있는지) 검증합니다. 후자가 없으면
    /// 한 플레이어가 한 라운드에 타일을 두 번 내서 상대의 커밋을 보고 다시 낼 수 있게 되어
    /// "블라인드"라는 게임의 핵심 전제가 깨집니다.
    /// </summary>
    public sealed class Cond_TileInHand : Ont.ICondition
    {
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

            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;
            int nRank = p_objAction.mv_stActionData.m_nX;

            string strHandZoneId = eColor == Ont.E_PlayerColor.Black ? GuryongTuZoneId.HandBlack : GuryongTuZoneId.HandWhite;
            string strPendingZoneId = eColor == Ont.E_PlayerColor.Black ? GuryongTuZoneId.PendingBlack : GuryongTuZoneId.PendingWhite;

            if (GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, strPendingZoneId).Count > 0)
            {
                return false; // 이미 이번 라운드에 커밋했다.
            }

            foreach (Ont.Entity objTile in GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, strHandZoneId))
            {
                if (objTile.mv_strType == nRank.ToString())
                {
                    return true;
                }
            }

            return false; // 이미 낸 타일이거나 애초에 존재하지 않는 랭크.
        }
    }
}
