using UnityEngine;

namespace Game
{
    public class BlacksmithBootstrap : MonoBehaviour
    {
        public IBlacksmith Runtime => _runtime;

        [SerializeField] BlacksmithConfig _config;

        IBlacksmith _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = BlacksmithFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
