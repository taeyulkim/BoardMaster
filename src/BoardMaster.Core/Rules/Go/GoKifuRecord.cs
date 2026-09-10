namespace BoardMaster.Core.Rules.Go
{
    /// <summary>
    /// 기보(棋譜) 직렬화용 DTO입니다. 이벤트 소싱 철학대로 매 턴의 전체 반상 스냅샷이 아니라
    /// [초기 조건(가로/세로 크기, 참가자) + 행동 시퀀스]만 저장합니다 — 초기 상태는 항상
    /// "지정된 크기의 빈 보드, Black 선공"이라는 규약을 따르므로 별도로 저장할 필요가 없습니다.
    ///
    /// 내부 도메인 타입(GameContext 등)의 헝가리안 표기(mv_, m_)와 달리, 이 DTO는 외부 상호운용
    /// 경계(JSON 파일, 다른 도구)에 노출되는 계약이라 일반적인 C# PascalCase 프로퍼티로 작성했습니다.
    /// </summary>
    public sealed class GoKifuRecord
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<GoKifuPlayerRecord> Players { get; set; } = new();
        public List<GoKifuMoveRecord> Moves { get; set; } = new();

        /// <summary>
        /// 내보낼 당시의 참고용 메타데이터입니다. GoKifuSerializer.Replay는 이 값을 신뢰하지 않고
        /// Moves를 실제로 재생해 다시 계산합니다 — 리플레이 없이 파일만 훑어볼 때 쓰라고 남겨둔 값입니다.
        /// </summary>
        public bool IsGameOver { get; set; }
        public int BlackScore { get; set; }
        public int WhiteScore { get; set; }
    }

    public sealed class GoKifuPlayerRecord
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // "Black" / "White"
    }

    public sealed class GoKifuMoveRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsPass { get; set; }
        public string Color { get; set; } = string.Empty; // "Black" / "White"
    }
}
