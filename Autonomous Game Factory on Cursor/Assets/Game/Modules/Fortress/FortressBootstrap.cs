using UnityEngine;

namespace Game
{
    public class FortressBootstrap : MonoBehaviour
    {
        public IFortress Runtime => _runtime;

        [SerializeField] FortressConfig _config;

        IFortress _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = FortressFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
