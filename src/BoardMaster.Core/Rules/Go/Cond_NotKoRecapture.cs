namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 단순패(simple Ko) 규칙 검증기입니다. 직전 한 수가 정확히 돌 하나만 따낸 경우, 그 따낸 자리를
    /// 곧바로 되따내는 착수를 다음 한 수 동안만 금지합니다.
    ///
    /// 이 조건 자체는 "지금 금지된 좌표가 어디인지"를 스스로 계산하지 않습니다. 그 계산(이번 수 전후
    /// 반상을 비교해 정확히 한 칸만 비었는지 확인)은 GoGameSession이 매 착수 직후 갱신해서 이 조건에
    /// 생성자로 주입합니다(GoGameSession 문서 참고). GameContext에 바둑 전용 "금지 좌표" 필드를 얹지
    /// 않으면서도, 매 수마다 값을 새로 계산해 넘기기 때문에 제한이 정확히 한 수 동안만 유지됩니다.
    ///
    /// 왜 "외톨이 돌 + 활로 1개" 같은 모양 휴리스틱이 아니라 직접 비교인가: 단순히 "직전 수의 돌이
    /// 활로 1개짜리 외톨이"라는 것만으로는 그 수가 실제로 무언가를 땄는지 알 수 없습니다(애초에 좁은
    /// 자리에 뒀을 뿐 아무것도 안 딴 수도 똑같은 모양이 됩니다). 착수 전후 반상을 직접 비교해 "정확히
    /// 한 칸이 상대 돌에서 빈 칸으로 바뀌었는가"만 보는 쪽이 오탐 없이 정확합니다.
    /// </summary>
    public sealed class Cond_NotKoRecapture : Ont.ICondition
    {
        private readonly (int X, int Y)? m_stForbiddenPoint;

        public Cond_NotKoRecapture((int X, int Y)? p_stForbiddenPoint)
        {
            m_stForbiddenPoint = p_stForbiddenPoint;
        }

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

            if (m_stForbiddenPoint is null)
            {
                return true;
            }

            if (p_objAction.mv_stActionData.m_isPass)
            {
                return true;
            }

            (int X, int Y) stForbidden = m_stForbiddenPoint.Value;
            return p_objAction.mv_stActionData.m_nX != stForbidden.X || p_objAction.mv_stActionData.m_nY != stForbidden.Y;
        }
    }
}
