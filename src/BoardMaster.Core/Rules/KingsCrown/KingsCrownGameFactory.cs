namespace BoardMaster.Core.Rules.KingsCrown
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 표준 킹스 크라운(이세돌, WIZSTONE) 초기 국면을 만드는 팩토리입니다. 5x5 반상에 25개 칸을
    /// 등록합니다(가운데 칸은 영원히 비워두는 와일드카드 — KingsCrownBoardGeometry 참고). 아직 놓지
    /// 않은 숫자칩은 GameContext에 속하지 않는 세션 전용 비밀 상태라, 이 팩토리는 반상만 준비하고
    /// 실제 숫자칩 배분은 KingsCrownGameSession 생성자가 담당합니다(Nine Knights의 비밀 임무/히든
    /// 번호 배분과 같은 위치).
    ///
    /// 원작(규칙서 기준)은 두 단계로 진행됩니다: (1) '가져오기' 단계 — 주머니에서 숫자칩을 2개씩
    /// 공개하고 그중 하나를 가져가기를 반복하며, 가져온 뒤 공개된 칩은 다시 뒤집어 두어 상대와
    /// 자신 모두 "어느 자리에 어떤 숫자가 뒤집혀 있는지" 기억해야 하는 기억력 미니게임입니다.
    /// (2) '놓기' 단계 — 모은 숫자칩과 자기 왕관을 결합해 인접 규칙에 따라 반상에 놓아 빙고를
    /// 만드는, 규칙서가 명시하는 이 게임의 핵심 전략 단계입니다.
    ///
    /// 이 구현은 (1)단계를 생략하고 양쪽 모두 게임 시작 시 무작위로 12개씩 숫자칩을 배분합니다 —
    /// (1)단계는 "실제로 공개됐던 정보를 인간이 얼마나 잘 기억하는가"라는, 규칙 엔진이나 AI
    /// 정직성 모델로 깔끔하게 옮기기 어려운 순수 기억력 미니게임이기 때문입니다(반면 최종적으로
    /// "상대가 어떤 숫자를 쥐고 있는지 모른다"는 핵심 은닉 정보 구조는 그대로 보존됩니다). Nine
    /// Knights의 자동 무작위 배치와 같은 성격의 의도적 단순화입니다. (2)단계의 인접/빙고 규칙은
    /// 전부 그대로 구현했습니다.
    /// </summary>
    public static class KingsCrownGameFactory
    {
        public const int BOARD_SIZE = 5;
        public const int CHIPS_PER_PLAYER = 12;
        public const int MAX_NUMBER = 24;

        public static Ont.GameContext CreateStandardGame()
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(1, Ont.E_PlayerColor.Black, new int[1, 1], 0, 0);
            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            RegisterZones(objContext);
            RegisterPlayers(objContext);

            return objContext;
        }

        private static void RegisterZones(Ont.GameContext p_objContext)
        {
            for (int nY = 0; nY < BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < BOARD_SIZE; nX++)
                {
                    string strZoneId = KingsCrownZoneId.Square(nX, nY);
                    p_objContext.mv_dicZones.Add(strZoneId, new Ont.Zone(strZoneId, nX, nY, Ont.E_VisibilityType.Public));
                }
            }
        }

        private static void RegisterPlayers(Ont.GameContext p_objContext)
        {
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player1", Ont.E_PlayerColor.Black));
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player2", Ont.E_PlayerColor.White));
        }
    }
}
