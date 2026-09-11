namespace BoardMaster.Core.Rules.GreatKingdom
{
    /// <summary>
    /// 종국 시 양쪽의 완성된 영토 크기와, 그 비교로 정해진 승자를 함께 담는 값입니다. Go의
    /// ST_GoScoreResult와 달리 "누가 이겼는가"까지 여기서 확정합니다 — 그레이트 킹덤은 영토 크기
    /// 자체가 최종 점수가 아니라, "선공이 후공보다 3 이상 많은가"라는 이진 판정의 재료일 뿐이라서
    /// (핸디캡 있는 승부 판정) 비교 로직을 호출부마다 반복하지 않도록 이 구조체가 대신 계산해 둡니다.
    /// </summary>
    public readonly struct ST_GreatKingdomScoreResult
    {
        public readonly int m_nPlayer1Territory;
        public readonly int m_nPlayer2Territory;
        public readonly bool m_bPlayer1Wins;

        public ST_GreatKingdomScoreResult(int p_nPlayer1Territory, int p_nPlayer2Territory, bool p_bPlayer1Wins)
        {
            m_nPlayer1Territory = p_nPlayer1Territory;
            m_nPlayer2Territory = p_nPlayer2Territory;
            m_bPlayer1Wins = p_bPlayer1Wins;
        }
    }
}
