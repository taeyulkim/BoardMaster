namespace BoardMaster.Core.Rules.War
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// War 표준 초기 국면을 만드는 팩토리입니다: 52장(13랭크 x 4수트)을 섞어 두 플레이어의 덱
    /// (Deck_Black/Deck_White, Hidden)에 26장씩 나눠 넣고, Table/WarPool/Pile Zone까지 등록합니다.
    /// 랭크는 Entity.mv_strType에 "2".."14"(11=J, 12=Q, 13=K, 14=A) 문자열로 담습니다 — War 규칙상
    /// 수트는 승패에 전혀 영향을 주지 않으므로 mv_strEntityID(예: "Spades_14")에만 기록해 식별용으로만
    /// 씁니다.
    ///
    /// Zone에는 순서(스택) 개념이 없으므로(Entity → Zone 단방향 참조뿐인 정적 설계서 원안 그대로),
    /// "실제로 카드를 섞어 순서대로 쌓는" 대신 여기서 한 번 무작위로 26/26을 나눠 배분하고,
    /// 이후 라운드마다 "덱에 남은 카드 중 하나를 난수로 뽑는" 방식으로 무작위성을 이어갑니다
    /// (Effect_DrawTopCards 참고) — 매 라운드 정확히 "맨 위 카드"라는 개념은 없지만 통계적으로는
    /// 동등합니다.
    /// </summary>
    public static class WarGameFactory
    {
        private static readonly string[] s_a_strSuits = { "Spades", "Hearts", "Diamonds", "Clubs" };

        public static Ont.GameContext CreateStandardGame(Random? p_objRandom = null)
        {
            Random objRandom = p_objRandom ?? new Random();

            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(
                1, Ont.E_PlayerColor.None, new int[1, 1], 0, 0);

            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            RegisterZones(objContext);
            RegisterPlayers(objContext);
            DealCards(objContext, objRandom);

            return objContext;
        }

        private static void RegisterZones(Ont.GameContext p_objContext)
        {
            p_objContext.mv_dicZones.Add(WarZoneId.DeckBlack, new Ont.Zone(WarZoneId.DeckBlack, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(WarZoneId.DeckWhite, new Ont.Zone(WarZoneId.DeckWhite, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(WarZoneId.PileBlack, new Ont.Zone(WarZoneId.PileBlack, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(WarZoneId.PileWhite, new Ont.Zone(WarZoneId.PileWhite, 0, 0, Ont.E_VisibilityType.Hidden));
            p_objContext.mv_dicZones.Add(WarZoneId.Table, new Ont.Zone(WarZoneId.Table, 0, 0, Ont.E_VisibilityType.Public));
            p_objContext.mv_dicZones.Add(WarZoneId.WarPool, new Ont.Zone(WarZoneId.WarPool, 0, 0, Ont.E_VisibilityType.Public));
        }

        private static void RegisterPlayers(Ont.GameContext p_objContext)
        {
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_Black", Ont.E_PlayerColor.Black));
            p_objContext.mv_lisPlayers.Add(new Ont.PlayerState("Player_White", Ont.E_PlayerColor.White));
        }

        private static void DealCards(Ont.GameContext p_objContext, Random p_objRandom)
        {
            List<(int Rank, string Suit)> lisCards = new List<(int Rank, string Suit)>();
            for (int nRank = 2; nRank <= 14; nRank++)
            {
                foreach (string strSuit in s_a_strSuits)
                {
                    lisCards.Add((nRank, strSuit));
                }
            }

            for (int i = lisCards.Count - 1; i > 0; i--)
            {
                int j = p_objRandom.Next(i + 1);
                (lisCards[i], lisCards[j]) = (lisCards[j], lisCards[i]);
            }

            Ont.Zone objDeckBlack = p_objContext.mv_dicZones[WarZoneId.DeckBlack];
            Ont.Zone objDeckWhite = p_objContext.mv_dicZones[WarZoneId.DeckWhite];

            for (int i = 0; i < lisCards.Count; i++)
            {
                (int nRank, string strSuit) = lisCards[i];
                bool bGoesToBlack = (i % 2) == 0;
                string strEntityId = $"{strSuit}_{nRank}";

                Ont.Entity objCard = new Ont.Entity(
                    strEntityId,
                    bGoesToBlack ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White,
                    nRank.ToString(),
                    bGoesToBlack ? objDeckBlack : objDeckWhite);

                p_objContext.mv_lisEntities.Add(objCard);
            }
        }
    }
}
