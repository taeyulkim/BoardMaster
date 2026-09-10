namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;
    using DomainAction = BoardMaster.Core.Ontology.Action;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoNoSuperkoModifierTests
    {
        public static void Apply_RemovesCondNotSuperko_FromActionConditions()
        {
            DomainAction objAction = new DomainAction("Action_PlayStone", new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.Black));
            objAction.mv_lisConditions.Add(new Go.Cond_EmptySpace());
            objAction.mv_lisConditions.Add(new Go.Cond_NotSuperko(0, new HashSet<ulong>()));

            Go.GoNoSuperkoModifier objModifier = new Go.GoNoSuperkoModifier();
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            objModifier.Apply(objAction, objContext);

            Assert.AreEqual(1, objAction.mv_lisConditions.Count, "Cond_NotSuperko 하나만 제거되어야 한다");
            Assert.IsTrue(objAction.mv_lisConditions[0] is Go.Cond_EmptySpace, "다른 조건은 그대로 남아 있어야 한다");
        }

        public static void GoGameSession_WithModifier_AssemblesActionWithoutCondNotSuperko()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            List<OntDyn.IRuleModifier> lisModifiers = new List<OntDyn.IRuleModifier> { new Go.GoNoSuperkoModifier() };
            Go.GoGameSession objSession = new Go.GoGameSession(objContext, null, lisModifiers);

            DomainAction objAction = objSession.CreatePlaceStoneAction(2, 2, Ont.E_PlayerColor.Black);

            bool bHasSuperko = false;
            foreach (Ont.ICondition objCondition in objAction.mv_lisConditions)
            {
                if (objCondition is Go.Cond_NotSuperko)
                {
                    bHasSuperko = true;
                }
            }

            Assert.IsTrue(!bHasSuperko, "GoNoSuperkoModifier가 등록되면 조립 결과에 Cond_NotSuperko가 없어야 한다");
        }

        public static void GoGameSession_WithoutModifier_AssemblesActionWithCondNotSuperko()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            DomainAction objAction = objSession.CreatePlaceStoneAction(2, 2, Ont.E_PlayerColor.Black);

            bool bHasSuperko = false;
            foreach (Ont.ICondition objCondition in objAction.mv_lisConditions)
            {
                if (objCondition is Go.Cond_NotSuperko)
                {
                    bHasSuperko = true;
                }
            }

            Assert.IsTrue(bHasSuperko, "모디파이어가 없으면 기본 조립 결과에 Cond_NotSuperko가 있어야 한다(회귀 확인)");
        }

        public static void GoGameSession_WithModifier_StillPlaysNormally()
        {
            // 모디파이어가 있어도(느린 GetLegalMoves 경로로 전환되어도) 기본 게임 진행 자체는
            // 정상적으로 동작해야 한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            List<OntDyn.IRuleModifier> lisModifiers = new List<OntDyn.IRuleModifier> { new Go.GoNoSuperkoModifier() };
            Go.GoGameSession objSession = new Go.GoGameSession(objContext, null, lisModifiers);

            List<(int X, int Y)> lisLegalMoves = objSession.GetLegalMoves();
            Assert.AreEqual(25, lisLegalMoves.Count, "빈 5x5 보드는 25칸 모두 합법수여야 한다(모디파이어 유무와 무관)");

            Ont.GameContext objResult = objSession.PlayStone(2, 2);
            Assert.AreEqual((int)Ont.E_PlayerColor.Black, objResult.mv_stCurrentState.m_a_nBoardGrid[2, 2], "모디파이어가 있어도 착수는 정상 반영되어야 한다");
        }
    }
}
