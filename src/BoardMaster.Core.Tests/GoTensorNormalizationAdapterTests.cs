namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using AiGo = BoardMaster.Core.AI.Go;

    internal static class GoTensorNormalizationAdapterTests
    {
        public static void GetInputShapeSpecification_ReturnsConfiguredShape()
        {
            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(9, 9);

            (int nChannels, int nWidth, int nHeight) = objAdapter.GetInputShapeSpecification();

            Assert.AreEqual(5, nChannels, "채널 개수는 항상 5여야 한다");
            Assert.AreEqual(9, nWidth, "너비는 생성자에서 지정한 target 너비와 같아야 한다");
            Assert.AreEqual(9, nHeight, "높이는 생성자에서 지정한 target 높이와 같아야 한다");
        }

        public static void NormalizeToFloatTensor_AlignsChannel0ToObserver_RegardlessOfActualColor()
        {
            // 5x5 보드, (1,1)=Black, (3,3)=White. 관측자를 Black/White 두 번 바꿔가며
            // Ch0(자신)가 항상 관측자 자신의 돌로, Ch1(상대)이 항상 반대쪽 돌로 정렬되는지 확인한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 3, 3, Ont.E_PlayerColor.White);

            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(5, 5);
            float[] a_fBuffer = new float[5 * 5 * 5];

            objAdapter.NormalizeToFloatTensor(objContext, Ont.E_PlayerColor.Black, a_fBuffer);
            Assert.AreEqual(1f, a_fBuffer[Plane(0, 5, 1, 1)], "Black 관측 시 Ch0(1,1)은 자신의 돌이므로 1이어야 한다");
            Assert.AreEqual(1f, a_fBuffer[Plane(1, 5, 3, 3)], "Black 관측 시 Ch1(3,3)은 상대의 돌이므로 1이어야 한다");
            Assert.AreEqual(0f, a_fBuffer[Plane(1, 5, 1, 1)], "Black 관측 시 Ch1(1,1)은 0이어야 한다(자신의 돌이 상대 채널에 새면 안 된다)");

            objAdapter.NormalizeToFloatTensor(objContext, Ont.E_PlayerColor.White, a_fBuffer);
            Assert.AreEqual(1f, a_fBuffer[Plane(0, 5, 3, 3)], "White 관측 시 Ch0(3,3)은 자신의 돌이므로 1이어야 한다");
            Assert.AreEqual(1f, a_fBuffer[Plane(1, 5, 1, 1)], "White 관측 시 Ch1(1,1)은 상대의 돌이므로 1이어야 한다");
            Assert.AreEqual(0f, a_fBuffer[Plane(0, 5, 1, 1)], "White 관측 시 Ch0(1,1)은 0이어야 한다");
        }

        public static void NormalizeToFloatTensor_MarksSuicideCellAsConstraint_NotLegalEmpty()
        {
            // Cond_NotSuicideTests의 순수 자충수 시나리오와 동일한 배치.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);

            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(5, 5);
            float[] a_fBuffer = new float[5 * 5 * 5];

            objAdapter.NormalizeToFloatTensor(objContext, Ont.E_PlayerColor.Black, a_fBuffer);

            Assert.AreEqual(1f, a_fBuffer[Plane(3, 5, 0, 0)], "자충수 칸은 Ch3(제약)에 1로 표시되어야 한다");
            Assert.AreEqual(0f, a_fBuffer[Plane(2, 5, 0, 0)], "자충수 칸은 Ch2(합법 빈칸)에는 표시되면 안 된다");
        }

        public static void NormalizeToFloatTensor_MarksCapturingCellAsLegalEmpty_NotConstraint()
        {
            // Cond_NotSuicideTests의 캡처 합법수 시나리오와 동일한 배치.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objContext, 1, 0, Ont.E_PlayerColor.White);
            TestFixtures.SetGrid(objContext, 2, 0, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);
            TestFixtures.SetGrid(objContext, 0, 1, Ont.E_PlayerColor.White);

            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(5, 5);
            float[] a_fBuffer = new float[5 * 5 * 5];

            objAdapter.NormalizeToFloatTensor(objContext, Ont.E_PlayerColor.Black, a_fBuffer);

            Assert.AreEqual(1f, a_fBuffer[Plane(2, 5, 0, 0)], "상대를 따낼 수 있는 칸은 Ch2(합법 빈칸)에 1로 표시되어야 한다");
            Assert.AreEqual(0f, a_fBuffer[Plane(3, 5, 0, 0)], "상대를 따낼 수 있는 칸은 Ch3(제약)에는 표시되면 안 된다");
        }

        public static void NormalizeToFloatTensor_PadsUnusedRegion_WhenBoardSmallerThanTarget()
        {
            // 실제 보드는 3x3인데 target은 5x5 -> (4,4)처럼 실제 보드 밖 좌표는 모든 채널이 0(패딩)이어야 한다.
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 3);
            TestFixtures.SetGrid(objContext, 1, 1, Ont.E_PlayerColor.Black);

            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(5, 5);
            float[] a_fBuffer = new float[5 * 5 * 5];

            objAdapter.NormalizeToFloatTensor(objContext, Ont.E_PlayerColor.Black, a_fBuffer);

            for (int nChannel = 0; nChannel < 5; nChannel++)
            {
                Assert.AreEqual(0f, a_fBuffer[Plane(nChannel, 5, 4, 4)], $"실제 3x3 보드 밖인 (4,4)는 Ch{nChannel}에서 항상 0(패딩)이어야 한다");
            }

            Assert.AreEqual(1f, a_fBuffer[Plane(0, 5, 1, 1)], "실제 보드 안쪽 좌표는 정상적으로 채워져야 한다");
        }

        public static void NormalizeToFloatTensor_ClearsStalePaddingFromPreviousLargerBoard()
        {
            // 같은 어댑터/버퍼를 재사용할 때, 이전 호출의 값이 다음 호출의 패딩 영역에 남아있으면 안 된다.
            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(5, 5);
            float[] a_fBuffer = new float[5 * 5 * 5];

            Ont.GameContext objFullContext = TestFixtures.CreateContext(p_nSize: 5);
            TestFixtures.SetGrid(objFullContext, 4, 4, Ont.E_PlayerColor.Black);
            objAdapter.NormalizeToFloatTensor(objFullContext, Ont.E_PlayerColor.Black, a_fBuffer);
            Assert.AreEqual(1f, a_fBuffer[Plane(0, 5, 4, 4)], "첫 호출에서는 (4,4)가 채워져 있어야 한다");

            Ont.GameContext objSmallContext = TestFixtures.CreateContext(p_nSize: 3);
            objAdapter.NormalizeToFloatTensor(objSmallContext, Ont.E_PlayerColor.Black, a_fBuffer);
            Assert.AreEqual(0f, a_fBuffer[Plane(0, 5, 4, 4)], "더 작은 보드로 재호출하면 이전 값이 남지 않고 0으로 지워져야 한다");
        }

        public static void NormalizeToFloatTensor_Throws_WhenBufferTooSmall()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5);
            AiGo.GoTensorNormalizationAdapter objAdapter = new AiGo.GoTensorNormalizationAdapter(5, 5);
            float[] a_fTooSmallBuffer = new float[5 * 5 * 5 - 1];

            Assert.Throws<ArgumentException>(
                () => objAdapter.NormalizeToFloatTensor(objContext, Ont.E_PlayerColor.Black, a_fTooSmallBuffer),
                "target 크기에 필요한 길이보다 작은 버퍼를 넘기면 ArgumentException이 발생해야 한다");
        }

        /// <summary>
        /// 어댑터의 채널-우선(channel-first) 평탄화 인덱스 공식을 그대로 재현한다.
        /// 이 테스트 파일은 정사각형 target(너비==높이)만 쓰므로 p_nSize 하나로 너비/높이를 겸한다.
        /// </summary>
        private static int Plane(int p_nChannel, int p_nSize, int p_nX, int p_nY)
        {
            return ((p_nChannel * p_nSize + p_nY) * p_nSize) + p_nX;
        }
    }
}
