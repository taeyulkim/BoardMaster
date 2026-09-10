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

        public static void LastRoundResult_IsNull_BeforeAnyRoundIsPlayed()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(8));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(9));

            Assert.IsTrue(objSession.LastRoundResult is null, "라운드를 한 번도 진행하지 않았으면 null이어야 한다");
        }

        public static void LastRoundResult_ReportsBothCardRanks_AndConsistentWinner()
        {
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(1));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(2));

            objSession.PlayRound();

            War.WarRoundResult? objResult = objSession.LastRoundResult;
            Assert.IsTrue(objResult is not null, "라운드를 진행했으면 LastRoundResult가 채워져야 한다");
            Assert.IsTrue(objResult!.BlackCardRank is not null, "양쪽 다 카드가 넘치는 첫 라운드는 Black도 카드를 냈어야 한다");
            Assert.IsTrue(objResult.WhiteCardRank is not null, "양쪽 다 카드가 넘치는 첫 라운드는 White도 카드를 냈어야 한다");

            int nBlackRank = int.Parse(objResult.BlackCardRank!);
            int nWhiteRank = int.Parse(objResult.WhiteCardRank!);

            if (nBlackRank == nWhiteRank)
            {
                Assert.IsTrue(objResult.WasWar, "랭크가 같으면 WasWar가 true여야 한다");
                Assert.IsTrue(objResult.Winner is null, "전쟁(비김)이면 Winner가 없어야 한다");
            }
            else
            {
                Assert.IsTrue(!objResult.WasWar, "랭크가 다르면 WasWar가 false여야 한다");
                Ont.E_PlayerColor eExpectedWinner = nBlackRank > nWhiteRank ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
                Assert.AreEqual((int)eExpectedWinner, (int)objResult.Winner!.Value, "높은 랭크를 낸 쪽이 Winner여야 한다");
            }
        }

        public static void LastRoundResult_UpdatesAfterEveryRound_AcrossFullGame()
        {
            // 게임이 끝날 때까지 반복해서, 매 라운드마다 LastRoundResult가 그 라운드의 결과로
            // 갱신되는지(과거 라운드 값이 그대로 남아있지 않는지) 확인한다.
            Ont.GameContext objContext = War.WarGameFactory.CreateStandardGame(new Random(21));
            War.WarGameSession objSession = new War.WarGameSession(objContext, new Random(22));

            int nRounds = 0;
            while (objSession.CurrentPhaseName != "GameOver" && nRounds < 10000)
            {
                objSession.PlayRound();
                Assert.IsTrue(objSession.LastRoundResult is not null, $"라운드 {nRounds}: LastRoundResult가 비어있으면 안 된다");
                nRounds++;
            }

            Assert.IsTrue(nRounds > 0, "적어도 한 라운드는 진행됐어야 한다");
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
