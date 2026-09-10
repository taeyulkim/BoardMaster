namespace BoardMaster.Core.Rules.Go
{
    using System.Text.Json;
    using Ont = BoardMaster.Core.Ontology;
    using OntDyn = BoardMaster.Core.Ontology.Dynamic;

    /// <summary>
    /// GameContext/GoGameSession의 기보를 JSON으로 내보내고(Export), 다시 읽어(Parse) 처음부터
    /// 재생(Replay)합니다. System.Text.Json은 BCL에 내장되어 있어 별도 NuGet 패키지가 필요 없습니다.
    ///
    /// Replay는 저장된 좌표/색상을 GameContext에 직접 주입하는 게 아니라, 매 수를 GoGameSession.PlayStone/
    /// Pass로 실제로 다시 두게 합니다. 그래서 리플레이 자체가 "이 엔진이 결정적(deterministic)이고
    /// 규칙 기반으로 항상 같은 결과를 재현하는가"를 매번 검증해 주는 부산물을 얻습니다 — 저장된 수가
    /// 중간에 규칙을 위반하면(파일이 손상되었거나 조작되었다면) 그 시점에 RuleViolationException이
    /// 그대로 터집니다.
    /// </summary>
    public static class GoKifuSerializer
    {
        private static readonly JsonSerializerOptions s_objOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string Export(Ont.GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            GoKifuRecord objRecord = BuildRecord(p_objContext);
            return JsonSerializer.Serialize(objRecord, s_objOptions);
        }

        public static string Export(GoGameSession p_objSession)
        {
            if (p_objSession is null)
            {
                throw new ArgumentNullException(nameof(p_objSession));
            }

            return Export(p_objSession.mv_objCurrentContext);
        }

        public static GoKifuRecord Parse(string p_strJson)
        {
            if (string.IsNullOrWhiteSpace(p_strJson))
            {
                throw new ArgumentException("기보 JSON 문자열이 비어 있습니다.", nameof(p_strJson));
            }

            return JsonSerializer.Deserialize<GoKifuRecord>(p_strJson, s_objOptions)
                ?? throw new InvalidOperationException("기보 JSON을 파싱하지 못했습니다.");
        }

        /// <summary>
        /// 기록된 참가자와 초기 크기로 새 GameContext를 만든 뒤, Moves를 처음부터 순서대로
        /// GoGameSession에 다시 재생해 최종 상태의 세션을 반환합니다.
        /// 각 수를 두기 전 현재 활성 색상과 기록된 색상이 일치하는지 검증합니다 — 손상되었거나
        /// 조작된 기보를 조용히 잘못 재생하는 대신 즉시 예외로 알립니다.
        /// </summary>
        public static GoGameSession Replay(GoKifuRecord p_objRecord, OntDyn.IActionLogger? p_objLogger = null)
        {
            if (p_objRecord is null)
            {
                throw new ArgumentNullException(nameof(p_objRecord));
            }

            Ont.ST_BoardState stInitialState = new Ont.ST_BoardState(
                1,
                Ont.E_PlayerColor.Black,
                new int[p_objRecord.Width, p_objRecord.Height],
                0,
                0);

            Ont.GameContext objContext = new Ont.GameContext(stInitialState);

            foreach (GoKifuPlayerRecord objPlayerRecord in p_objRecord.Players)
            {
                Ont.E_PlayerColor eColor = Enum.Parse<Ont.E_PlayerColor>(objPlayerRecord.Color);
                objContext.mv_lisPlayers.Add(new Ont.PlayerState(objPlayerRecord.PlayerId, eColor));
            }

            GoGameSession objSession = new GoGameSession(objContext, p_objLogger);

            foreach (GoKifuMoveRecord objMove in p_objRecord.Moves)
            {
                Ont.E_PlayerColor eExpectedColor = objSession.mv_objCurrentContext.mv_stCurrentState.m_eActiveColor;
                Ont.E_PlayerColor eRecordedColor = Enum.Parse<Ont.E_PlayerColor>(objMove.Color);

                if (eRecordedColor != eExpectedColor)
                {
                    throw new InvalidOperationException(
                        $"기보 이력이 손상되었습니다: {eExpectedColor} 차례인데 기록된 색상은 {eRecordedColor}입니다.");
                }

                if (objMove.IsPass)
                {
                    objSession.Pass();
                }
                else
                {
                    objSession.PlayStone(objMove.X, objMove.Y);
                }
            }

            return objSession;
        }

        private static GoKifuRecord BuildRecord(Ont.GameContext p_objContext)
        {
            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;

            GoKifuRecord objRecord = new GoKifuRecord
            {
                Width = a_nGrid.GetLength(0),
                Height = a_nGrid.GetLength(1),
                IsGameOver = p_objContext.mv_isGameOver
            };

            foreach (Ont.PlayerState objPlayer in p_objContext.mv_lisPlayers)
            {
                objRecord.Players.Add(new GoKifuPlayerRecord
                {
                    PlayerId = objPlayer.mv_strPlayerID,
                    Color = objPlayer.mv_eColor.ToString()
                });

                if (objPlayer.mv_eColor == Ont.E_PlayerColor.Black)
                {
                    objRecord.BlackScore = objPlayer.mv_nScore;
                }
                else if (objPlayer.mv_eColor == Ont.E_PlayerColor.White)
                {
                    objRecord.WhiteScore = objPlayer.mv_nScore;
                }
            }

            foreach (Ont.ST_ActionData stMove in p_objContext.mv_lisHistory)
            {
                objRecord.Moves.Add(new GoKifuMoveRecord
                {
                    X = stMove.m_nX,
                    Y = stMove.m_nY,
                    IsPass = stMove.m_isPass,
                    Color = stMove.m_eColor.ToString()
                });
            }

            return objRecord;
        }
    }
}
