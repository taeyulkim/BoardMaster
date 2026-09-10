namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using War = BoardMaster.Core.Rules.War;

    internal static class WarGameSessionTests
    {
        public static void CurrentPhaseName_StartsAsMainPlay()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(5));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(6));

            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "시작 시점엔 MainPlay 페이즈여야 한다");
        }

        public static void PlayRound_KeepsTotalCardCountAt52()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(1));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(2));

            objSession.PlayRound();

            int nTotal = objSession.CountCardsOwnedBy(Ont.E_PlayerColor.Black) + objSession.CountCardsOwnedBy(Ont.E_PlayerColor.White);
            Assert.AreEqual(52, nTotal, "카드 총량은 라운드가 진행돼도 52장으로 유지되어야 한다");
        }

        public static void PlayUntilGameOver_RunsToCompletion_WithOnePlayerWinningAllCards()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(7));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(11));

            objSession.PlayUntilGameOver();

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "최대 라운드 안에 종국까지 도달해야 한다");

            int nBlackCount = objSession.CountCardsOwnedBy(Ont.E_PlayerColor.Black);
            int nWhiteCount = objSession.CountCardsOwnedBy(Ont.E_PlayerColor.White);
            Assert.AreEqual(52, nBlackCount + nWhiteCount, "게임이 끝나도 카드 총량은 52장이어야 한다");
            Assert.IsTrue(nBlackCount == 0 || nWhiteCount == 0, "한쪽이 카드를 전부 잃어야 종국된 것이다");
        }

        public static void PlayRound_Throws_AfterGameHasEnded()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(3));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(4));
            objSession.PlayUntilGameOver();

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlayRound(),
                "종국된 대국에서 PlayRound를 호출하면 거부되어야 한다");
        }
    }
}
