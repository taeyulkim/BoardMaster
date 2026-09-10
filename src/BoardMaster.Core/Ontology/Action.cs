namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// 플레이어가 선언한 하나의 행동입니다. 검증(ICondition)과 상태 전이(IEffect) 목록을 조립하여 보유하며,
    /// 스스로를 검증하고(Validate) 원자적으로 실행(Execute)할 수 있습니다.
    /// </summary>
    public sealed class Action
    {
        public string mv_strActionType { get; set; }
        public ST_ActionData mv_stActionData { get; set; }
        public List<ICondition> mv_lisConditions { get; }
        public List<IEffect> mv_lisEffects { get; }

        public Action(string p_strActionType, ST_ActionData p_stActionData)
        {
            mv_strActionType = p_strActionType ?? throw new ArgumentNullException(nameof(p_strActionType));
            mv_stActionData = p_stActionData;
            mv_lisConditions = new List<ICondition>();
            mv_lisEffects = new List<IEffect>();
        }

        /// <summary>
        /// 등록된 모든 ICondition이 만족되는지 검사합니다. 하나라도 불만족하면 즉시 false를 반환합니다.
        /// </summary>
        public bool Validate(GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            foreach (ICondition objCondition in mv_lisConditions)
            {
                if (!objCondition.IsSatisfied(p_objContext, this))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 검증 통과 시 원본 p_objContext는 절대 변경하지 않고, 복제본 위에서만 IEffect 목록을 순차 적용하여
        /// 새로운 GameContext를 반환합니다. 검증 실패 시 InvalidOperationException을 던지며, 이 경우에도
        /// 아무런 상태 전이가 일어나지 않았으므로 p_objContext는 완전히 이전 상태 그대로 유지됩니다.
        /// </summary>
        public GameContext Execute(GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (!Validate(p_objContext))
            {
                throw new InvalidOperationException($"동작 실행이 거부되었습니다. 규칙 제약 조건 위반: {mv_strActionType}");
            }

            GameContext objNewContext = p_objContext.Clone();

            foreach (IEffect objEffect in mv_lisEffects)
            {
                objNewContext = objEffect.Apply(objNewContext, this);
            }

            objNewContext.AppendHistory(mv_stActionData);

            return objNewContext;
        }
    }
}
