namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 양쪽 Hand Zone이 모두 비면(=9라운드 전부 커밋되고 정산됨) 대국을 종료 처리합니다.
    /// 이 Effect가 Effect_ResolveComparisonAndScore 바로 다음에 놓이므로(정산 이후에만 호출됨),
    /// Hand가 비었다는 건 이번 정산까지 포함해 9번의 라운드가 전부 끝났다는 뜻입니다.
    /// </summary>
    public sealed class Effect_CheckGuryongTuGameOver : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            bool bBlackHandEmpty = GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, GuryongTuZoneId.HandBlack).Count == 0;
            bool bWhiteHandEmpty = GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, GuryongTuZoneId.HandWhite).Count == 0;

            if (bBlackHandEmpty && bWhiteHandEmpty)
            {
                p_objContext.mv_isGameOver = true;
            }

            return p_objContext;
        }
    }
}
