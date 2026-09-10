namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 어느 한쪽이든 다음 라운드에 낼 수 있는 카드(덱 + 사물 Pile)가 0장이 되면 대국을 종료 처리합니다.
    /// WarPool에 걸려 있는 카드는 아직 누구 것도 아니므로(다음 승부에서 결정됨) 셈에 넣지 않습니다 —
    /// 그 카드들을 뽑아서 낼 수는 없으니, WarPool 몫이 남아 있어도 덱+Pile이 0이면 그 플레이어는
    /// 더 이상 게임을 진행할 수 없습니다.
    /// </summary>
    public sealed class Effect_CheckWarGameOver : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            int nBlackDrawable = WarZoneQuery.FindEntitiesInZone(p_objContext, WarZoneId.DeckBlack).Count
                + WarZoneQuery.FindEntitiesInZone(p_objContext, WarZoneId.PileBlack).Count;
            int nWhiteDrawable = WarZoneQuery.FindEntitiesInZone(p_objContext, WarZoneId.DeckWhite).Count
                + WarZoneQuery.FindEntitiesInZone(p_objContext, WarZoneId.PileWhite).Count;

            if (nBlackDrawable == 0 || nWhiteDrawable == 0)
            {
                p_objContext.mv_isGameOver = true;
            }

            return p_objContext;
        }
    }
}
