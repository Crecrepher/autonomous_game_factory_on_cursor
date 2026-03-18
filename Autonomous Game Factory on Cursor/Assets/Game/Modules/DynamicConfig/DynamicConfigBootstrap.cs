using UnityEngine;

namespace Game
{
    public class DynamicConfigBootstrap : MonoBehaviour
    {
        public IDynamicConfig Runtime => _runtime;

        [SerializeField] DynamicConfigConfig _config;

        IDynamicConfig _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = DynamicConfigFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
