namespace BoardMaster.Core.Rules.NineKnights
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 표준 나인 나이츠(이세돌, WIZSTONE) 초기 국면을 만드는 팩토리입니다. 9x9 반상에서 각 플레이어는
    /// 1~9 숫자가 하나씩 적힌 기사 9명을 갖습니다 — 6명은 자기 쪽 배치 줄(y=2 또는 y=6, "3행")의
    /// 무작위 칸에 곧바로 배치되고, 나머지 3명은 예비로 대기합니다.
    ///
    /// 물리 원작은 "선공 1개 - 후공 2개 - 선공 2개 - ..." 순서로 두 사람이 번갈아 직접 배치하는
    /// 스네이크 드래프트라 "어느 숫자를 어디에 둘지"가 그 자체로 전략/블러핑입니다. 이 구현은
    /// 그 상호작용식 배치 단계를 생략하고 양쪽 다 무작위로 배치합니다 — War가 실제 카드 섞기는
    /// 구현하되 전쟁의 서브 배틀은 생략한 것과 같은 성격의 의도적 단순화입니다.
    ///
    /// 비밀 임무 번호(자신의 몇 번 기사가 상대 진영 끝(y=0 또는 y=8)에 닿아야 이기는지)와 히든 토큰
    /// 번호(상대의 8을 잡을 수 있는 나만 아는 1~5 사이 숫자)도 여기서 무작위로 정합니다. 원작은
    /// 미션이 워리어/아처/레인저 세 병과별로 1~3/4~6/7~9 중 하나씩 배정되는 카드 뽑기지만, 병과가
    /// 이동/전투에 실질적 차이를 주지 않으므로 이 구현은 그 세부 절차 없이 1~9 중 균등 무작위로
    /// 하나만 정합니다.
    /// </summary>
    public static class NineKnightsGameFactory
    {
        public const int BOARD_SIZE = 9;
        public const int DEPLOYED_COUNT = 6;
        public const int RESERVE_COUNT = 3;
        public const int PIECE_COUNT_PER_PLAYER = DEPLOYED_COUNT + RESERVE_COUNT;

        public static Ont.GameContext CreateStandardGame(Random? p_objRandom = null)
        {
            Random objRandom = p_objRandom ?? new Random();

            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(1, Ont.E_PlayerColor.Black, new int[1, 1], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            RegisterZones(objContext);
            RegisterPlayers(objContext);
            DealPieces(objContext, Ont.E_PlayerColor.Black, NineKnightsBoardGeometry.DeploymentRow(Ont.E_PlayerColor.Black), objRandom);
            DealPieces(objContext, Ont.E_PlayerColor.White, NineKnightsBoardGeometry.DeploymentRow(Ont.E_PlayerColor.White), objRandom);

            return objContext;
        }

        private static void RegisterZones(Ont.GameContext p_objContext)
        {
            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    string strZoneId = NineKnightsZoneId.Square(nX, nY);
                    p_objContext.mv_dicZones.Add(strZoneId, new Ont.Zone(strZoneId, nX, nY, Ont.E_VisibilityType.Public));
                }
            }

            p_objContext.mv_dicZones.Add(NineKnightsZoneId.ReservePlayer1, new Ont.Zone(NineKnightsZoneId.ReservePlayer1, -1, -1, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(NineKnightsZoneId.ReservePlayer2, new Ont.Zone(NineKnightsZoneId.ReservePlayer2, -1, -1, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(NineKnightsZoneId.Captured, new Ont.Zone(NineKnightsZoneId.Captured, -1, -1, Ont.E_VisibilityType.Public));
        }

        private static void RegisterPlayers(Ont.GameContext p_objContext)
        {
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player1", Ont.E_PlayerColor.Black));
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player2", Ont.E_PlayerColor.White));
        }

        private static void DealPieces(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nDeploymentRowY, Random p_objRandom)
        {
            List<int> lisNumbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Shuffle(lisNumbers, p_objRandom);

            List<int> lisDeploymentColumns = new List<int>();
            for (int nX = 0; nX < BOARD_SIZE; nX++)
            {
                lisDeploymentColumns.Add(nX);
            }

            Shuffle(lisDeploymentColumns, p_objRandom);

            string strReserveZoneId = p_eColor == Ont.E_PlayerColor.Black ? NineKnightsZoneId.ReservePlayer1 : NineKnightsZoneId.ReservePlayer2;
            Ont.Zone objReserveZone = p_objContext.mv_dicZones[strReserveZoneId];

            for (int i = 0; i < PIECE_COUNT_PER_PLAYER; i++)
            {
                int nNumber = lisNumbers[i];
                string strEntityId = $"{p_eColor}_Knight_{nNumber}";

                Ont.Zone objStartZone = i < DEPLOYED_COUNT
                    ? p_objContext.mv_dicZones[NineKnightsZoneId.Square(lisDeploymentColumns[i], p_nDeploymentRowY)]
                    : objReserveZone;

                p_objContext.mv_lisEntities.Add(new Ont.Entity(strEntityId, p_eColor, nNumber.ToString(), objStartZone));
            }
        }

        private static void Shuffle<T>(List<T> p_lisItems, Random p_objRandom)
        {
            for (int i = p_lisItems.Count - 1; i > 0; i--)
            {
                int j = p_objRandom.Next(i + 1);
                (p_lisItems[i], p_lisItems[j]) = (p_lisItems[j], p_lisItems[i]);
            }
        }
    }
}
