using UnityEngine;

namespace Game
{
    public class BuffIconUIBootstrap : MonoBehaviour
    {
        public IBuffIconUI Runtime => _runtime;

        [SerializeField] BuffIconUIConfig _config;

        IBuffIconUI _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = BuffIconUIFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
