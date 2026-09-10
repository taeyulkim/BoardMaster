namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoPhasesTests
    {
        public static void GoMainPlayPhase_IsPhaseCompleted_ReflectsGameOverFlag()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            Go.GoMainPlayPhase objPhase = new Go.GoMainPlayPhase();

            Assert.IsTrue(!objPhase.IsPhaseCompleted(objContext), "mv_isGameOver가 false면 아직 완료된 게 아니다");

            objContext.mv_isGameOver = true;

            Assert.IsTrue(objPhase.IsPhaseCompleted(objContext), "mv_isGameOver가 true면 완료된 것으로 봐야 한다");
        }

        public static void GoGameOverPhase_IsPhaseCompleted_AlwaysFalse()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            objContext.mv_isGameOver = true;

            Go.GoGameOverPhase objPhase = new Go.GoGameOverPhase();

            Assert.IsTrue(!objPhase.IsPhaseCompleted(objContext), "GameOver는 종단 페이즈라 스스로는 절대 완료되면 안 된다");
        }
    }
}
