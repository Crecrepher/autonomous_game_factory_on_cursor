namespace Game
{
    /// <summary>
    /// 모듈 공개 계약. 다른 모듈/시스템은 이 인터페이스만 참조한다.
    /// 새 모듈 생성 시 I&lt;ModuleName&gt;.cs 로 복사 후 이름·메서드를 도메인에 맞게 수정.
    /// </summary>
    public interface ITemplate
    {
        void Init();
        void Tick(float deltaTime);
    }
}
