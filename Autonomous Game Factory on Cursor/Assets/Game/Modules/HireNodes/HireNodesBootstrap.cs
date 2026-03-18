using UnityEngine;

namespace Game
{
    public class HireNodesBootstrap : MonoBehaviour
    {
        public IHireNodes Runtime => _runtime;

        [SerializeField] HireNodesConfig _config;

        IHireNodes _runtime;

        void Start()
        {
            if (_config == null)
                return;

            _runtime = HireNodesFactory.CreateRuntime(_config);
            _runtime.Init();
        }
    }
}
