using UnityEngine;

namespace Game
{
    public class UIBootstrap : MonoBehaviour
    {
        public IUI Runtime => _runtime;

        [SerializeField] UIConfig _config;

        IUI _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = UIFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
