namespace BoardMaster.Core.Rules.GuryongTu
{
    using Ont = BoardMaster.Core.Ontology;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 양쪽 Pending Zone에 타일이 모두 올라와 있다고 가정하고(그렇지 않으면 아무 일도 하지 않습니다 —
    /// GuryongTuGameSession이 둘 다 찼을 때만 이 Effect가 든 Action을 디스패치합니다) 랭크를 비교해
    /// 승자의 PlayerState.mv_nScore를 1 올리고, 두 타일을 Discard로 치웁니다.
    ///
    /// 특수 규칙: 1은 9를 이깁니다(구룡투 고유 규칙, 승부의 긴박감을 위한 예외) — 그 외에는 항상
    /// 숫자가 큰 쪽이 이깁니다. 같은 숫자는 나올 수 없습니다(양쪽이 서로 다른 소유의 타일 세트를
    /// 쓰지만 같은 랭크를 동시에 낼 수는 있습니다 — 이 경우 무승부이며 아무도 점수를 얻지 못합니다).
    /// </summary>
    public sealed class Effect_ResolveComparisonAndScore : Ont.IEffect
    {
        public Ont.GameContext Apply(Ont.GameContext p_objContext, DomainAction p_objAction)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            List<Ont.Entity> lisBlackPending = GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, GuryongTuZoneId.PendingBlack);
            List<Ont.Entity> lisWhitePending = GuryongTuZoneQuery.FindEntitiesInZone(p_objContext, GuryongTuZoneId.PendingWhite);

            if (lisBlackPending.Count == 0 || lisWhitePending.Count == 0)
            {
                return p_objContext; // 아직 한쪽만 커밋했다 — 정산할 게 없다.
            }

            Ont.Entity objBlackTile = lisBlackPending[0];
            Ont.Entity objWhiteTile = lisWhitePending[0];
            int nBlackRank = int.Parse(objBlackTile.mv_strType);
            int nWhiteRank = int.Parse(objWhiteTile.mv_strType);

            Ont.E_PlayerColor? eWinner = DetermineWinner(nBlackRank, nWhiteRank);

            if (eWinner is not null)
            {
                Ont.PlayerState? objWinnerState = p_objContext.mv_lisPlayers.Find(p => p.mv_eColor == eWinner.Value);
                if (objWinnerState is not null)
                {
                    objWinnerState.mv_nScore++;
                }
            }

            Ont.Zone objDiscardZone = p_objContext.mv_dicZones[GuryongTuZoneId.Discard];
            objBlackTile.mv_objLocatedZone = objDiscardZone;
            objWhiteTile.mv_objLocatedZone = objDiscardZone;

            return p_objContext;
        }

        /// <summary>
        /// 승자를 결정합니다(무승부면 null). "1이 9를 이긴다"는 예외를 먼저 확인한 뒤, 그 외에는
        /// 그냥 숫자가 큰 쪽이 이깁니다. internal로 노출해 이 예외 규칙 자체를 단위 테스트로
        /// 직접 검증할 수 있게 했습니다.
        /// </summary>
        internal static Ont.E_PlayerColor? DetermineWinner(int p_nBlackRank, int p_nWhiteRank)
        {
            if (p_nBlackRank == p_nWhiteRank)
            {
                return null;
            }

            if (p_nBlackRank == 1 && p_nWhiteRank == 9)
            {
                return Ont.E_PlayerColor.Black;
            }

            if (p_nWhiteRank == 1 && p_nBlackRank == 9)
            {
                return Ont.E_PlayerColor.White;
            }

            return p_nBlackRank > p_nWhiteRank ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
        }
    }
}
