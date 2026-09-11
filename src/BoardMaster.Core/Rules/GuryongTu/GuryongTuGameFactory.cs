namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 구룡투(九龍鬪 / Showdown Tactics) 표준 초기 국면을 만드는 팩토리입니다. 두 플레이어 모두
    /// 1~9 타일을 하나씩(각자 자기 소유의 9장) Hand Zone에 받고 대국을 시작합니다.
    /// 두 플레이어가 완전히 대칭인 자기 세트를 갖는다는 점이 War(52장을 나눠 갖는 한 벌)와의 차이입니다.
    /// </summary>
    public static class GuryongTuGameFactory
    {
        public const int TILE_COUNT = 9;

        public static Ont.GameContext CreateStandardGame()
        {
            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(
                1, Ont.E_PlayerColor.None, new int[1, 1], 0, 0);

            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            RegisterZones(objContext);
            RegisterPlayers(objContext);
            DealTiles(objContext);

            return objContext;
        }

        private static void RegisterZones(Ont.GameContext p_objContext)
        {
            p_objContext.mv_dicZones.Add(GuryongTuZoneId.HandBlack, new Ont.Zone(GuryongTuZoneId.HandBlack, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(GuryongTuZoneId.HandWhite, new Ont.Zone(GuryongTuZoneId.HandWhite, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(GuryongTuZoneId.PendingBlack, new Ont.Zone(GuryongTuZoneId.PendingBlack, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(GuryongTuZoneId.PendingWhite, new Ont.Zone(GuryongTuZoneId.PendingWhite, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(GuryongTuZoneId.Discard, new Ont.Zone(GuryongTuZoneId.Discard, 0, 0, Ont.E_VisibilityType.Public));
        }

        private static void RegisterPlayers(Ont.GameContext p_objContext)
        {
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_Black", Ont.E_PlayerColor.Black));
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_White", Ont.E_PlayerColor.White));
        }

        private static void DealTiles(Ont.GameContext p_objContext)
        {
            Ont.Zone objHandBlack = p_objContext.mv_dicZones[GuryongTuZoneId.HandBlack];
            Ont.Zone objHandWhite = p_objContext.mv_dicZones[GuryongTuZoneId.HandWhite];

            for (int nRank = 1; nRank <= TILE_COUNT; nRank++)
            {
                p_objContext.mv_lisEntities.Add(
                    new Ont.Entity($"Black_{nRank}", Ont.E_PlayerColor.Black, nRank.ToString(), objHandBlack));
                p_objContext.mv_lisEntities.Add(
                    new Ont.Entity($"White_{nRank}", Ont.E_PlayerColor.White, nRank.ToString(), objHandWhite));
            }
        }
    }
}
