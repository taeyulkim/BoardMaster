namespace BoardMaster.Core.Rules.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// mv_isGameOver가 true가 된 시점(Effect_CheckConsecutivePassGameEnd 다음에 실행되어야 함)에만
    /// GoScoreCalculator로 중국식(면적) 최종 점수를 계산해 각 PlayerState.mv_nScore에 반영합니다.
    /// 아직 게임이 끝나지 않았으면 아무 것도 하지 않고 그대로 통과시킵니다(매 턴 O(width*height)
    /// 계가를 도는 낭비를 막기 위함).
    /// </summary>
    public sealed class Effect_FinalizeScore : Ont.IEffect
    {
        private readonly GoScoreCalculator m_objScoreCalculator = new GoScoreCalculator();

        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (!p_objContext.mv_isGameOver)
            {
                return p_objContext;
            }

            ST_GoScoreResult stResult = m_objScoreCalculator.CalculateScore(p_objContext);

            for (int i = 0; i < p_objContext.mv_lisPlayers.Count; i++)
            {
                Ont.PlayerState objPlayer = p_objContext.mv_lisPlayers[i];

                if (objPlayer.mv_eColor == Ont.E_PlayerColor.Black)
                {
                    objPlayer.mv_nScore = stResult.m_nBlackScore;
                }
                else if (objPlayer.mv_eColor == Ont.E_PlayerColor.White)
                {
                    objPlayer.mv_nScore = stResult.m_nWhiteScore;
                }
            }

            return p_objContext;
        }
    }
}
