namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using GT = BoardMaster.Core.Rules.GuryongTu;

    internal static class GuryongTuGameSessionTests
    {
        public static void CurrentPhaseName_StartsAsMainPlay()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());
            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "시작 시점엔 MainPlay 페이즈여야 한다");
        }

        public static void CommitTile_OnlyOneSide_DoesNotResolveYet()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());

            objSession.CommitTile(Ont.E_PlayerColor.Black, 5);

            Assert.IsTrue(objSession.HasCommittedThisRound(Ont.E_PlayerColor.Black), "Black은 커밋한 상태여야 한다");
            Assert.IsTrue(!objSession.HasCommittedThisRound(Ont.E_PlayerColor.White), "White는 아직 커밋 전이어야 한다");
            Assert.IsTrue(objSession.LastRoundResult is null, "한쪽만 냈으면 아직 라운드가 정산되면 안 된다");
        }

        public static void CommitTile_BothSides_ResolvesRoundAndUpdatesScore()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());

            objSession.CommitTile(Ont.E_PlayerColor.Black, 8);
            objSession.CommitTile(Ont.E_PlayerColor.White, 2);

            Assert.IsTrue(!objSession.HasCommittedThisRound(Ont.E_PlayerColor.Black), "정산 후에는 다음 라운드를 위해 Pending이 비어야 한다");
            Assert.IsTrue(objSession.LastRoundResult is not null, "양쪽 다 냈으면 라운드가 정산되어야 한다");
            Assert.AreEqual(8, objSession.LastRoundResult!.BlackRank, "LastRoundResult에 Black의 랭크가 기록되어야 한다");
            Assert.AreEqual(2, objSession.LastRoundResult.WhiteRank, "LastRoundResult에 White의 랭크가 기록되어야 한다");
            Assert.AreEqual((int)Ont.E_PlayerColor.Black, (int)objSession.LastRoundResult.Winner!.Value, "더 높은 랭크(8)를 낸 Black이 이겨야 한다");
            Assert.AreEqual(1, objSession.CountRoundsWon(Ont.E_PlayerColor.Black), "Black의 승 카운트가 1이어야 한다");
            Assert.AreEqual(0, objSession.CountRoundsWon(Ont.E_PlayerColor.White), "White의 승 카운트는 0이어야 한다");
        }

        public static void CommitTile_Throws_WhenSameRankCommittedTwiceInOneRound()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());
            objSession.CommitTile(Ont.E_PlayerColor.Black, 5);

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.CommitTile(Ont.E_PlayerColor.Black, 3),
                "같은 플레이어가 한 라운드에 두 번 커밋하려 하면 거부되어야 한다(블라인드 규칙 보호)");
        }

        public static void CommitTile_Throws_WhenRankAlreadyUsed()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());
            objSession.CommitTile(Ont.E_PlayerColor.Black, 5);
            objSession.CommitTile(Ont.E_PlayerColor.White, 5); // 동점 라운드 -> 둘 다 5 소진.

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.CommitTile(Ont.E_PlayerColor.Black, 5),
                "이미 다 쓴 랭크를 다시 내려 하면 거부되어야 한다");
        }

        public static void GetRemainingTiles_ShrinksAsRoundsAreResolved()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());
            Assert.AreEqual(9, objSession.GetRemainingTiles(Ont.E_PlayerColor.Black).Count, "시작 시 9개를 가져야 한다");

            objSession.CommitTile(Ont.E_PlayerColor.Black, 7);
            objSession.CommitTile(Ont.E_PlayerColor.White, 4);

            List<int> lisRemaining = objSession.GetRemainingTiles(Ont.E_PlayerColor.Black);
            Assert.AreEqual(8, lisRemaining.Count, "한 라운드가 정산되면 8개로 줄어야 한다");
            Assert.IsTrue(!lisRemaining.Contains(7), "이미 낸 랭크는 더 이상 남아있으면 안 된다");
        }

        public static void PlayingAllNineRounds_EndsGame_WithScoresSummingToAtMostNine()
        {
            // 매 라운드 Black은 오름차순(1,2,...), White는 내림차순(9,8,...)으로 낸다 — 서로 다른
            // 조합이 매번 나오게 해서 9라운드 전부를 실제로 정산까지 밀어붙여 본다.
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());

            for (int i = 0; i < 9; i++)
            {
                int nBlackRank = i + 1;
                int nWhiteRank = 9 - i;
                objSession.CommitTile(Ont.E_PlayerColor.Black, nBlackRank);
                objSession.CommitTile(Ont.E_PlayerColor.White, nWhiteRank);
            }

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "9라운드를 전부 마치면 종국이어야 한다");

            int nTotalRoundsWon = objSession.CountRoundsWon(Ont.E_PlayerColor.Black) + objSession.CountRoundsWon(Ont.E_PlayerColor.White);
            Assert.IsTrue(nTotalRoundsWon <= 9, "정산된 라운드 수(승+무)는 9를 넘을 수 없다");
            Assert.AreEqual(0, objSession.GetRemainingTiles(Ont.E_PlayerColor.Black).Count, "종국 시 Black의 Hand는 비어 있어야 한다");
            Assert.AreEqual(0, objSession.GetRemainingTiles(Ont.E_PlayerColor.White).Count, "종국 시 White의 Hand는 비어 있어야 한다");
        }

        public static void CommitTile_Throws_AfterGameHasEnded()
        {
            GT.GuryongTuGameSession objSession = new GT.GuryongTuGameSession(GT.GuryongTuGameFactory.CreateStandardGame());

            for (int i = 0; i < 9; i++)
            {
                objSession.CommitTile(Ont.E_PlayerColor.Black, i + 1);
                objSession.CommitTile(Ont.E_PlayerColor.White, 9 - i);
            }

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.CommitTile(Ont.E_PlayerColor.Black, 1),
                "종국된 대국에서 타일을 내려 하면 거부되어야 한다");
        }
    }
}
