namespace BoardMaster.Core.Ontology
{
    /// <summary>
    /// Zone 단위의 정보 공개 범위입니다. 불완전 정보 게임에서 시야 마스킹의 기준이 됩니다.
    /// </summary>
    public enum E_VisibilityType
    {
        Public = 0,
        Private = 1,
        Hidden = 2
    }
}
