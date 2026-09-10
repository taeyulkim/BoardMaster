namespace BoardMaster.Core.Tests
{
    using Ont = BoardMaster.Core.Ontology;
    using Go = BoardMaster.Core.Rules.Go;

    internal static class GoZobristTableTests
    {
        public static void GetValue_IsDeterministic_ForSameInputs()
        {
            ulong ulFirst = Go.GoZobristTable.GetValue(3, 4, Ont.E_PlayerColor.Black);
            ulong ulSecond = Go.GoZobristTable.GetValue(3, 4, Ont.E_PlayerColor.Black);

            Assert.AreEqual(ulFirst, ulSecond, "같은 좌표/색상은 항상 같은 값을 반환해야 한다");
        }

        public static void GetValue_DiffersAcrossCoordinatesAndColors()
        {
            ulong ulBlackAt00 = Go.GoZobristTable.GetValue(0, 0, Ont.E_PlayerColor.Black);
            ulong ulWhiteAt00 = Go.GoZobristTable.GetValue(0, 0, Ont.E_PlayerColor.White);
            ulong ulBlackAt10 = Go.GoZobristTable.GetValue(1, 0, Ont.E_PlayerColor.Black);

            Assert.IsTrue(ulBlackAt00 != ulWhiteAt00, "같은 좌표라도 색이 다르면 값이 달라야 한다");
            Assert.IsTrue(ulBlackAt00 != ulBlackAt10, "같은 색이라도 좌표가 다르면 값이 달라야 한다");
        }

        public static void GetValue_XorTwice_CancelsOut()
        {
            // Zobrist 해시의 핵심 성질: 같은 값을 두 번 XOR하면 원래대로 돌아온다(제거 연산의 기반).
            ulong ulValue = Go.GoZobristTable.GetValue(4, 4, Ont.E_PlayerColor.White);
            ulong ulHash = 0;

            ulHash ^= ulValue;
            ulHash ^= ulValue;

            Assert.AreEqual(0UL, ulHash, "같은 값을 두 번 XOR하면 원래 값으로 돌아와야 한다");
        }

        public static void GetValue_Throws_WhenCoordinateOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Go.GoZobristTable.GetValue(999, 0, Ont.E_PlayerColor.Black),
                "지원 범위를 벗어난 좌표는 예외를 던져야 한다");
        }
    }
}
