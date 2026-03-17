using UnityEngine;

namespace Game
{
    /// <summary>
    /// 씬에서 한 번만 초기화할 때 사용. Config 참조 → Factory 호출 → 결과 등록.
    /// MonoBehaviour는 얇게 유지. 로직은 Runtime에 있음.
    /// </summary>
    public class TemplateBootstrap : MonoBehaviour
    {
        [SerializeField] TemplateConfig _config;

        void Start()
        {
            if (_config == null)
                return;

            ITemplate runtime = TemplateFactory.CreateRuntime(_config);
            runtime.Init();
            // 여기서 정적/DI 등에 runtime 등록. 예: TemplateService.Register(runtime);
        }
    }
}
