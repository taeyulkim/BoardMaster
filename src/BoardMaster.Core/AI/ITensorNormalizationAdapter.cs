namespace BoardMaster.Core.AI
{
    using BoardMaster.Core.Ontology;

    /// <summary>
    /// 온톨로지 그래프 상태(GameContext)를 특정 관측 플레이어 시점의 다중 채널 float 텐서로 변환하는 어댑터입니다.
    /// MCTS 롤아웃 등 초당 수만 번 호출되는 경로에서 쓰이므로, 구현체는 반드시 호출자가 미리 할당한
    /// p_a_fOutBuffer에 in-place로만 쓰고 내부적으로 힙 할당이 없어야 합니다.
    /// </summary>
    public interface ITensorNormalizationAdapter
    {
        /// <summary>
        /// 서버가 보존 중인 참 물리 상태(Ground Truth)를 p_ePlayerColor 시점의 텐서로 정규화하여
        /// p_a_fOutBuffer에 기록합니다. 버퍼는 GetInputShapeSpecification()이 알려주는 크기 이상이어야 합니다.
        /// </summary>
        void NormalizeToFloatTensor(
            GameContext p_objContext,
            E_PlayerColor p_ePlayerColor,
            float[] p_a_fOutBuffer);

        /// <summary>
        /// 이 어댑터가 생성하는 텐서의 정적 형태(채널 개수, 가로 너비, 세로 높이)를 반환합니다.
        /// </summary>
        (int nChannels, int nWidth, int nHeight) GetInputShapeSpecification();
    }
}
