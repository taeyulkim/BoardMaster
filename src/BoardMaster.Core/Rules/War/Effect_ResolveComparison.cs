namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// Table Zone에 놓인 양쪽 카드의 랭크를 비교합니다. 랭크가 높은 쪽이 Table과 WarPool에 쌓여있던
    /// 카드를 전부 가져가(mv_eColor를 승자 색으로 바꾸고 자신의 Pile로 옮김) 승부를 정산합니다.
    /// 비기면 Table의 두 카드를 WarPool로 옮겨 쌓아두고, 다음 라운드 승자가 통째로 가져가게 합니다
    /// (실제 War처럼 3장 엎고 1장 대결하는 서브 배틀은 생략한 단순화 버전입니다 — 이 구현의 목적은
    /// 엔진이 카드 게임도 감당하는지 증명하는 것이라, 엔진 검증과 무관한 규칙 디테일까지 따라가진
    /// 않았습니다).
    /// 어느 한쪽이 이번 라운드에 낼 카드가 없었다면(Table에 한쪽 카드만 있음) 비교를 생략합니다 —
    /// 곧이어 Effect_CheckWarGameOver가 종국 처리합니다.
    /// </summary>
    public sealed class Effect_ResolveComparison : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            List<Ont.Entity> lisTableCards = WarZoneQuery.FindEntitiesInZone(p_objContext, WarZoneId.Table);

            Ont.Entity? objBlackCard = FindCardOwnedBy(lisTableCards, Ont.E_PlayerColor.Black);
            Ont.Entity? objWhiteCard = FindCardOwnedBy(lisTableCards, Ont.E_PlayerColor.White);

            if (objBlackCard is null || objWhiteCard is null)
            {
                return p_objContext;
            }

            int nBlackRank = int.Parse(objBlackCard.mv_strType);
            int nWhiteRank = int.Parse(objWhiteCard.mv_strType);

            if (nBlackRank == nWhiteRank)
            {
                Ont.Zone objWarPoolZone = p_objContext.mv_dicZones[WarZoneId.WarPool];
                foreach (Ont.Entity objCard in lisTableCards)
                {
                    objCard.mv_objLocatedZone = objWarPoolZone;
                }

                return p_objContext;
            }

            Ont.E_PlayerColor eWinnerColor = nBlackRank > nWhiteRank ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
            string strWinnerPileZoneId = eWinnerColor == Ont.E_PlayerColor.Black ? WarZoneId.PileBlack : WarZoneId.PileWhite;
            Ont.Zone objWinnerPile = p_objContext.mv_dicZones[strWinnerPileZoneId];

            List<Ont.Entity> lisSpoils = new List<Ont.Entity>(lisTableCards);
            lisSpoils.AddRange(WarZoneQuery.FindEntitiesInZone(p_objContext, WarZoneId.WarPool));

            foreach (Ont.Entity objCard in lisSpoils)
            {
                objCard.mv_eColor = eWinnerColor;
                objCard.mv_objLocatedZone = objWinnerPile;
            }

            return p_objContext;
        }

        private static Ont.Entity? FindCardOwnedBy(List<Ont.Entity> p_lisCards, Ont.E_PlayerColor p_eColor)
        {
            foreach (Ont.Entity objCard in p_lisCards)
            {
                if (objCard.mv_eColor == p_eColor)
                {
                    return objCard;
                }
            }

            return null;
        }
    }
}
