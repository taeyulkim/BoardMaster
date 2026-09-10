namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    internal static class GamePhaseManagerTests
    {
        public static void Start_EntersFirstPhase_AndCallsOnPhaseEnterOnce()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A");
            FakeGamePhase objPhaseB = new FakeGamePhase("B");
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA, objPhaseB });

            objManager.Start(objContext);

            Assert.AreEqual("A", objManager.CurrentPhase.mv_strPhaseName, "시작하면 첫 페이즈에 있어야 한다");
            Assert.AreEqual(1, objPhaseA.EnterCount, "첫 페이즈의 OnPhaseEnter가 정확히 한 번 호출되어야 한다");
            Assert.AreEqual(0, objPhaseB.EnterCount, "두 번째 페이즈는 아직 진입하면 안 된다");
        }

        public static void Update_AdvancesToNextPhase_WhenCurrentPhaseCompletes()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A") { CompletesAfterUpdateCount = 1 };
            FakeGamePhase objPhaseB = new FakeGamePhase("B");
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA, objPhaseB });

            objManager.Start(objContext);
            objManager.Update(objContext);

            Assert.AreEqual("B", objManager.CurrentPhase.mv_strPhaseName, "완료 조건을 만족하면 다음 페이즈로 전이해야 한다");
            Assert.AreEqual(1, objPhaseA.ExitCount, "떠난 페이즈의 OnPhaseExit가 호출되어야 한다");
            Assert.AreEqual(1, objPhaseB.EnterCount, "새 페이즈의 OnPhaseEnter가 호출되어야 한다");
        }

        public static void Update_ObservesLatestContext_NotTheOneStartedWith()
        {
            // GoGameSession처럼 매 수마다 GameContext 인스턴스 자체가 통째로 바뀌는 호출자를 흉내낸다.
            // GamePhaseManager가 Start() 시점의 컨텍스트를 필드로 붙들어 뒀다면, 이후 새 인스턴스에서
            // 완료 조건이 참이 되어도 절대 알아채지 못한다.
            Ont.GameContext objInitialContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A") { CompletesAfterUpdateCount = 1 };
            FakeGamePhase objPhaseB = new FakeGamePhase("B");
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA, objPhaseB });

            objManager.Start(objInitialContext);

            Ont.GameContext objReplacementContext = TestFixtures.CreateContext(p_nSize: 3);
            objManager.Update(objReplacementContext);

            Assert.AreEqual("B", objManager.CurrentPhase.mv_strPhaseName, "Start() 이후 교체된 새 GameContext 인스턴스를 기준으로도 완료 판정이 정상 동작해야 한다");
        }

        public static void Update_DoesNothing_WhenAlreadyFinished()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A") { CompletesAfterUpdateCount = 0 };
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA });

            objManager.Start(objContext);
            Assert.IsTrue(objManager.IsFinished, "유일한 페이즈가 즉시 완료되면(CompletesAfterUpdateCount=0) 머신 전체가 끝나야 한다");

            objManager.Update(objContext);

            Assert.AreEqual(0, objPhaseA.UpdateCount, "이미 끝난 뒤의 Update()는 페이즈의 OnPhaseUpdate를 호출하면 안 된다");
        }

        public static void CurrentPhase_Throws_WhenFinished()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A") { CompletesAfterUpdateCount = 0 };
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA });

            objManager.Start(objContext);

            Assert.Throws<InvalidOperationException>(
                () => { OntDyn.IGamePhase _ = objManager.CurrentPhase; },
                "모든 페이즈가 끝난 뒤 CurrentPhase 접근은 예외를 던져야 한다");
        }

        public static void Start_Throws_WhenCalledTwice()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A");
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA });

            objManager.Start(objContext);

            Assert.Throws<InvalidOperationException>(() => objManager.Start(objContext), "Start()는 한 번만 호출할 수 있어야 한다");
        }

        public static void Update_Throws_WhenCalledBeforeStart()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            FakeGamePhase objPhaseA = new FakeGamePhase("A");
            OntDyn.GamePhaseManager objManager = new OntDyn.GamePhaseManager(new OntDyn.IGamePhase[] { objPhaseA });

            Assert.Throws<InvalidOperationException>(() => objManager.Update(objContext), "Start() 전에 Update()를 호출하면 예외가 발생해야 한다");
        }

        public static void Constructor_Throws_WhenPhaseListIsEmpty()
        {
            Assert.Throws<ArgumentException>(
                () => new OntDyn.GamePhaseManager(Array.Empty<OntDyn.IGamePhase>()),
                "빈 페이즈 목록은 거부되어야 한다");
        }
    }
}
