namespace BoardMaster.Core.AI.Go
{
    using Ont = BoardMaster.Core.Ontology;
    using GoRules = BoardMaster.Core.Rules.Go;
    using DomainAction = BoardMaster.Core.Ontology.Action;

    /// <summary>
    /// 바둑 GameContext를 관측자 시점의 5채널 float 텐서로 정규화하는 구현체입니다.
    ///
    /// 채널 구성 (요구사항 2):
    ///   Ch0 자신의 기물, Ch1 상대의 공개된 기물, Ch2 착수 가능한 빈 공간, Ch3 규칙 제약 칸, Ch4 은닉 정보.
    ///
    /// 관측자 중심 프로젝션 (요구사항 1):
    ///   p_ePlayerColor로 넘어온 색이 항상 Ch0으로 정렬되므로, 어느 진영이든 같은 가중치로 추론 가능하다.
    ///
    /// 알려진 범위 축소 (정직하게 명시):
    ///   - Ch3은 이 시점 엔진에 존재하는 규칙(자충수, Cond_NotSuicide)만 반영한다. 패(Ko) 판정은
    ///     아직 엔진에 반상 해시/이력 추적이 구현되어 있지 않아(ActionDispatcher.UpdateBoardStateHash 미구현),
    ///     여기서 임의로 흉내 내지 않았다. Ko 추적이 추가되면 이 메서드에 조건을 하나 더 얹으면 된다.
    ///   - Ch4은 바둑에 은닉 정보가 없으므로 항상 0으로 유지된다(마피아/카드 등 후속 장르를 위한 자리).
    ///   - 정적 설계서 5.1.3의 포로 수/점수 등 스칼라 메타데이터 정규화는 이 어댑터의 5채널 고정 인터페이스에
    ///     들어갈 자리가 없어 이번 구현 범위에서 제외했다(별도 스칼라 벡터 출력이 필요하면 인터페이스 확장 필요).
    ///
    /// Zero-Allocation 설계 (요구사항 3):
    ///   - 착수 좌표별 합법성 판정은 Cond_NotSuicide(이미 검증된 zero-alloc BFS 활로 계산기)를 재사용한다.
    ///     매 호출마다 새 Action을 만들지 않도록, 재사용 가능한 스크래치 Action 인스턴스 하나를 생성자에서
    ///     한 번만 만들고 ST_ActionData(구조체, 스택/인라인 값이라 힙 할당 없음)만 매 셀마다 덮어쓴다.
    ///   - 출력 버퍼는 호출자가 미리 할당해 넘기고, 이 메서드는 Array.Clear + 인덱스 대입만 수행한다
    ///     (Array.Clear는 제자리 초기화이며 힙 할당이 아니다).
    ///   - 인스턴스가 스크래치 버퍼/Action을 갖고 있어 스레드 세이프하지 않다. MCTS 워커별로 인스턴스를 하나씩 두라.
    /// </summary>
    public sealed class GoTensorNormalizationAdapter : ITensorNormalizationAdapter
    {
        private const int CHANNEL_COUNT = 5;
        private const int CH_SELF = 0;
        private const int CH_OPPONENT = 1;
        private const int CH_LEGAL_EMPTY = 2;
        private const int CH_CONSTRAINT = 3;
        private const int CH_HIDDEN = 4; // 바둑은 은닉 정보가 없어 항상 0. Array.Clear로 충분하므로 별도 기록 없음.

        private readonly int m_nTargetWidth;
        private readonly int m_nTargetHeight;
        private readonly GoRules.Cond_NotSuicide m_objSuicideCondition;
        private readonly DomainAction m_objScratchAction;

        public GoTensorNormalizationAdapter(int p_nTargetWidth = 19, int p_nTargetHeight = 19)
        {
            if (p_nTargetWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(p_nTargetWidth));
            }

            if (p_nTargetHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(p_nTargetHeight));
            }

            m_nTargetWidth = p_nTargetWidth;
            m_nTargetHeight = p_nTargetHeight;
            m_objSuicideCondition = new GoRules.Cond_NotSuicide();
            m_objScratchAction = new DomainAction(
                "Tensor_LegalityProbe",
                new Ont.ST_ActionData(0, 0, false, Ont.E_PlayerColor.None));
        }

        public (int nChannels, int nWidth, int nHeight) GetInputShapeSpecification()
        {
            return (CHANNEL_COUNT, m_nTargetWidth, m_nTargetHeight);
        }

        public void NormalizeToFloatTensor(Ont.GameContext p_objContext, Ont.E_PlayerColor p_ePlayerColor, float[] p_a_fOutBuffer)
        {
            if (p_objContext is null)
            {
                throw new ArgumentNullException(nameof(p_objContext));
            }

            if (p_a_fOutBuffer is null)
            {
                throw new ArgumentNullException(nameof(p_a_fOutBuffer));
            }

            if (p_ePlayerColor == Ont.E_PlayerColor.None)
            {
                throw new ArgumentException("관측자 색상은 Black 또는 White여야 합니다.", nameof(p_ePlayerColor));
            }

            int nPlaneSize = m_nTargetWidth * m_nTargetHeight;
            int nRequiredLength = CHANNEL_COUNT * nPlaneSize;

            if (p_a_fOutBuffer.Length < nRequiredLength)
            {
                throw new ArgumentException(
                    $"출력 버퍼 길이가 부족합니다. 필요: {nRequiredLength}, 실제: {p_a_fOutBuffer.Length}",
                    nameof(p_a_fOutBuffer));
            }

            // 이전 호출의 잔여 값(특히 보드 크기가 target보다 작을 때의 패딩 영역)을 제자리에서 지운다.
            // Array.Clear는 힙 할당이 아니라 기존 배열을 초기화하는 in-place 연산이다.
            Array.Clear(p_a_fOutBuffer, 0, nRequiredLength);

            int[,] a_nGrid = p_objContext.mv_stCurrentState.m_a_nBoardGrid;
            int nBoardWidth = a_nGrid.GetLength(0);
            int nBoardHeight = a_nGrid.GetLength(1);

            // 동적 사이즈 조정: 실제 보드가 target보다 작으면 남는 영역은 Array.Clear로 이미 0(패딩) 상태이고,
            // 실제 보드가 target보다 크면 넘치는 부분은 그냥 스캔하지 않아 버퍼 오버런을 막는다.
            int nScanWidth = Math.Min(nBoardWidth, m_nTargetWidth);
            int nScanHeight = Math.Min(nBoardHeight, m_nTargetHeight);

            Ont.E_PlayerColor eOpponentColor =
                (p_ePlayerColor == Ont.E_PlayerColor.Black) ? Ont.E_PlayerColor.White : Ont.E_PlayerColor.Black;

            int nCh0Base = CH_SELF * nPlaneSize;
            int nCh1Base = CH_OPPONENT * nPlaneSize;
            int nCh2Base = CH_LEGAL_EMPTY * nPlaneSize;
            int nCh3Base = CH_CONSTRAINT * nPlaneSize;

            for (int nY = 0; nY < nScanHeight; nY++)
            {
                int nRowOffset = nY * m_nTargetWidth;

                for (int nX = 0; nX < nScanWidth; nX++)
                {
                    int nPlaneIndex = nRowOffset + nX;
                    Ont.E_PlayerColor eCellColor = (Ont.E_PlayerColor)a_nGrid[nX, nY];

                    if (eCellColor == p_ePlayerColor)
                    {
                        p_a_fOutBuffer[nCh0Base + nPlaneIndex] = 1f;
                    }
                    else if (eCellColor == eOpponentColor)
                    {
                        p_a_fOutBuffer[nCh1Base + nPlaneIndex] = 1f;
                    }
                    else
                    {
                        // 빈 칸: 자충수 여부를 한 번만 계산해 합법(Ch2)/제약(Ch3) 두 채널에 동시에 반영한다.
                        // ST_ActionData는 struct라 여기서의 new는 스택 값 대입일 뿐 힙 할당이 아니다.
                        m_objScratchAction.mv_stActionData = new Ont.ST_ActionData(nX, nY, false, p_ePlayerColor);

                        bool bIsLegal = m_objSuicideCondition.IsSatisfied(p_objContext, m_objScratchAction);
                        if (bIsLegal)
                        {
                            p_a_fOutBuffer[nCh2Base + nPlaneIndex] = 1f;
                        }
                        else
                        {
                            p_a_fOutBuffer[nCh3Base + nPlaneIndex] = 1f;
                        }
                    }
                }
            }
        }
    }
}
