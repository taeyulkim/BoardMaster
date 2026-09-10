namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// IRuleModifier의 구체적인 예시입니다. 위치 기반 슈퍼코(Cond_NotSuperko)를 끄고 단순패
    /// (Cond_NotKoRecapture)만 적용하는 규칙 변형입니다 — 일부 캐주얼 룰셋/서버가 실제로 이렇게
    /// 슈퍼코를 강제하지 않고 단순패만 봅니다. GoGameSession의 기본 조립 로직(CreatePlaceStoneAction)
    /// 은 전혀 건드리지 않고, 생성자에 이 모디파이어 하나만 넘기면 적용됩니다.
    /// </summary>
    public sealed class GoNoSuperkoModifier : OntDyn.IRuleModifier
    {
        public string mv_strModifierName => "NoSuperko(SimpleKoOnly)";

        public void Apply(DomainAction p_objAction, Ont.GameContext p_objContext)
        {
            if (p_objAction is null)
            {
                throw new ArgumentNullException(nameof(p_objAction));
            }

            p_objAction.mv_lisConditions.RemoveAll(c => c is Cond_NotSuperko);
        }
    }
}
