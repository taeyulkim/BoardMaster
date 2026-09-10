namespace BoardMaster.Core.Rules.Go
{
    /// <summary>
    /// GoScoreCalculator가 계산한 중국식(면적) 계가 결과를 담는 경량 불변 구조체입니다.
    /// </summary>
    public readonly struct ST_GoScoreResult
    {
        public readonly int m_nBlackScore;
        public readonly int m_nWhiteScore;

        public ST_GoScoreResult(int p_nBlackScore, int p_nWhiteScore)
        {
            m_nBlackScore = p_nBlackScore;
            m_nWhiteScore = p_nWhiteScore;
        }
    }
}
