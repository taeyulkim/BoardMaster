namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// 게임 전체 메타데이터와 진행 시점을 소유 및 추적하는 최상위 컨텍스트입니다.
    /// </summary>
    public sealed class GameContext
    {
        public ST_BoardState mv_stCurrentState { get; set; }
        public List<PlayerState> mv_lisPlayers { get; }
        public Dictionary<string, Zone> mv_dicZones { get; }
        public List<Entity> mv_lisEntities { get; }
        public GameHistory mv_lisHistory { get; private set; }
        public bool mv_isGameOver { get; set; }

        public GameContext(ST_BoardState p_stInitialState)
        {
            mv_stCurrentState = p_stInitialState;
            mv_lisPlayers = new List<PlayerState>();
            mv_dicZones = new Dictionary<string, Zone>();
            mv_lisEntities = new List<Entity>();
            mv_lisHistory = default; // 빈 이력(꼬리 노드 없음)
            mv_isGameOver = false;
        }

        /// <summary>
        /// 현재 행동을 이력에 추가합니다. Action.Execute가 Effect 체인을 다 적용한 직후 호출하는
        /// 용도이며, O(1)입니다(GameHistory 참고) — 기존 List.Add와 달리 매 Clone()마다 전체 이력을
        /// 복사할 필요가 없습니다.
        /// </summary>
        internal void AppendHistory(ST_ActionData p_stAction)
        {
            mv_lisHistory = mv_lisHistory.Append(p_stAction);
        }

        /// <summary>
        /// 현재 컨텍스트를 완전히 격리된 값으로 깊은 복사합니다.
        /// Action.Execute 및 ActionDispatcher가 원자적 상태 전이(가상 롤아웃 포함)를 수행할 때
        /// 원본을 절대 건드리지 않고 이 복제본 위에서만 IEffect를 적용하기 위한 단일 진입점입니다.
        /// </summary>
        public GameContext Clone()
        {
            ST_BoardState stClonedState = new ST_BoardState(
                mv_stCurrentState.m_nTurnNumber,
                mv_stCurrentState.m_eActiveColor,
                (int[,])mv_stCurrentState.m_a_nBoardGrid.Clone(),
                mv_stCurrentState.m_nBlackPrisoners,
                mv_stCurrentState.m_nWhitePrisoners);

            GameContext objClone = new GameContext(stClonedState)
            {
                mv_isGameOver = mv_isGameOver
            };

            foreach (PlayerState objPlayer in mv_lisPlayers)
            {
                PlayerState objClonedPlayer = new PlayerState(objPlayer.mv_strPlayerID, objPlayer.mv_eColor)
                {
                    mv_nPrisonerCount = objPlayer.mv_nPrisonerCount,
                    mv_nScore = objPlayer.mv_nScore
                };
                objClone.mv_lisPlayers.Add(objClonedPlayer);
            }

            foreach (KeyValuePair<string, Zone> kvpZone in mv_dicZones)
            {
                Zone objZone = kvpZone.Value;
                Zone objClonedZone = new Zone(objZone.mv_strZoneID, objZone.mv_nX, objZone.mv_nY, objZone.mv_eVisibility)
                {
                    mv_lisAdjacentZoneIDs = new List<string>(objZone.mv_lisAdjacentZoneIDs)
                };
                objClone.mv_dicZones.Add(kvpZone.Key, objClonedZone);
            }

            // Zone을 먼저 복제해 둔 뒤에야 Entity가 "복제된" Zone 인스턴스를 가리키도록 연결할 수 있다.
            // 원본 Zone 참조를 그대로 물려주면 복제본의 Entity가 원본 GameContext의 Zone 객체를
            // 계속 가리키게 되어 복제 격리가 깨진다.
            foreach (Entity objEntity in mv_lisEntities)
            {
                Zone objClonedZone = objClone.mv_dicZones[objEntity.mv_objLocatedZone.mv_strZoneID];
                Entity objClonedEntity = new Entity(objEntity.mv_strEntityID, objEntity.mv_eColor, objEntity.mv_strType, objClonedZone);
                objClone.mv_lisEntities.Add(objClonedEntity);
            }

            // 꼬리 노드 참조만 복사한다(O(1)) — 리스트 전체를 복사하지 않는다. 이후 원본과
            // 복제본이 각자 AppendHistory를 호출해도 서로 다른 새 노드가 생길 뿐, 기존 노드는
            // 절대 변경되지 않으므로 격리는 그대로 유지된다.
            objClone.mv_lisHistory = mv_lisHistory;

            return objClone;
        }
    }
}
