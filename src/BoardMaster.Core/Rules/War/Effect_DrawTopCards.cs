namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 양쪽 플레이어의 덱에서 카드 한 장씩을 Table Zone으로 옮깁니다. 덱이 비어 있으면 먼저 그
    /// 플레이어의 Pile을 덱으로 되돌려(재활용) 채운 뒤 뽑습니다. 그래도 카드가 없으면(더 낼 카드가
    /// 완전히 떨어짐) 이번 라운드엔 그냥 넘어갑니다 — 곧이어 Effect_CheckWarGameOver가 종국 처리합니다.
    ///
    /// Zone에는 순서 개념이 없어서 "맨 위 카드"를 결정할 방법이 없으므로, 덱에 남아있는 카드 중
    /// 하나를 난수로 골라 뽑는 방식으로 대신합니다(WarGameFactory 문서 참고).
    /// </summary>
    public sealed class Effect_DrawTopCards : Ont.IEffect
    {
        private readonly Random m_objRandom;

        public Effect_DrawTopCards(Random p_objRandom)
        {
            m_objRandom = p_objRandom ?? throw new ArgumentNullException(nameof(p_objRandom));
        }

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            DrawOneCard(p_objContext, WarZoneId.DeckBlack, WarZoneId.PileBlack);
            DrawOneCard(p_objContext, WarZoneId.DeckWhite, WarZoneId.PileWhite);

            return p_objContext;
        }

        private void DrawOneCard(Ont.GameContext p_objContext, string p_strDeckZoneId, string p_strPileZoneId)
        {
            List<Ont.Entity> lisDeckCards = WarZoneQuery.FindEntitiesInZone(p_objContext, p_strDeckZoneId);

            if (lisDeckCards.Count == 0)
            {
                RecyclePileIntoDeck(p_objContext, p_strPileZoneId, p_strDeckZoneId);
                lisDeckCards = WarZoneQuery.FindEntitiesInZone(p_objContext, p_strDeckZoneId);
            }

            if (lisDeckCards.Count == 0)
            {
                return; // 이 플레이어는 낼 카드가 완전히 떨어졌다.
            }

            int nIndex = m_objRandom.Next(lisDeckCards.Count);
            Ont.Entity objDrawnCard = lisDeckCards[nIndex];
            objDrawnCard.mv_objLocatedZone = p_objContext.mv_dicZones[WarZoneId.Table];
        }

        private static void RecyclePileIntoDeck(Ont.GameContext p_objContext, string p_strPileZoneId, string p_strDeckZoneId)
        {
            Ont.Zone objDeckZone = p_objContext.mv_dicZones[p_strDeckZoneId];

            foreach (Ont.Entity objCard in WarZoneQuery.FindEntitiesInZone(p_objContext, p_strPileZoneId))
            {
                objCard.mv_objLocatedZone = objDeckZone;
            }
        }
    }
}
