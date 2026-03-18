using UnityEngine;

namespace Game
{
    public class EconomyBootstrap : MonoBehaviour
    {
        public IEconomy Runtime => _runtime;

        [SerializeField] EconomyConfig _config;

        IEconomy _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = EconomyFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
