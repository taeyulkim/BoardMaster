namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// GameContext.Clone()을 O(1)로 만들기 위한, 불변(persistent) 단일 연결 리스트 기반 행동 이력입니다.
    /// 기존 List&lt;ST_ActionData&gt;.AddRange 방식은 Clone()마다 지금까지의 전체 이력을 복사했는데,
    /// MCTS 롤아웃처럼 한 번의 탐색에서 수백 번씩 Clone()이 연쇄되는 경로에서는 이 복사 비용이
    /// 게임 진행 길이에 비례해 누적되어(한 롤아웃 안에서 총 O(n^2)) NFR-3이 지적하는 GC 부하의
    /// 주된 원인이 되었습니다.
    ///
    /// 이 구조는 노드가 한 번 만들어지면 절대 변경되지 않는다는 점을 이용합니다: Clone()은 꼬리
    /// 노드 참조 하나만 복사하면 되고(O(1)), 이후 원본과 복제본이 각자 Append하더라도 서로 다른
    /// 새 노드를 만들 뿐 기존 노드를 공유한 채 안전하게 갈라집니다(FR-3.1의 격리 보장은 그대로 유지).
    /// 시간순 열거(기보 저장 등)만 실제로 필요할 때 O(n) 비용을 지불합니다.
    /// </summary>
    public readonly struct GameHistory : IEnumerable<ST_ActionData>
    {
        private readonly HistoryNode? m_objTail;

        internal GameHistory(HistoryNode? p_objTail)
        {
            m_objTail = p_objTail;
        }

        public int Count => m_objTail?.m_nCount ?? 0;

        /// <summary>
        /// 가장 최근에 추가된 행동입니다(꼬리 노드를 그대로 가리키므로 O(1)). 이력이 비어 있으면
        /// 예외를 던집니다 — 기존 List&lt;T&gt;의 인덱서 `[^1]`이 빈 리스트에서 던지던 것과 동일한 계약입니다.
        /// </summary>
        public ST_ActionData Last => m_objTail is null
            ? throw new InvalidOperationException("행동 이력이 비어 있습니다.")
            : m_objTail.m_stAction;

        internal GameHistory Append(ST_ActionData p_stAction)
        {
            return new GameHistory(new HistoryNode(p_stAction, m_objTail));
        }

        public IEnumerator<ST_ActionData> GetEnumerator()
        {
            List<ST_ActionData> lisChronological = new List<ST_ActionData>(Count);
            for (HistoryNode? objNode = m_objTail; objNode != null; objNode = objNode.m_objPrevious)
            {
                lisChronological.Add(objNode.m_stAction);
            }

            lisChronological.Reverse();
            return lisChronological.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class HistoryNode
    {
        internal readonly ST_ActionData m_stAction;
        internal readonly HistoryNode? m_objPrevious;
        internal readonly int m_nCount;

        internal HistoryNode(ST_ActionData p_stAction, HistoryNode? p_objPrevious)
        {
            m_stAction = p_stAction;
            m_objPrevious = p_objPrevious;
            m_nCount = (p_objPrevious?.m_nCount ?? 0) + 1;
        }
    }
}
