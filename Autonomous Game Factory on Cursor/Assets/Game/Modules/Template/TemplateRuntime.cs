namespace Game
{
    /// <summary>
    /// 순수 C# 런타임 로직. MonoBehaviour 상속 없음. 상태·비즈니스 로직만 담당.
    /// 새 모듈 생성 시 &lt;ModuleName&gt;Runtime.cs 로 복사 후 도메인 로직으로 교체.
    /// </summary>
    public class TemplateRuntime : ITemplate
    {
        const float EPSILON = 0.0001f;

        readonly TemplateConfig _config;
        float _accumulator;

        public TemplateRuntime(TemplateConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _accumulator = 0f;
        }

        public void Tick(float deltaTime)
        {
            _accumulator += deltaTime;
            float interval = _config.TickInterval;
            while (_accumulator >= interval - EPSILON)
            {
                _accumulator -= interval;
                Step();
            }
        }

        void Step()
        {
            // 도메인별 주기 로직
        }
    }
}
