namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using KC = BoardMaster.Core.Rules.KingsCrown;

    internal static class KingsCrownGameSessionTests
    {
        public static void CurrentPhaseName_StartsAsMainPlay()
        {
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), new List<int> { 1 }, new List<int> { 2 });

            Assert.AreEqual("MainPlay", objSession.CurrentPhaseName, "게임 시작 시 MainPlay 페이즈여야 한다");
        }

        public static void GetHeldChips_ReturnsInjectedChips()
        {
            List<int> lisBlackChips = new List<int> { 1, 2, 3 };
            List<int> lisWhiteChips = new List<int> { 4, 5, 6 };
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), lisBlackChips, lisWhiteChips);

            Assert.IsTrue(SameContents(lisBlackChips, objSession.GetHeldChips(Ont.E_PlayerColor.Black)), "Black의 보유 숫자칩이 주입한 값과 같아야 한다");
            Assert.IsTrue(SameContents(lisWhiteChips, objSession.GetHeldChips(Ont.E_PlayerColor.White)), "White의 보유 숫자칩이 주입한 값과 같아야 한다");
        }

        public static void PlaceCrown_PlacesEntity_SwitchesTurn_AndConsumesChip()
        {
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), new List<int> { 7, 8 }, new List<int> { 3 });

            objSession.PlaceCrown(7, 0, 0);

            Ont.Entity? objPlaced = objSession.GetPieceAt(0, 0);
            Assert.IsTrue(objPlaced is not null, "왕관이 (0,0)에 놓여 있어야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.Black, objPlaced!.mv_eColor, "놓은 왕관은 착수한 플레이어의 색이어야 한다");
            Assert.AreEqual("7", objPlaced.mv_strType, "왕관에 결합된 숫자가 사용한 숫자칩과 같아야 한다");
            Assert.AreEqual(Ont.E_PlayerColor.White, objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor, "착수 후 턴이 상대에게 넘어가야 한다");
            Assert.AreEqual(1, objSession.GetHeldChips(Ont.E_PlayerColor.Black).Count, "사용한 숫자칩은 보유 목록에서 사라져야 한다");
            Assert.IsTrue(!objSession.GetHeldChips(Ont.E_PlayerColor.Black).Contains(7), "사용한 숫자칩 7은 더 이상 보유 목록에 없어야 한다");
        }

        public static void PlaceCrown_Throws_WhenChipNotHeld()
        {
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), new List<int> { 7 }, new List<int> { 3 });

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlaceCrown(9, 0, 0), "보유하지 않은 숫자칩으로 놓으려 하면 예외가 발생해야 한다");
        }

        public static void PlaceCrown_Throws_WhenPlacementIllegal()
        {
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(
                KC.KingsCrownGameFactory.CreateStandardGame(), new List<int> { 7 }, new List<int> { 3 });

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlaceCrown(7, 2, 2), "가운데 칸에 놓으려 하면 예외가 발생해야 한다");
        }

        public static void PlaceCrown_DeclaresWinner_OnBingo()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            for (int nY = 0; nY < 4; nY++) // (0,4)만 비워둔다.
            {
                KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, nY + 1, 0, nY);
            }

            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 5 }, new List<int> { 9 });

            objSession.PlaceCrown(5, 0, 4);

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "빙고가 완성되면 즉시 GameOver 페이즈로 전환해야 한다");
            Ont.PlayerState objBlack = objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Assert.AreEqual(1, objBlack.mv_nScore, "빙고를 완성한 Black이 승리 점수를 받아야 한다");
        }

        public static void PlaceCrown_DeclaresWinner_WhenOpponentHasNoLegalPlacement()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();

            // 가운데 칸(2,2)과 (0,0)/(4,0) 두 칸만 비워두고 나머지 22칸을 모두 Black,5로 채운다.
            for (int nY = 0; nY < KC.KingsCrownGameFactory.BOARD_SIZE; nY++)
            {
                for (int nX = 0; nX < KC.KingsCrownGameFactory.BOARD_SIZE; nX++)
                {
                    if (KC.KingsCrownBoardGeometry.IsCenter(nX, nY)) continue;
                    if ((nX == 0 && nY == 0) || (nX == 4 && nY == 0)) continue;
                    KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, nX, nY);
                }
            }

            // Black은 (4,0)에 6(=5와 연속, 같은 색)을 놓을 수 있다. White는 10만 갖고 있는데,
            // 남는 유일한 빈 칸 (0,0)의 이웃은 전부 Black,5라서 "다른 색+같은 숫자"(10=5 아님)도
            // "같은 색+연속"(색이 다름)도 만족 못해 White는 꼼짝없이 막힌다.
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 6 }, new List<int> { 10 });

            objSession.PlaceCrown(6, 4, 0);

            Assert.AreEqual("GameOver", objSession.CurrentPhaseName, "상대가 더 이상 놓을 수 없으면 즉시 GameOver 페이즈로 전환해야 한다");
            Ont.PlayerState objBlack = objSession.mv_objCurrentContext.mv_lisPlayers.Find(p => p.mv_eColor == Ont.E_PlayerColor.Black)!;
            Assert.AreEqual(1, objBlack.mv_nScore, "마지막으로 왕관을 놓은 Black이 승리해야 한다");
        }

        public static void GetLegalPlacements_ReturnsExpectedMoves()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, 5, 1, 1);

            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 6 }, new List<int> { 3 });

            List<(int ChipValue, int X, int Y)> lisMoves = objSession.GetLegalPlacements(Ont.E_PlayerColor.Black);

            Assert.IsTrue(lisMoves.Contains((6, 1, 2)), "(1,1)의 Black,5와 연속된 숫자 6은 인접 칸에 놓을 수 있어야 한다");
            Assert.IsTrue(lisMoves.Contains((6, 4, 4)), "이웃이 없는 칸에도 6을 놓을 수 있어야 한다");
            Assert.IsTrue(!lisMoves.Contains((6, 2, 2)), "가운데 칸은 합법수 목록에 없어야 한다");
        }

        public static void PlaceCrown_Throws_AfterGameOver()
        {
            Ont.GameContext objContext = KingsCrownTestFixtures.CreateEmptyBoardContext();
            for (int nY = 0; nY < 4; nY++)
            {
                KingsCrownTestFixtures.PlaceCrown(objContext, Ont.E_PlayerColor.Black, nY + 1, 0, nY);
            }
            KC.KingsCrownGameSession objSession = new KC.KingsCrownGameSession(objContext, new List<int> { 5 }, new List<int> { 9 });
            objSession.PlaceCrown(5, 0, 4);

            Assert.Throws<OntDyn.RuleViolationException>(
                () => objSession.PlaceCrown(9, 4, 4), "게임이 끝난 뒤에는 왕관을 놓으려 하면 예외가 발생해야 한다");
        }

        private static bool SameContents(List<int> p_lisExpected, IReadOnlyList<int> p_lisActual)
        {
            List<int> lisExpectedSorted = new List<int>(p_lisExpected);
            List<int> lisActualSorted = new List<int>(p_lisActual);
            lisExpectedSorted.Sort();
            lisActualSorted.Sort();

            if (lisExpectedSorted.Count != lisActualSorted.Count)
            {
                return false;
            }

            for (int i = 0; i < lisExpectedSorted.Count; i++)
            {
                if (lisExpectedSorted[i] != lisActualSorted[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
