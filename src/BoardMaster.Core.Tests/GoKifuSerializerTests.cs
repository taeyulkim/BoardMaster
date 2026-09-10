namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoKifuSerializerTests
    {
        /// <summary>
        /// 순수 착수만으로 캡처와 종국까지 재현하는 짧은 실전 수순을 만든다(수동 그리드 조작 없음).
        /// Black(1,2)/(3,2)/(2,1)이 White(2,2)를 포위한 뒤 Black(2,3)이 마지막 활로를 메워 따내고,
        /// 이후 양쪽이 패스해 종국까지 이어진다. White(0,0)/(0,1)은 White 차례를 채우기 위한 딴 곳 수.
        /// </summary>
        private static Go.GoGameSession PlayScriptedGameWithCapture()
        {
            Ont.GameContext objContext = TestFixtures.CreateContext(p_nSize: 5, p_eActiveColor: Ont.E_PlayerColor.Black);
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("Black", Ont.E_PlayerColor.Black));
            objContext.mv_lisPlayers.Add(new Ont.PlayerState("White", Ont.E_PlayerColor.White));

            Go.GoGameSession objSession = new Go.GoGameSession(objContext);

            objSession.PlayStone(1, 2); // Black
            objSession.PlayStone(0, 0); // White (딴 곳)
            objSession.PlayStone(3, 2); // Black
            objSession.PlayStone(0, 1); // White (딴 곳)
            objSession.PlayStone(2, 1); // Black
            objSession.PlayStone(2, 2); // White: 활로 1개(=2,3)로 자충수 아님
            objSession.PlayStone(2, 3); // Black: White(2,2)의 마지막 활로를 메워 따낸다
            objSession.Pass();          // White
            objSession.Pass();          // Black -> 연속 2패스로 종국

            return objSession;
        }

        public static void Export_ProducesRecord_WithBoardSizePlayersAndFullMoveSequence()
        {
            Go.GoGameSession objSession = PlayScriptedGameWithCapture();

            string strJson = objSession.ExportKifu();
            Go.GoKifuRecord objRecord = Go.GoKifuSerializer.Parse(strJson);

            Assert.AreEqual(5, objRecord.Width, "보드 너비가 그대로 기록되어야 한다");
            Assert.AreEqual(5, objRecord.Height, "보드 높이가 그대로 기록되어야 한다");
            Assert.AreEqual(2, objRecord.Players.Count, "참가자 2명이 기록되어야 한다");
            Assert.AreEqual(9, objRecord.Moves.Count, "착수 7회 + 패스 2회 = 9수가 기록되어야 한다");
            Assert.IsTrue(objRecord.IsGameOver, "종국 메타데이터가 true로 기록되어야 한다");
        }

        public static void Replay_ReconstructsIdenticalFinalState_IncludingCaptureAndScore()
        {
            Go.GoGameSession objOriginalSession = PlayScriptedGameWithCapture();
            string strJson = objOriginalSession.ExportKifu();

            Go.GoKifuRecord objRecord = Go.GoKifuSerializer.Parse(strJson);
            Go.GoGameSession objReplayedSession = Go.GoKifuSerializer.Replay(objRecord);

            Ont.GameContext objOriginal = objOriginalSession.mv_objCurrentContext;
            Ont.GameContext objReplayed = objReplayedSession.mv_objCurrentContext;

            Assert.AreEqual(objOriginal.mv_isGameOver, objReplayed.mv_isGameOver, "종국 여부가 재생 결과와 같아야 한다");
            Assert.AreEqual((int)Ont.E_PlayerColor.None, objReplayed.mv_stCurrentState.m_a_nBoardGrid[2, 2], "재생 결과에서도 White(2,2)는 따내져 비어 있어야 한다");
            Assert.AreEqual(
                objOriginal.mv_stCurrentState.m_a_nBoardGrid[2, 2],
                objReplayed.mv_stCurrentState.m_a_nBoardGrid[2, 2],
                "캡처된 좌표의 상태가 원본과 재생본에서 같아야 한다");
            Assert.AreEqual(
                objOriginal.mv_stCurrentState.m_nBlackPrisoners,
                objReplayed.mv_stCurrentState.m_nBlackPrisoners,
                "Black 포로 수가 원본과 재생본에서 같아야 한다");
            Assert.AreEqual(
                objOriginal.mv_lisPlayers[0].mv_nScore,
                objReplayed.mv_lisPlayers[0].mv_nScore,
                "Black 최종 점수가 원본과 재생본에서 같아야 한다");
            Assert.AreEqual(
                objOriginal.mv_lisPlayers[1].mv_nScore,
                objReplayed.mv_lisPlayers[1].mv_nScore,
                "White 최종 점수가 원본과 재생본에서 같아야 한다");
        }

        public static void Replay_Throws_WhenRecordedColorDoesNotMatchExpectedTurn()
        {
            Go.GoKifuRecord objRecord = new Go.GoKifuRecord
            {
                Width = 5,
                Height = 5
            };
            objRecord.Players.Add(new Go.GoKifuPlayerRecord { PlayerId = "Black", Color = "Black" });
            objRecord.Players.Add(new Go.GoKifuPlayerRecord { PlayerId = "White", Color = "White" });
            // 첫 수는 항상 Black 차례여야 하는데 White로 조작된 손상 기보.
            objRecord.Moves.Add(new Go.GoKifuMoveRecord { X = 2, Y = 2, IsPass = false, Color = "White" });

            Assert.Throws<InvalidOperationException>(
                () => Go.GoKifuSerializer.Replay(objRecord),
                "기록된 색상이 실제 차례와 다르면 손상된 기보로 간주해 예외를 던져야 한다");
        }

        public static void Parse_RoundTrips_ExportedJson()
        {
            Go.GoGameSession objSession = PlayScriptedGameWithCapture();
            string strJson = objSession.ExportKifu();

            Go.GoKifuRecord objRecord = Go.GoKifuSerializer.Parse(strJson);

            Assert.AreEqual(9, objRecord.Moves.Count, "파싱된 기록의 수 개수가 원본과 같아야 한다");
            Assert.AreEqual(1, objRecord.Moves[0].X, "첫 수의 X 좌표가 그대로 보존되어야 한다");
            Assert.AreEqual(2, objRecord.Moves[0].Y, "첫 수의 Y 좌표가 그대로 보존되어야 한다");
            Assert.IsTrue(!objRecord.Moves[0].IsPass, "첫 수는 착수이지 패스가 아니어야 한다");
            Assert.AreEqual("Black", objRecord.Moves[0].Color, "첫 수의 색상이 Black으로 보존되어야 한다");
        }
    }
}
