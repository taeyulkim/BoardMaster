namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 위치 기반 슈퍼코 판정을 위한 Zobrist 해시 테이블입니다. 예전 구현(GoBoardPositionKey)은 매
    /// 후보 수마다 격자를 복제하고 통째로 문자열로 직렬화해서 비교했습니다(O(width*height), 할당까지
    /// 발생). 이 테이블을 쓰면 착수/포획으로 "바뀐 칸"만 골라 XOR하는 것으로 결과 배치의 해시를
    /// O(바뀐 칸 수)에 계산할 수 있습니다 — 대부분의 후보 수는 아무것도 포획하지 않으므로 사실상
    /// O(1)입니다. Cond_NotSuperko와 GoGameSession이 함께 씁니다.
    ///
    /// 전체 앱에서 공유하는 정적 테이블입니다: GoGameSession.Clone()이 슈퍼코 이력(해시 집합)을
    /// 복제본에 그대로 복사해 넘기는데, 원본과 복제본이 서로 다른 난수 테이블을 쓰면 같은 반상
    /// 배치가 다른 해시로 계산되어 이력 비교가 깨집니다. 고정 시드로 한 번만 생성해서 이 문제를
    /// 막고, 테스트 재현성도 함께 보장합니다.
    /// </summary>
    internal static class GoZobristTable
    {
        private const int MAX_BOARD_DIMENSION = 32; // 19x19까지의 실제 바둑판을 넉넉히 커버한다.
        private const int COLOR_COUNT = 2; // Black, White. None은 기여분이 0이라 테이블 항목이 필요 없다.
        private const int FIXED_SEED = 987654321;

        private static readonly ulong[] s_a_ulTable = BuildTable();

        public static ulong GetValue(int p_nX, int p_nY, Ont.E_PlayerColor p_eColor)
        {
            if (p_nX < 0 || p_nX >= MAX_BOARD_DIMENSION || p_nY < 0 || p_nY >= MAX_BOARD_DIMENSION)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(p_nX), $"GoZobristTable은 {MAX_BOARD_DIMENSION}x{MAX_BOARD_DIMENSION}까지의 좌표만 지원합니다.");
            }

            int nColorIndex = ColorIndex(p_eColor);
            return s_a_ulTable[(((p_nY * MAX_BOARD_DIMENSION) + p_nX) * COLOR_COUNT) + nColorIndex];
        }

        private static int ColorIndex(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor switch
            {
                Ont.E_PlayerColor.Black => 0,
                Ont.E_PlayerColor.White => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(p_eColor), "Zobrist 값은 Black/White에만 정의됩니다.")
            };
        }

        private static ulong[] BuildTable()
        {
            Random objRandom = new Random(FIXED_SEED);
            ulong[] a_ulTable = new ulong[MAX_BOARD_DIMENSION * MAX_BOARD_DIMENSION * COLOR_COUNT];
            byte[] a_byBuffer = new byte[8];

            for (int i = 0; i < a_ulTable.Length; i++)
            {
                objRandom.NextBytes(a_byBuffer);
                a_ulTable[i] = BitConverter.ToUInt64(a_byBuffer, 0);
            }

            return a_ulTable;
        }
    }
}
