namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 양쪽이 패스로 물러나 대국이 끝났을 때(포위로 인한 즉시 승리가 아닐 때)만 쓰이는 계가입니다.
    /// 점수는 돌 수를 더하는 바둑의 중국식(면적) 계가와 달리 "완성된 영토 크기"만 비교합니다 —
    /// 반상 위 자기 성의 개수는 세지 않습니다(이 게임의 규칙 설명이 "영토 크기"를 비교한다고
    /// 명시하기 때문입니다). 선공이 후공보다 영토가 3 이상 많아야 선공 승이고, 그렇지 않으면
    /// (동률 포함) 후공 승입니다 — 무승부가 없는 이진 판정입니다.
    /// </summary>
    public sealed class GreatKingdomScoreCalculator
    {
        private const int PLAYER1_HANDICAP_MARGIN = 3;

        public ST_GreatKingdomScoreResult CalculateScore(Ont.GameContext p_objContext)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nWidth = a_nGrid.GetLength(0);
            int nHeight = a_nGrid.GetLength(1);

            (int nPlayer1Territory, int nPlayer2Territory) = GreatKingdomTerritoryScanner.CalculateAllTerritory(a_nGrid, nWidth, nHeight);
            bool bPlayer1Wins = (nPlayer1Territory - nPlayer2Territory) >= PLAYER1_HANDICAP_MARGIN;

            return new ST_GreatKingdomScoreResult(nPlayer1Territory, nPlayer2Territory, bPlayer1Wins);
        }
    }
}
