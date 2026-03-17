namespace Game.Editor.AI
{
    /// <summary>
    /// 모듈 검증기 계약. 검증 결과는 report에 추가한다.
    /// </summary>
    public interface IModuleValidator
    {
        void Validate(ValidationReport report);
    }
}
