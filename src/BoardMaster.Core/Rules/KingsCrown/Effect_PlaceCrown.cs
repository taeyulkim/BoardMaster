namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>action data가 담은 칸에 action 색의 새 왕관 Entity를 만들어 놓습니다. 나인 나이츠와
    /// 달리 기존 기물을 옮기는 게 아니라, 이번 턴에 쓴 숫자칩+왕관을 결합해 새로 생겨나는 것이므로
    /// Entity를 새로 생성해 GameContext에 추가합니다.</summary>
    public sealed class Effect_PlaceCrown : Ont.IEffect
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

            (int nX, int nY) = KingsCrownActionCoding.DecodeSquare(p_objAction.mv_stActionData.m_nX);
            int nNumber = p_objAction.mv_stActionData.m_nY;
            Ont.E_PlayerColor eColor = p_objAction.mv_stActionData.m_eColor;

            Ont.Zone objZone = p_objContext.mv_dicZones[KingsCrownZoneId.Square(nX, nY)];
            string strEntityId = $"{eColor}_Crown_{nX}_{nY}";
            p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, eColor, nNumber.ToString(), objZone));

            return p_objContext;
        }
    }
}
