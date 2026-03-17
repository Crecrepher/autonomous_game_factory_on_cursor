namespace Game
{
    /// <summary>
    /// Config로 Runtime 인스턴스 생성. 의존성 주입·테스트·Bootstrap에서 사용.
    /// 새 모듈 생성 시 &lt;ModuleName&gt;Factory.cs 로 복사.
    /// </summary>
    public static class TemplateFactory
    {
        public static ITemplate CreateRuntime(TemplateConfig config)
        {
            return new TemplateRuntime(config);
        }
    }
}
