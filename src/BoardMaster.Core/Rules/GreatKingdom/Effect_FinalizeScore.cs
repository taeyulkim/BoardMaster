namespace BoardMaster.Core.Rules.GreatKingdom
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// mv_isGameOver가 true가 된 시점(양쪽 연속 패스로 종료된 경우 — 포위 승리는
    /// Effect_CaptureStonesAndCheckSiege가 이미 PlayerState.mv_nScore를 채워놓았으므로 여기서
    /// 다시 건드리지 않습니다)에 GreatKingdomScoreCalculator로 영토를 비교해 승자를 정합니다.
    /// mv_nScore는 원시 영토 크기가 아니라 승패 표시(승자 1 / 패자 0)입니다 — 포위 승리 경로와
    /// 같은 의미로 맞춰서, 호출부(WPF, MCTS)가 종국 사유와 상관없이 mv_nScore == 1만 보면 승자를
    /// 알 수 있게 했습니다. 영토 숫자 자체가 필요하면 GreatKingdomScoreCalculator를 직접 부르면 됩니다.
    /// </summary>
    public sealed class Effect_FinalizeScore : Ont.IEffect
    {
        private readonly GreatKingdomScoreCalculator m_objScoreCalculator = new GreatKingdomScoreCalculator();

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

            // 이미 포위로 승자가 정해진 경우(둘 중 하나가 mv_nScore == 1) 다시 계산하지 않는다.
            foreach (Ont.PlayerState objExisting in p_objContext.mv_lisPlayers)
            {
                if (objExisting.mv_nScore != 0)
                {
                    return p_objContext;
                }
            }

            ST_GreatKingdomScoreResult stResult = m_objScoreCalculator.CalculateScore(p_objContext);

            foreach (Ont.PlayerState objPlayer in p_objContext.mv_lisPlayers)
            {
                bool bIsPlayer1 = objPlayer.mv_eColor == Ont.E_PlayerColor.Black;
                bool bWins = bIsPlayer1 ? stResult.m_bPlayer1Wins : !stResult.m_bPlayer1Wins;
                objPlayer.mv_nScore = bWins ? 1 : 0;
            }

            return p_objContext;
        }
    }
}
